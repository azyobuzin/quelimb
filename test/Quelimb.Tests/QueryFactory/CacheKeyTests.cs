using System;
using System.Linq.Expressions;
using ChainingAssertion;
using Xunit;
using static Quelimb.QueryFactory.QueryFactoryCache;

namespace Quelimb.Tests.QueryFactory
{
    public class CacheKeyTests
    {
        [Fact]
        public void SameExpression()
        {
            static Expression<Func<int>> CreateExpression(int x, int y)
            {
                return () => x + y;
            }

            var leftExpr = CreateExpression(1, 2);
            var rightExpr = CreateExpression(3, 4);

            var leftCollector = new HashAndConstantCollector();
            leftCollector.Walk(leftExpr);
            var leftHashCode = leftCollector.Hasher.ToHashCode();

            var rightCollector = new HashAndConstantCollector();
            rightCollector.Walk(rightExpr);
            var rightHashCode = rightCollector.Hasher.ToHashCode();

            // The two of ObjectConstants have the same number of elements, but the elements are different instances.
            // The expression read the fields of the closure environment object twice.
            leftCollector.ObjectConstants.Count.Is(2);
            rightCollector.ObjectConstants.Count.Is(2);
            Assert.NotEqual(leftCollector.ObjectConstants, rightCollector.ObjectConstants);

            var leftExprKey = new ExpressionKey(leftExpr, leftHashCode);
            var leftSerializedKey = new SerializedKey(TreeSerializer.Serialize(leftExpr), leftHashCode);
            var rightExprKey = new ExpressionKey(rightExpr, rightHashCode);
            var rightSerializedKey = new SerializedKey(TreeSerializer.Serialize(rightExpr), rightHashCode);

            void AssertEqual(CacheKey left, CacheKey right)
            {
                Assert.Equal(left.GetHashCode(), right.GetHashCode());
                left.Equals(right).IsTrue();
                right.Equals(left).IsTrue();
            }

            AssertEqual(leftSerializedKey, rightSerializedKey);
            AssertEqual(leftExprKey, rightSerializedKey);
            AssertEqual(leftSerializedKey, rightExprKey);

            Assert.Throws<NotSupportedException>(() => leftExprKey.Equals(rightExprKey));
            Assert.Throws<NotSupportedException>(() => rightExprKey.Equals(leftExprKey));
        }

        [Fact]
        public void DifferentExpression()
        {
            Expression<Func<int>> leftExpr = () => 1 + 2;
            Expression<Func<int>> rightExpr = () => 3 + 4;

            var leftCollector = new HashAndConstantCollector();
            leftCollector.Walk(leftExpr);
            var leftHashCode = leftCollector.Hasher.ToHashCode();

            var rightCollector = new HashAndConstantCollector();
            rightCollector.Walk(rightExpr);
            var rightHashCode = rightCollector.Hasher.ToHashCode();

            // ObjectConstants are empty because no variable is captured.
            leftCollector.ObjectConstants.IsEmpty();
            rightCollector.ObjectConstants.IsEmpty();

            var leftExprKey = new ExpressionKey(leftExpr, leftHashCode);
            var leftSerializedKey = new SerializedKey(TreeSerializer.Serialize(leftExpr), leftHashCode);
            var rightExprKey = new ExpressionKey(rightExpr, rightHashCode);
            var rightSerializedKey = new SerializedKey(TreeSerializer.Serialize(rightExpr), rightHashCode);

            void AssertNotEqual(CacheKey left, CacheKey right)
            {
                left.Equals(right).IsFalse();
                right.Equals(left).IsFalse();
            }

            AssertNotEqual(leftSerializedKey, rightSerializedKey);
            AssertNotEqual(leftExprKey, rightSerializedKey);
            AssertNotEqual(leftSerializedKey, rightExprKey);
        }
    }
}
