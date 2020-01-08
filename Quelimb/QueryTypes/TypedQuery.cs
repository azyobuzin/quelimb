using System;
using System.Collections.Immutable;
using System.Data;
using Dawn;

namespace Quelimb
{
    public class TypedQuery<TRecord>
    {
        protected ImmutableArray<StringOrFormattableString> QueryStrings { get; }
        protected Func<IDataRecord, TRecord> RecordConverter { get; }

        public TypedQuery(ImmutableArray<StringOrFormattableString> queryStrings, Func<IDataRecord, TRecord> recordConverter)
        {
            Guard.Argument(queryStrings, nameof(queryStrings)).Require(!queryStrings.IsDefaultOrEmpty, _ => "queryStrings is empty.");
            Guard.Argument(recordConverter, nameof(recordConverter)).NotNull();

            this.QueryStrings = queryStrings;
            this.RecordConverter = recordConverter;
        }

        public TypedQuery<T> Map<T>(Func<TRecord, T> mapper)
        {
            var recordConverter = this.RecordConverter;
            return new TypedQuery<T>(this.QueryStrings, x => mapper(recordConverter(x)));
        }
    }
}
