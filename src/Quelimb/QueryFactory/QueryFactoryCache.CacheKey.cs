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
            protected CacheKey(int hashCode)
            {
                this.HashCode = hashCode;
            }

            protected int HashCode { get; }

            public override int GetHashCode() => this.HashCode;

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
                return new CompareWithSerialized(left.SerializedTree).Walk(right.Expression);
            }

            private sealed class CompareWithSerialized : TreeWalker
            {
                private ImmutableArray<byte> _serialized;
                private int _position;

                public CompareWithSerialized(ImmutableArray<byte> serialized)
                {
                    this._serialized = serialized;
                }

                protected override bool OnData(ReadOnlySpan<byte> data)
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
            }
        }

        internal sealed class SerializedKey : CacheKey
        {
            public SerializedKey(ImmutableArray<byte> serializedTree, int hashCode)
                : base(hashCode)
            {
                this.SerializedTree = serializedTree;
            }

            public ImmutableArray<byte> SerializedTree { get; }
        }

        internal sealed class ExpressionKey : CacheKey
        {
            public ExpressionKey(Expression expression, int hashCode)
                : base(hashCode)
            {
                this.Expression = expression;
            }

            public Expression Expression { get; }
        }
    }
}
