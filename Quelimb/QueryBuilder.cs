using System;
using System.Linq.Expressions;
using Dawn;

namespace Quelimb
{
    public class QueryBuilder
    {
        protected internal QueryEnvironment Environment { get; }

        public QueryBuilder(QueryEnvironment environment)
        {
            this.Environment = environment;
        }

        public UntypedQuery QueryS(string query)
        {
            Guard.Argument(query, nameof(query)).NotNull().NotEmpty();
            return new UntypedQuery(new StringOrFormattableString(query));
        }

        public UntypedQuery QueryF(FormattableString query)
        {
            Guard.Argument(query, nameof(query)).NotNull();
            return new UntypedQuery(new StringOrFormattableString(query));
        }

        public UntypedQuery QueryF<T1>(Expression<Func<T1, FormattableString>> queryFactory)
        {
            Guard.Argument(queryFactory, nameof(queryFactory)).NotNull();
            throw new NotImplementedException();
        }
    }
}
