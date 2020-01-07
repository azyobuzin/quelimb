using System;
using System.Collections.Immutable;
using Dawn;

namespace Quelimb
{
    public class UntypedQuery
    {
        protected ImmutableArray<FormattableString> QueryStrings { get; }

        public UntypedQuery(ImmutableArray<FormattableString> queryStrings)
        {
            Guard.Argument(queryStrings, nameof(queryStrings)).Require(!queryStrings.IsDefaultOrEmpty, _ => "queryStrings is empty.");
            this.QueryStrings = queryStrings;
        }

        public UntypedQuery(FormattableString query)
        {
            Guard.Argument(query, nameof(query)).NotNull();
            this.QueryStrings = ImmutableArray.Create(query);
        }

        public TypedQuery<T> Map<T>()
        {
            throw new NotImplementedException();
        }

        public TypedQuery<TResult> Map<T1, T2, TResult>(Func<T1, T2, TResult> mapper)
        {
            throw new NotImplementedException();
        }
    }
}
