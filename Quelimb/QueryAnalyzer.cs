using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Quelimb.Converters;

namespace Quelimb
{
    internal class QueryAnalyzer
    {
        private readonly QueryEnvironment _environment;

        internal QueryAnalyzer(QueryEnvironment environment)
        {
            this._environment = environment;
        }

        internal FormattableString ExtractFormattableString(LambdaExpression lambda)
        {
            var tableDictionary = lambda.Parameters
                .ToDictionary(x => x, x => new TableReference(this._environment.RecordConverterProvider.GetTableInfoByType(x.Type)));
            var rewritedExpr = new RewriteTableReferenceVisitor(tableDictionary).Visit(lambda.Body);
            return Expression.Lambda<Func<FormattableString>>(rewritedExpr).Compile()();
        }

        // TODO: Read from FormattableString

        internal sealed class TableReference
        {
            public ITableInfo TableInfo { get; }
            public string Alias { get; set; }

            public TableReference(ITableInfo tableInfo)
            {
                this.TableInfo = tableInfo;
            }
        }

        internal sealed class ColumnReference
        {
            public TableReference Table { get; }
            public string ColumnName { get; }

            public ColumnReference(TableReference table, string columnName)
            {
                this.Table = table;
                this.ColumnName = columnName;
            }
        }

        internal sealed class RewriteTableReferenceVisitor : ExpressionVisitor
        {
            private static readonly MethodInfo s_createFormattableStringMethod = typeof(FormattableStringFactory)
                .GetTypeInfo()
                .GetRuntimeMethod(nameof(FormattableStringFactory.Create), new[] { typeof(string), typeof(object[]) });

            private readonly Dictionary<ParameterExpression, TableReference> _tableDictionary;

            public RewriteTableReferenceVisitor(Dictionary<ParameterExpression, TableReference> tableDictionary)
            {
                this._tableDictionary = tableDictionary;
            }

            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                if (!Equals(node.Method, s_createFormattableStringMethod))
                    return base.VisitMethodCall(node);

                return node.Update(
                    this.Visit(node.Object),
                    node.Arguments.Select(this.ConvertFormattableStringArgument));
            }

            private Expression ConvertFormattableStringArgument(Expression argument)
            {
                if (argument.NodeType == ExpressionType.Convert && Equals(argument.Type, typeof(object)))
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
                        var columnInfo = tableRef.TableInfo.GetColumnForSelect(memberExpr.Member);
                        if (columnInfo == null) throw new InvalidOperationException(memberExpr.Member.ToString() + " could not be resolved as a column.");
                        return Expression.Convert(
                            Expression.Constant(new ColumnReference(tableRef, columnInfo.ColumnName)),
                            typeof(object));
                    }
                }

                return this.Visit(argument);
            }
        }
    }
}
