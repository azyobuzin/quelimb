using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Quelimb.QueryFactory
{
    internal static partial class QueryFactoryCompiler
    {
        public static Func<IReadOnlyList<object>, FormattableString> CreateDelegate(Expression expression, QueryEnvironment environment)
        {
            Check.NotNull(expression, nameof(expression));
            Check.NotNull(environment, nameof(environment));

            throw new NotImplementedException(); // TODO
        }

        private sealed class Rewriter : ExpressionVisitor
        {
            private readonly IReadOnlyDictionary<ParameterExpression, TableReference> _tableDictionary;
            private readonly ParameterExpression _constantsParameter;
            private int _objectConstantIndex;

            public Rewriter(
                IReadOnlyDictionary<ParameterExpression, TableReference> tableDictionary,
                ParameterExpression constantsParameter)
            {
                this._tableDictionary = tableDictionary;
                this._constantsParameter = constantsParameter;
            }

            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                if (!Equals(node.Method, ReflectionUtils.FormattableStringFactoryCreateMethod))
                    return base.VisitMethodCall(node);

                // Rewrite invocation of FormattableStringFactory.Create
                var args = node.Arguments;
                var newArgs = new Expression[args.Count];
                for (var i = 0; i < args.Count; i++)
                {
                    var argExpr = args[i];
                    if (i == 1 && argExpr.NodeType == ExpressionType.NewArrayInit)
                    {
                        var arrayInitExpr = (NewArrayExpression)argExpr;
                        newArgs[i] = arrayInitExpr.Update(
                            arrayInitExpr.Expressions.Select(this.ConvertFormattableStringArgument));
                    }
                    else
                    {
                        newArgs[i] = this.Visit(argExpr);
                    }
                }

                return node.Update(this.Visit(node.Object), newArgs);
            }

            private Expression ConvertFormattableStringArgument(Expression argument)
            {
                if ((argument.NodeType == ExpressionType.Convert || argument.NodeType == ExpressionType.ConvertChecked)
                    && Equals(argument.Type, typeof(object)))
                {
                    var unaryExpr = (UnaryExpression)argument;
                    return unaryExpr.Update(this.ConvertFormattableStringArgument(unaryExpr.Operand));
                }

                {
                    if (argument is ParameterExpression parameterExpr &&
                        this._tableDictionary.TryGetValue(parameterExpr, out var tableRef))
                    {
                        return Expression.Convert(
                            Expression.Constant(tableRef),
                            typeof(object));
                    }
                }

                {
                    if (argument is MemberExpression memberExpr &&
                        memberExpr.Expression is ParameterExpression parameterExpr &&
                        this._tableDictionary.TryGetValue(parameterExpr, out var tableRef))
                    {
                        var columnName = tableRef.Table.GetColumnNameByMemberInfo(memberExpr.Member);
                        if (columnName == null) throw new InvalidOperationException(memberExpr.Member.ToString() + " could not be resolved as a column.");
                        return Expression.Convert(
                            Expression.Constant(new ColumnReference(tableRef, columnName)),
                            typeof(object));
                    }
                }

                return this.Visit(argument);
            }

            protected override Expression VisitConstant(ConstantExpression node)
            {
                var type = node.Type;
                if (Type.GetTypeCode(Nullable.GetUnderlyingType(type) ?? type) != TypeCode.Object)
                    return node;

                // This is an object constant.
                // Rewrite this to an expression reading _constantsParameter.
                var index = this._objectConstantIndex++;
                return Expression.Property(
                    this._constantsParameter,
                    ReflectionUtils.IReadOnlyListObjectItemProperty,
                    Expression.Constant(index));
            }
        }
    }
}
