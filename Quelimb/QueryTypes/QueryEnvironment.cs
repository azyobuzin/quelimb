using Dawn;
using Quelimb.SqlGenerators;
using Quelimb.TableInfoProviders;

namespace Quelimb.QueryTypes
{
    public class QueryEnvironment
    {
        public ISqlGenerator Generator { get; }
        public ITableInfoProvider TableInfoProvider { get; }

        public QueryEnvironment(ISqlGenerator generator, ITableInfoProvider tableInfoProvider)
        {
            Guard.Argument(generator, nameof(generator)).NotNull();
            Guard.Argument(tableInfoProvider, nameof(tableInfoProvider)).NotNull();

            this.Generator = generator;
            this.TableInfoProvider = tableInfoProvider;
        }
    }
}
