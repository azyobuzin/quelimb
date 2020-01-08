using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Dawn;
using Quelimb.TableMappers;

namespace Quelimb
{
    internal static class QueryAnalyzer
    {
        internal static FormattableString ExtractFormattableString(LambdaExpression lambda, QueryEnvironment environment)
        {
            Guard.Argument(lambda, nameof(lambda)).NotNull();
            Guard.Argument(environment, nameof(environment)).NotNull();

            var tableDictionary = new Dictionary<ParameterExpression, TableReference>();
            foreach (var parameter in lambda.Parameters)
            {
                var tableMapper = environment.TableMapperProvider.GetTableByType(parameter.Type);
                if (tableMapper == null)
                    throw new ArgumentException($"A parameter `{parameter}` cannot be resolved as a table.", nameof(lambda));

                tableDictionary.Add(parameter, new TableReference(tableMapper));
            }

            var rewritedExpr = new RewriteTableReferenceVisitor(tableDictionary).Visit(lambda.Body);
            var fs = Expression.Lambda<Func<FormattableString>>(rewritedExpr).Compile()();

            if (fs == null) throw new InvalidOperationException("The lambda expression returned null.");

            ResolveTableAliases(fs);
            return fs;
        }

        private static void ResolveTableAliases(FormattableString query)
        {
            var matches = Regex.Matches(
                query.Format,
                @"(?<!\{)\{\d*([0-9]+)\s*:\s*AS(?!\w)\s*((?:[^\}]|\}\})+)\}",
                RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant);

            var arguments = query.GetArguments();

            foreach (Match match in matches)
            {
                var argIndex = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                if (arguments[argIndex] is TableReference tableRef)
                {
                    if (tableRef.EscapedAlias != null)
                        throw new FormatException($"AS specifier for {tableRef.TableMapper.TableName} is appeared multiple times.");
                    tableRef.EscapedAlias = match.Groups[2].Value;
                }
            }
        }

        internal static void SetQueryToDbCommand(FormattableString query, IDbCommand command, QueryEnvironment environment)
        {
            Guard.Argument(query, nameof(query)).NotNull().NotEmpty();
            Guard.Argument(command, nameof(command)).NotNull();
            Guard.Argument(environment, nameof(environment)).NotNull();

            var sb = environment.StringBuilderPool.Get();
            try
            {
                // TODO
            }
            finally
            {
                environment.StringBuilderPool.Return(sb);
            }
        }

        internal sealed class TableReference
        {
            public TableMapper TableMapper { get; }
            public string? EscapedAlias { get; set; }

            public TableReference(TableMapper tableMapper)
            {
                this.TableMapper = tableMapper;
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

        private sealed class RewriteTableReferenceVisitor : ExpressionVisitor
        {
            private static readonly MethodInfo s_createFormattableStringMethod = typeof(FormattableStringFactory)
                .GetTypeInfo()
                .GetRuntimeMethod(nameof(FormattableStringFactory.Create), new[] { typeof(string), typeof(object[]) });

            private readonly IReadOnlyDictionary<ParameterExpression, TableReference> _tableDictionary;

            public RewriteTableReferenceVisitor(IReadOnlyDictionary<ParameterExpression, TableReference> tableDictionary)
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
                        var columnName = tableRef.TableMapper.GetColumnNameByMemberInfo(memberExpr.Member);
                        if (columnName == null) throw new InvalidOperationException(memberExpr.Member.ToString() + " could not be resolved as a column.");
                        return Expression.Convert(
                            Expression.Constant(new ColumnReference(tableRef, columnName)),
                            typeof(object));
                    }
                }

                return this.Visit(argument);
            }
        }
    }
}
