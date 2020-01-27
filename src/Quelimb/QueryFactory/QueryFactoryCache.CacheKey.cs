using System;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;

namespace Quelimb.QueryFactory
{
    partial class QueryFactoryCache
    {
        internal abstract class CacheKey
        {
            public override abstract int GetHashCode();

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(this, obj)) return true;

                return (this, obj) switch
                {
                    (_, null) => false,
                    (SerializedKey x, SerializedKey y) => SerializedVsSerialized(x, y),
                    (SerializedKey x, ExpressionKey y) => SerializedVsExpression(x, y),
                    (ExpressionKey x, SerializedKey y) => SerializedVsExpression(y, x),
                    _ => throw new NotSupportedException(),
                };
            }

            /// <summary>
            /// Returns <see langword="true"/> if the byte arrays are equal.
            /// </summary>
            private static bool SerializedVsSerialized(SerializedKey left, SerializedKey right)
            {
                return left.SerializedTree.AsSpan().SequenceEqual(right.SerializedTree.AsSpan());
            }

            /// <summary>
            /// Returns <see langword="true"/> if the expression tree of <paramref name="right"/> is equal to
            /// another one which the byte array of <paramref name="left"/> represents.
            /// </summary>
            private static bool SerializedVsExpression(SerializedKey left, ExpressionKey right)
            {
                return TreeWalker.Walk(right.Expression, new CompareWithSerialized(left.SerializedTree));
            }

            private sealed class CompareWithSerialized : ITreeTraversalReporter
            {
                private ImmutableArray<byte> _serialized;
                private int _position;

                public CompareWithSerialized(ImmutableArray<byte> serialized)
                {
                    this._serialized = serialized;
                }

                public bool OnData(ReadOnlySpan<byte> data)
                {
                    var enoughLength = this._serialized.Length - this._position >= data.Length;
                    var dataEqual = enoughLength &&
                        this._serialized.AsSpan()
                            .Slice(this._position, data.Length)
                            .SequenceEqual(data);
                    if (!dataEqual) return false;

                    this._position += data.Length;
                    return true;
                }

                public void OnObjectConstant(ConstantExpression expression)
                {
                }
            }
        }

        internal sealed class SerializedKey : CacheKey
        {
            private readonly CacheItem _inner;

            public SerializedKey(CacheItem inner)
            {
                this._inner = inner;
            }

            public ImmutableArray<byte> SerializedTree => this._inner.SerializedTree;

            public override int GetHashCode() => this._inner.HashCode;
        }

        internal sealed class ExpressionKey : CacheKey
        {
            public Expression Expression { get; }
            private readonly int _hashCode;

            public ExpressionKey(Expression expression, int hashCode)
            {
                this.Expression = expression;
                this._hashCode = hashCode;
            }

            public override int GetHashCode() => this._hashCode;
        }
    }
}
