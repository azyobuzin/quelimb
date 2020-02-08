using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq.Expressions;
using System.Runtime.InteropServices;

namespace Quelimb.QueryFactory
{
    internal sealed partial class QueryFactoryCache
    {
        private readonly ConcurrentDictionary<CacheKey, CacheItem> _dic = new ConcurrentDictionary<CacheKey, CacheItem>();
        [ThreadStatic] private static HashAndConstantCollector? s_collector;

        private readonly QueryEnvironment _environment;

        public QueryFactoryCache(QueryEnvironment environment)
        {
            this._environment = environment;
        }

        public void SetupCommand(LambdaExpression lambda, IDbCommand command)
        {
            var fs = this.CreateQueryString(lambda);
            ResolveTableAliases(fs, this._environment);
            SetQueryToDbCommand(fs, command, this._environment);
        }

        private FormattableString CreateQueryString(LambdaExpression lambda)
        {
            var collector = s_collector ?? (s_collector = new HashAndConstantCollector());
            collector.Reset();
            collector.Walk(lambda);
            var hashCode = collector.Hasher.ToHashCode();

            if (!this._dic.TryGetValue(new ExpressionKey(lambda, hashCode), out var cacheItem))
            {
                // Compile the expression tree! (slow path)
                var dlg = QueryFactoryCompiler.CreateDelegate(lambda, this._environment);
                var serialized = TreeSerializer.Serialize(lambda);

                cacheItem = new CacheItem(serialized, hashCode, dlg);
                cacheItem = this._dic.GetOrAdd(new SerializedKey(serialized, hashCode), cacheItem);
            }

            return cacheItem.CompiledDelegate(collector.ObjectConstants);
        }

        internal sealed class HashAndConstantCollector : TreeWalker
        {
            public HashCode Hasher;
            public readonly List<object> ObjectConstants = new List<object>();

            public void Reset()
            {
                this.Hasher = new HashCode();
                this.ObjectConstants.Clear();
            }

            protected override bool OnData(ReadOnlySpan<byte> data)
            {
                // If data is large, hash by 4 bytes.
                if (data.Length >= sizeof(int))
                {
                    var ints = MemoryMarshal.Cast<byte, int>(data);
                    foreach (var x in ints) this.Hasher.Add(x);
                    data = data.Slice(ints.Length * sizeof(int));
                }

                foreach (var x in data) this.Hasher.Add(x);

                return true;
            }

            protected override void OnObjectConstant(ConstantExpression expression)
            {
                this.ObjectConstants.Add(expression.Value);
            }

            public static int CalculateHashCode(Expression expression)
            {
                var self = new HashAndConstantCollector();
                self.Walk(expression);
                return self.Hasher.ToHashCode();
            }
        }

        internal sealed class TreeSerializer : TreeWalker
        {
            private const int MinBufferSize = 256; // TODO: Optimize for use cases
            private byte[]? _buffer;
            private int _position;

            private TreeSerializer() { }

            protected override bool OnData(ReadOnlySpan<byte> data)
            {
                if (this._buffer == null)
                {
                    var bufSize = data.Length < MinBufferSize
                        ? MinBufferSize : checked(data.Length + MinBufferSize);
                    this._buffer = new byte[bufSize];
                }
                else if (this._buffer.Length - this._position < data.Length)
                {
                    var bufSize = checked(this._buffer.Length * 2);
                    if (bufSize - this._position < data.Length)
                        bufSize = checked(this._position + data.Length + MinBufferSize);
                    Array.Resize(ref this._buffer, bufSize);
                }

                data.CopyTo(new Span<byte>(this._buffer, this._position, data.Length));
                this._position += data.Length;

                return true;
            }

            public static ImmutableArray<byte> Serialize(Expression expression)
            {
                var self = new TreeSerializer();
                self.Walk(expression);

                return self._buffer == null
                    ? ImmutableArray<byte>.Empty
                    : ImmutableArray.Create(self._buffer, 0, self._position);
            }
        }
    }
}
