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
                (t1, t2) => $"SELECT {t1:COLUMNS} FROM {t1:AS \"T1\"}, {t2} WHERE {t1.Id} = {t2.Id} AND {t1.ColumnName} = {10,2:X}";

            var environment = QueryEnvironment.Default;
            var fs = QueryAnalyzer.ExtractFormattableString(expr, environment);
            fs.Format.Is("SELECT {0:COLUMNS} FROM {1:AS \"T1\"}, {2} WHERE {3} = {4} AND {5} = {6,2:X}");

            var arguments = fs.GetArguments();
            var arg0 = arguments[0]!.IsInstanceOf<QueryAnalyzer.TableReference>("arguments[0] is a TableReference");
            arg0.TableMapper.TableName.Is(nameof(Table1));
            arg0.EscapedAlias.Is("\"T1\"");

            var arg1 = arguments[1]!.IsInstanceOf<QueryAnalyzer.TableReference>("arguments[1] is a TableReference");
            arg1.IsSameReferenceAs(arg0, "arguments[0] == arguments[1]");

            var arg2 = arguments[2]!.IsInstanceOf<QueryAnalyzer.TableReference>("arguments[2] is a TableReference");
            arg2.TableMapper.TableName.Is("TableTwo");
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

            command.CommandText.Is(@"SELECT ""T1"".""Id"", ""T1"".""FooColumn"", ""T1"".""IntField"" FROM ""Table1"" AS ""T1"", ""TableTwo"" WHERE ""T1"".""Id"" = ""TableTwo"".""Id"" AND ""T1"".""FooColumn"" = ?");
            command.Parameters.Count.Is(1, "The command has 1 parameter");
            command.Parameters[0].Value.Is(" A", "10 in hexadecimal and padding");
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
    }
}
