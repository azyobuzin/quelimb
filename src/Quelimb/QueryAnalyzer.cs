using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Dawn;
using Microsoft.Extensions.ObjectPool;
using Quelimb.TableMappers;

namespace Quelimb
{
    internal static class QueryAnalyzer
    {
        private static readonly Regex s_tableAliasFormatStringPattern = new Regex(
            @"^\s*(AS(?!\w)\s*.+)",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant);

        private static readonly Regex s_tableAliasFormatItemPattern = new Regex(
            @"(?<!\{)\{([0-9]+)\s*:\s*AS(?!\w)\s*((?:[^\}]|\}\})+)\}",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant);

        private static readonly Regex s_formatItemPattern = new Regex(
            @"^\s*([0-9]+)\s*((?:,\s*(\-?[0-9]+)\s*)?(?:\:((?:[^\}]|\}\})*))?)\}",
            RegexOptions.Singleline | RegexOptions.CultureInvariant);

        private static readonly char[] s_brackets = { '{', '}' };

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
            var matches = s_tableAliasFormatItemPattern.Matches(query.Format);
            object?[] arguments = query.GetArguments();

            foreach (Match match in matches)
            {
                var argIndex = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                if ((uint)argIndex >= (uint)arguments.Length)
                    throw new FormatException($"Invalid format index {argIndex}.");

                if (arguments[argIndex] is TableReference tableRef)
                {
                    var alias = match.Groups[2].Value;
                    if (tableRef.EscapedAlias != null && tableRef.EscapedAlias != alias)
                        throw new FormatException($"Cannot use alias {alias} for {tableRef.TableMapper} because you have set {tableRef.EscapedAlias} as the alias.");
                    tableRef.EscapedAlias = alias;
                }
            }
        }

        internal static void SetQueryToDbCommand(FormattableString query, IDbCommand command, QueryEnvironment environment)
        {
            Guard.Argument(query, nameof(query)).NotNull().NotEmpty();
            Guard.Argument(command, nameof(command)).NotNull();
            Guard.Argument(environment, nameof(environment)).NotNull();

            var formatStr = query.Format;

            if (query.ArgumentCount == 0)
            {
                command.CommandText = UnescapeBrackets(formatStr, environment.StringBuilderPool);
                return;
            }

            var sb = environment.StringBuilderPool?.Get() ?? new StringBuilder();
            try
            {
                object?[] arguments = query.GetArguments();
                var lastIndex = 0;
                var parameterCount = 0;
                for (var i = 0; i < formatStr.Length;)
                {
                    var c = formatStr[i];
                    switch (c)
                    {
                        case '{':
                            if (i + 1 >= formatStr.Length) throw new FormatException("Unexpected EOS.");
                            if (formatStr[i + 1] == '{')
                            {
                                // {{
                                sb.Append(formatStr, lastIndex, i + 1 - lastIndex);
                                lastIndex = i += 2;
                            }
                            else
                            {
                                sb.Append(formatStr, lastIndex, i - lastIndex);

                                var formatItemMatch = s_formatItemPattern.Match(formatStr, i + 1, formatStr.Length - i - 1);
                                if (!formatItemMatch.Success) throw new FormatException("Invalid format item.");

                                var argIndex = int.Parse(formatItemMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                                if ((uint)argIndex >= (uint)arguments.Length)
                                    throw new FormatException($"Invalid format index {argIndex}.");

                                var arg = arguments[argIndex];
                                switch (arg)
                                {
                                    case TableReference tableRef:
                                        if (formatItemMatch.Groups[3].Success)
                                            throw new FormatException("Cannot specify alignment to a table reference.");
                                        WriteTableReference(tableRef, formatItemMatch.Groups[4].Value, sb, environment);
                                        break;

                                    case ColumnReference columnRef:
                                        if (formatItemMatch.Groups[3].Success)
                                            throw new FormatException("Cannot specify alignment to a column reference.");
                                        WriteColumnReference(columnRef, formatItemMatch.Groups[4].Value, sb, environment);
                                        break;

                                    case RawQuery rawQuery:
                                        sb.Append(rawQuery.Content);
                                        break;

                                    default:
                                        // TODO: Support IEnumerable

                                        if (formatItemMatch.Groups[2].Length > 0)
                                        {
                                            // If format is specified, the argument will become a formatted string.
                                            var itemFormat = formatItemMatch.Groups[2].Value;
                                            arg = string.Format(environment.FormatProvider, "{0" + itemFormat + "}", arg);
                                        }

                                        // Add IDbDataParameter to `command`
                                        var parameter = command.CreateParameter();
                                        parameter.ParameterName = environment.SqlGenerator.AddParameterToQuery(parameterCount++, sb);
                                        environment.ValueConverter.ConvertTo(arg, parameter);
                                        command.Parameters.Add(parameter);
                                        break;
                                }

                                lastIndex = i = formatItemMatch.Index + formatItemMatch.Length;
                            }
                            break;

                        case '}':
                            // }}
                            if (i + 1 >= formatStr.Length || formatStr[i + 1] != '}')
                                throw new FormatException(@"""}}"" is expected.");
                            sb.Append(formatStr, lastIndex, i + 1 - lastIndex);
                            lastIndex = i += 2;
                            break;

                        default:
                            i++;
                            break;
                    }
                }

                sb.Append(formatStr, lastIndex, formatStr.Length - lastIndex);
                command.CommandText = sb.ToString();
            }
            finally
            {
                environment.StringBuilderPool?.Return(sb);
            }
        }

        private static string UnescapeBrackets(string formatStr, ObjectPool<StringBuilder>? stringBuilderPool)
        {
            var replaceStartIndex = formatStr.IndexOfAny(s_brackets);
            if (replaceStartIndex < 0) return formatStr;

            var sb = stringBuilderPool?.Get().Append(formatStr) ?? new StringBuilder(formatStr);
            sb.Replace("{{", "{", replaceStartIndex, sb.Length - replaceStartIndex);
            sb.Replace("}}", "}", replaceStartIndex, sb.Length - replaceStartIndex);
            var s = sb.ToString();
            stringBuilderPool?.Return(sb);
            return s;
        }

        private static void WriteTableReference(TableReference tableRef, string format, StringBuilder destination, QueryEnvironment environment)
        {
            if (string.IsNullOrEmpty(format))
            {
                // Write table name or alias
                destination.Append(GetEscapedTableNameOrAlias(tableRef, environment));
            }
            else if (format == "*")
            {
                // Write all columns of the table
                var alias = GetEscapedTableNameOrAlias(tableRef, environment);
                var first = true;
                foreach (var columnName in tableRef.TableMapper.GetColumnsNamesForSelect())
                {
                    if (first) first = false;
                    else destination.Append(", ");

                    destination.Append(alias).Append('.');
                    environment.SqlGenerator.EscapeIdentifier(columnName, destination);
                }
            }
            else if (s_tableAliasFormatStringPattern.Match(format) is Match aliasFormatMatch && aliasFormatMatch.Success)
            {
                // AS alias
                environment.SqlGenerator.EscapeIdentifier(tableRef.TableMapper.GetTableName(), destination);
                destination.Append(' ').Append(aliasFormatMatch.Groups[1].Value);
            }
            else
            {
                throw new FormatException("Invalid format for a table reference: " + format);
            }
        }

        private static void WriteColumnReference(ColumnReference columnRef, string format, StringBuilder destination, QueryEnvironment environment)
        {
            var writeTable = true;

            if (!string.IsNullOrEmpty(format))
            {
                writeTable = format switch
                {
                    "T" => true,
                    "C" => false,
                    _ => throw new FormatException("Invalid format for a column reference: " + format),
                };
            }

            if (writeTable)
                destination.Append(GetEscapedTableNameOrAlias(columnRef.Table, environment)).Append('.');

            environment.SqlGenerator.EscapeIdentifier(columnRef.ColumnName, destination);
        }

        private static string GetEscapedTableNameOrAlias(TableReference tableRef, QueryEnvironment environment)
        {
            if (tableRef.EscapedAlias == null)
            {
                // Cache escaped table name
                var tableNameSb = environment.StringBuilderPool?.Get() ?? new StringBuilder();
                environment.SqlGenerator.EscapeIdentifier(tableRef.TableMapper.GetTableName(), tableNameSb);
                tableRef.EscapedAlias = tableNameSb.ToString();
                environment.StringBuilderPool?.Return(tableNameSb);
            }

            return tableRef.EscapedAlias;
        }

        internal sealed class TableReference
        {
            public TableMapper TableMapper { get; }
            public string? EscapedAlias { get; set; }

            public TableReference(TableMapper tableMapper)
            {
                this.TableMapper = tableMapper;
            }

            public override string ToString()
            {
                var s = this.TableMapper.ToString();
                if (this.EscapedAlias != null)
                    s = $"{s} AS {this.EscapedAlias}";
                return s;
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

            public override string ToString()
            {
                return $"ColumnReference({this.ColumnName}, {this.Table})";
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
