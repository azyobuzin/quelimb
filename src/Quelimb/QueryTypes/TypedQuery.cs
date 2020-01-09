using System;
using System.Collections.Immutable;
using System.Data;
using Dawn;

namespace Quelimb
{
    public class TypedQuery<TRecord> : UntypedQuery
    {
        protected Func<IDataRecord, TRecord> RecordConverter { get; }

        public TypedQuery(ImmutableArray<StringOrFormattableString> queryStrings, Func<IDataRecord, TRecord> recordConverter)
            : base(queryStrings)
        {
            Guard.Argument(recordConverter, nameof(recordConverter)).NotNull();
            this.RecordConverter = recordConverter;
        }

        public TypedQuery<T> Map<T>(Func<TRecord, T> mapper)
        {
            var recordConverter = this.RecordConverter;
            return new TypedQuery<T>(this.QueryStrings, x => mapper(recordConverter(x)));
        }
    }
}
