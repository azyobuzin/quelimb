using System.Text;
using Microsoft.Extensions.ObjectPool;
using Quelimb.SqlGenerators;
using Quelimb.TableMappers;

namespace Quelimb
{
    public class QueryEnvironment
    {
        private static QueryEnvironment? s_default;
        public static QueryEnvironment Default => s_default ?? (s_default = new QueryEnvironment(null, null, null, null));

        public ObjectPool<StringBuilder> StringBuilderPool { get; }
        public SqlGenerator SqlGenerator { get; }
        public TableMapperProvider TableMapperProvider { get; }
        public ValueConverter ValueConverter { get; }

        public QueryEnvironment(
            ObjectPool<StringBuilder>? stringBuilderPool,
            SqlGenerator? sqlGenerator,
            TableMapperProvider? tableMapperProvider,
            ValueConverter? valueConverter)
        {
            this.StringBuilderPool = stringBuilderPool ?? new DefaultObjectPoolProvider().CreateStringBuilderPool();
            this.SqlGenerator = sqlGenerator ?? SqlGenerator.Instance;
            this.TableMapperProvider = tableMapperProvider ?? DefaultTableMapperProvider.Default;
            this.ValueConverter = valueConverter ?? DefaultValueConverter.Default;
        }
    }
}
