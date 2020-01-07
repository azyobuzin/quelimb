using System;
using System.Collections.Immutable;
using Dawn;

namespace Quelimb
{
    public class TypedQuery<TRecord>
    {
        protected ImmutableArray<FormattableString> QueryStrings { get; }
        protected RecordConverter<TRecord> RecordConverter { get; }

        public TypedQuery(ImmutableArray<FormattableString> queryStrings, RecordConverter<TRecord> recordConverter)
        {
            Guard.Argument(queryStrings, nameof(queryStrings)).Require(!queryStrings.IsDefaultOrEmpty, _ => "queryStrings is empty.");
            Guard.Argument(recordConverter, nameof(recordConverter)).NotNull();

            this.QueryStrings = queryStrings;
            this.RecordConverter = recordConverter;
        }
    }
}
