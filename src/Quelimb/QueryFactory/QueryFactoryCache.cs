using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Runtime.InteropServices;

namespace Quelimb.QueryFactory
{
    internal sealed partial class QueryFactoryCache
    {
        private readonly ConcurrentDictionary<CacheKey, CacheItem> _dic = new ConcurrentDictionary<CacheKey, CacheItem>();
        [ThreadStatic] private static HashAndConstantCollector? s_collector;

        public FormattableString CreateQueryString(Expression expression)
        {
            Check.NotNull(expression, nameof(expression));

            var collector = s_collector ?? (s_collector = new HashAndConstantCollector());

            try
            {
                TreeWalker.Walk(expression, collector);
                var hashCode = collector.Hasher.ToHashCode();

                if (!this._dic.TryGetValue(new ExpressionKey(expression, hashCode), out var cacheItem))
                {
                    // Compile the expression tree! (slow path)
                    var dlg = this.CreateDelegate(expression);
                    var serialized = TreeSerializer.Serialize(expression);

                    cacheItem = new CacheItem(serialized, hashCode, dlg);
                    cacheItem = this._dic.GetOrAdd(new SerializedKey(cacheItem), cacheItem);
                }

                return cacheItem.CompiledDelegate(collector.Constants);
            }
            finally
            {
                collector.Clear();
            }
        }

        private Func<IReadOnlyList<object>, FormattableString> CreateDelegate(Expression expression)
        {
            throw new NotImplementedException(); // TODO
        }

        internal sealed class HashAndConstantCollector : ITreeTraversalReporter
        {
            public HashCode Hasher;
            public readonly List<object> Constants = new List<object>();

            public void Clear()
            {
                this.Hasher = new HashCode();
                this.Constants.Clear();
            }

            bool ITreeTraversalReporter.OnData(ReadOnlySpan<byte> data)
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

            void ITreeTraversalReporter.OnObjectConstant(ConstantExpression expression)
            {
                this.Constants.Add(expression.Value);
            }

            public static int CalculateHashCode(Expression expression)
            {
                var self = new HashAndConstantCollector();
                TreeWalker.Walk(expression, self);
                return self.Hasher.ToHashCode();
            }
        }

        internal sealed class TreeSerializer : ITreeTraversalReporter
        {
            private const int MinBufferSize = 256; // TODO: Optimize for use cases
            private byte[]? _buffer;
            private int _position;

            bool ITreeTraversalReporter.OnData(ReadOnlySpan<byte> data)
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

            void ITreeTraversalReporter.OnObjectConstant(ConstantExpression expression)
            {
            }

            public static ImmutableArray<byte> Serialize(Expression expression)
            {
                var self = new TreeSerializer();
                TreeWalker.Walk(expression, self);

                return self._buffer == null
                    ? ImmutableArray<byte>.Empty
                    : ImmutableArray.Create(self._buffer, 0, self._position);
            }
        }
    }
}
