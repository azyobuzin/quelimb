using Dawn;
using Quelimb.SqlGenerators;
using Quelimb.Converters;

namespace Quelimb
{
    public class QueryEnvironment
    {
        public ISqlGenerator Generator { get; }
        public IConverterProvider RecordConverterProvider { get; }

        public QueryEnvironment(ISqlGenerator generator, IConverterProvider recordConverterProvider)
        {
            Guard.Argument(generator, nameof(generator)).NotNull();
            Guard.Argument(recordConverterProvider, nameof(recordConverterProvider)).NotNull();

            this.Generator = generator;
            this.RecordConverterProvider = recordConverterProvider;
        }
    }
}
