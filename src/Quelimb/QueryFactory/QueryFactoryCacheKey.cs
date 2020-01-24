using System;
using System.Linq.Expressions;

namespace Quelimb.QueryFactory
{
    internal abstract class QueryFactoryCacheKey
    {
        public abstract int HashCode { get; }

        public override int GetHashCode()
        {
            return this.HashCode;
        }

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

        private static bool SerializedVsSerialized(SerializedKey left, SerializedKey right)
        {
            return left.SerializedTree.Span.SequenceEqual(right.SerializedTree.Span);
        }

        private static bool SerializedVsExpression(SerializedKey left, ExpressionKey right)
        {
            return TreeWalker.Walk(right.Expression, new CompareWithSerialized(left.SerializedTree));
        }

        private sealed class CompareWithSerialized : ITreeTraversalReporter
        {
            private ReadOnlyMemory<byte> _serialized;

            public CompareWithSerialized(ReadOnlyMemory<byte> serialized)
            {
                this._serialized = serialized;
            }

            public bool OnData(ReadOnlySpan<byte> data)
            {
                var thisSpan = this._serialized.Span;
                var match = thisSpan.Length >= data.Length &&
                    thisSpan.Slice(0, data.Length).SequenceEqual(data);
                if (!match) return false;

                this._serialized = this._serialized.Slice(data.Length);
                return true;
            }

            public void OnObjectConstant(ConstantExpression expression)
            {
            }
        }
    }

    internal sealed class SerializedKey : QueryFactoryCacheKey
    {
        private readonly QueryFactoryCacheItem _inner;

        public SerializedKey(QueryFactoryCacheItem inner)
        {
            this._inner = inner;
        }

        public ReadOnlyMemory<byte> SerializedTree => this._inner.SerializedTree;
        public override int HashCode => this._inner.HashCode;
    }

    internal sealed class ExpressionKey : QueryFactoryCacheKey
    {
        public Expression Expression { get; }
        public override int HashCode { get; }

        public ExpressionKey(Expression expression, int hashCode)
        {
            this.Expression = expression;
            this.HashCode = hashCode;
        }
    }
}
