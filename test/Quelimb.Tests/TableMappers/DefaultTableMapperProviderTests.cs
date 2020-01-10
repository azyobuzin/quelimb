using System;
using System.Linq;
using ChainingAssertion;
using Quelimb.TableMappers;
using Quelimb.Tests.Models;
using Xunit;

namespace Quelimb.Tests.TableMappers
{
    public class DefaultTableMapperProviderTests
    {
        [Fact]
        public void Table1Mapper()
        {
            var tableMapper = DefaultTableMapperProvider.Default.GetTableByType(typeof(Table1))!;

            tableMapper.GetTableName().Is(nameof(Table1), "TableName must be the class name");
            tableMapper.GetColumnCountForSelect().Is(3);

            tableMapper.GetColumnsNamesForSelect()
                .ToHashSet()
                .SetEquals(new[]
                {
                    nameof(Table1.Id),
                    "FooColumn",
                    nameof(Table1.NullableField),
                    // PrivateField is excluded
                    // Excluded is excluded
                })
                .IsTrue("GetColumnsNamesForSelect returns public members of Table1");
        }

        [Fact]
        public void TableAttribute()
        {
            var tableMapper = DefaultTableMapperProvider.Default.GetTableByType(typeof(Table2))!;
            tableMapper.GetTableName().Is("TableTwo", "TableName must be the name specified in TableAttribute");
        }

        [Fact]
        public void TupleMapper()
        {
            var provider = DefaultTableMapperProvider.Default;
            var tupleMapper = provider.GetTableByType(typeof(Tuple<string, int>))!;
            tupleMapper.GetColumnCountForSelect().Is(2);

            var manyTupleMapper = provider.GetTableByType(typeof(Tuple<int, int, int, int, int, int, int, Tuple<int, int>>))!;
            manyTupleMapper.GetColumnCountForSelect().Is(9);
        }

        [Fact]
        public void ValueTupleMapper()
        {
            var provider = DefaultTableMapperProvider.Default;
            var tupleMapper = provider.GetTableByType(typeof(ValueTuple<string, int>))!;
            tupleMapper.GetColumnCountForSelect().Is(2);

            var manyTupleMapper = provider.GetTableByType(typeof(ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int>>))!;
            manyTupleMapper.GetColumnCountForSelect().Is(9);
        }
    }
}
