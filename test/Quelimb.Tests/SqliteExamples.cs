using System;
using System.Collections.Generic;
using System.Linq;
using ChainingAssertion;
using Microsoft.Data.Sqlite;
using Quelimb.Tests.Models;
using Xunit;

namespace Quelimb.Tests
{
    public class SqliteExamples
    {
        [Fact]
        public void Table1Example()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();

            var qb = QueryBuilder.Default;
            qb.Query<Table1>(
                tbl => $@"CREATE TABLE {tbl} (
                    {tbl.Id:C} INTEGER,
                    {tbl.ColumnName:C} TEXT,
                    {tbl.NullableField:C} INTEGER)")
                .ExecuteNonQuery(connection);

            var testData = new[]
            {
                new Table1()
                {
                    Id = 1,
                    ColumnName = "foo",
                    NullableField = null,
                },
                new Table1()
                {
                    Id = 2,
                    ColumnName = "bar",
                    NullableField = 100,
                }
            };

            foreach (var record in testData)
            {
                qb.Query<Table1>(
                    tbl => $@"INSERT INTO {tbl} ({tbl.Id:C}, {tbl.ColumnName:C}, {tbl.NullableField:C})
                        VALUES ({record.Id}, {record.ColumnName}, {record.NullableField})")
                    .ExecuteNonQuery(connection);
            }

            {
                // SELECT Table1.*
                var asteriskResults =
                    qb.Query<Table1>(tbl => $"SELECT * FROM {tbl} ORDER BY {tbl.Id}")
                       .Map<Table1>()
                       .ExecuteQuery(connection)
                       .ToList();
                asteriskResults.Count.Is(2);
                AssertTable1Data(testData, asteriskResults);
            }

            {
                // SELECT 42, T1.Id, T1.FooColumn, T1.NullableField
                var mixColumnsResults =
                    qb.Query<Table1>(tbl => $"SELECT 42, {tbl:*} FROM {tbl:AS T1} ORDER BY {tbl.Id}")
                        .Map((int i, Table1 record) =>
                        {
                            i.Is(42);
                            return record;
                        })
                        .ExecuteQuery(connection)
                        .ToList();
                mixColumnsResults.Count.Is(2);
                AssertTable1Data(testData, mixColumnsResults);
            }

            {
                // Tuple
                var tupleResults =
                    qb.Query<Table1>(tbl => $"SELECT {tbl.Id}, {tbl.NullableField} FROM {tbl} ORDER BY {tbl.Id}")
                        .Map<Tuple<int, int?>>()
                        .ExecuteQuery(connection)
                        .ToList();
                tupleResults.Count.Is(2);
                tupleResults[0].Is(Tuple.Create(1, (int?)null));
                tupleResults[1].Is(Tuple.Create(2, (int?)100));
            }
        }

        private static void AssertTable1Data(IReadOnlyList<Table1> expectedData, IReadOnlyList<Table1> actualRecords)
        {
            actualRecords.Count.Is(expectedData.Count);

            foreach (var (expected, actual) in expectedData.Zip(actualRecords))
            {
                actual.Id.Is(expected.Id);
                actual.ColumnName.Is(expected.ColumnName);
                actual.NullableField.Is(expected.NullableField);
            }
        }
    }
}
