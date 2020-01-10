using System;
using System.Data;
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

        public UntypedQuery Query(string query)
        {
            Guard.Argument(query, nameof(query)).NotNull().NotEmpty();

            Action<IDbCommand> setup = cmd => cmd.CommandText = query;
            return new UntypedQuery(this.Environment, setup);
        }

        public UntypedQuery Query(Expression<Func<FormattableString>> queryFactory)
        {
            return this.QueryCore(queryFactory);
        }

        public UntypedQuery Query<T1>(Expression<Func<T1, FormattableString>> queryFactory)
        {
            return this.QueryCore(queryFactory);
        }

        private UntypedQuery QueryCore(LambdaExpression queryFactory)
        {
            Guard.Argument(queryFactory, nameof(queryFactory)).NotNull();

            var fs = QueryAnalyzer.ExtractFormattableString(queryFactory, this.Environment);
            Action<IDbCommand> setup = cmd => QueryAnalyzer.SetQueryToDbCommand(fs, cmd, this.Environment);
            return new UntypedQuery(this.Environment, setup);
        }
    }
}
