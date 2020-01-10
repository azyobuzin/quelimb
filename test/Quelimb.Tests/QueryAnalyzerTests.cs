using System;
using System.Linq.Expressions;
using ChainingAssertion;
using Microsoft.Data.Sqlite;
using Quelimb.Tests.Models;
using Xunit;

namespace Quelimb.Tests
{
    public class QueryAnalyzerTests
    {
        [Fact]
        public void BasicExtract()
        {
            Expression<Func<Table1, Table2, FormattableString>> expr =
                (t1, t2) => $"SELECT {t1:*} FROM {t1:AS \"T1\"}, {t2} WHERE {t1.Id} = {t2.Id} AND {t1.ColumnName:C} = {10,2:X}";

            var environment = QueryEnvironment.Default;
            var fs = QueryAnalyzer.ExtractFormattableString(expr, environment);
            fs.Format.Is("SELECT {0:*} FROM {1:AS \"T1\"}, {2} WHERE {3} = {4} AND {5:C} = {6,2:X}");

            var arguments = fs.GetArguments();
            var arg0 = arguments[0]!.IsInstanceOf<QueryAnalyzer.TableReference>("arguments[0] is a TableReference");
            arg0.TableMapper.GetTableName().Is(nameof(Table1));
            arg0.EscapedAlias.Is("\"T1\"");

            var arg1 = arguments[1]!.IsInstanceOf<QueryAnalyzer.TableReference>("arguments[1] is a TableReference");
            arg1.IsSameReferenceAs(arg0, "arguments[0] == arguments[1]");

            var arg2 = arguments[2]!.IsInstanceOf<QueryAnalyzer.TableReference>("arguments[2] is a TableReference");
            arg2.TableMapper.GetTableName().Is("TableTwo");
            arg2.EscapedAlias.IsNull("Table2 has no alias");

            var arg3 = arguments[3]!.IsInstanceOf<QueryAnalyzer.ColumnReference>("arguments[3] is a ColumnReference");
            arg3.Table.IsSameReferenceAs(arg1, "The table of arguments[3] is Table1");
            arg3.ColumnName.Is(nameof(Table1.Id));

            var arg4 = arguments[4]!.IsInstanceOf<QueryAnalyzer.ColumnReference>("arguments[4] is a ColumnReference");
            arg4.Table.IsSameReferenceAs(arg2, "The table of arguemnts[4] is Table2");
            arg4.ColumnName.Is(nameof(Table2.Id));

            var arg5 = arguments[5]!.IsInstanceOf<QueryAnalyzer.ColumnReference>("arguments[5] is a ColumnReference");
            arg5.Table.IsSameReferenceAs(arg1, "The table of arguemnts[5] is Table1");
            arg5.ColumnName.Is("FooColumn");

            arguments[6]!.IsInstanceOf<int>("arguments[6] is a int").Is(10);

            var command = new SqliteCommand();
            QueryAnalyzer.SetQueryToDbCommand(fs, command, environment);

            command.CommandText.Is(@"SELECT ""T1"".""Id"", ""T1"".""FooColumn"", ""T1"".""NullableField"" FROM ""Table1"" AS ""T1"", ""TableTwo"" WHERE ""T1"".""Id"" = ""TableTwo"".""Id"" AND ""FooColumn"" = @QuelimbParam0");
            command.Parameters.Count.Is(1, "The command has 1 parameter");
            var param0 = command.Parameters[0];
            param0.ParameterName.Is("@QuelimbParam0");
            param0.Value.Is(" A", "10 in hexadecimal and padding");
        }

        [Fact]
        public void InvalidFormat()
        {
            var command = new SqliteCommand();
            var environment = QueryEnvironment.Default;

            Expression<Func<Table1, FormattableString>> tableAlign = t1 => $"{t1,1}";
            Assert.Throws<FormatException>(() => RunSetQuery(tableAlign));

            Expression<Func<Table1, FormattableString>> tableInvalidFormat = t1 => $"{t1:INVALID}";
            Assert.Throws<FormatException>(() => RunSetQuery(tableInvalidFormat));

            Expression<Func<Table1, FormattableString>> columnAlign = t1 => $"{t1.Id,1}";
            Assert.Throws<FormatException>(() => RunSetQuery(columnAlign));

            Expression<Func<Table1, FormattableString>> columnInvalidFormat = t1 => $"{t1.Id:INVALID}";
            Assert.Throws<FormatException>(() => RunSetQuery(columnInvalidFormat));

            void RunSetQuery(LambdaExpression expr)
            {
                QueryAnalyzer.SetQueryToDbCommand(QueryAnalyzer.ExtractFormattableString(expr, environment), command, environment);
            }
        }

        [Fact]
        public void NoArgumentQuery()
        {
            var command = new SqliteCommand();
            var environment = QueryEnvironment.Default;

            FormattableString plain = $"SELECT 1";
            QueryAnalyzer.SetQueryToDbCommand(plain, command, environment);
            command.CommandText.Is("SELECT 1");
            command.Parameters.Count.Is(0);

            FormattableString brackets = $"SELECT '{{foo}}{{bar'";
            QueryAnalyzer.SetQueryToDbCommand(brackets, command, environment);
            command.CommandText.Is("SELECT '{foo}{bar'", "Unescape brackets");
            command.Parameters.Count.Is(0);
        }
    }
}
