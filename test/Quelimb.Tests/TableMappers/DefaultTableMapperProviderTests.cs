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
            var tableMapper = DefaultTableMapperProvider.Default.GetTableByType(typeof(Table1));

            tableMapper.TableName.Is(nameof(Table1), "TableName must be the class name");

            tableMapper.GetColumnsNamesForSelect()
                .ToHashSet()
                .SetEquals(new[]
                {
                    nameof(Table1.Id),
                    "FooColumn",
                    nameof(Table1.IntField),
                    // PrivateField is excluded
                    // Excluded is excluded
                })
                .IsTrue("GetColumnsNamesForSelect returns public members of Table1");
        }

        [Fact]
        public void TableAttribute()
        {
            var tableMapper = DefaultTableMapperProvider.Default.GetTableByType(typeof(Table2));
            tableMapper.TableName.Is("TableTwo", "TableName must be the name specified in TableAttribute");
        }
    }
}
