using System;
using System.Data;
using System.Linq.Expressions;

namespace Quelimb
{
    public class QueryBuilder
    {
        private static QueryBuilder? s_default;
        public static QueryBuilder Default => s_default ?? (s_default = new QueryBuilder(QueryEnvironment.Default));

        protected internal QueryEnvironment Environment { get; }

        public QueryBuilder(QueryEnvironment environment)
        {
            this.Environment = environment;
        }

        public UntypedQuery Query(string query)
        {
            Check.NotNullOrEmpty(query, nameof(query));

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

        public UntypedQuery Query<T1, T2>(Expression<Func<T1, T2, FormattableString>> queryFactory)
        {
            return this.QueryCore(queryFactory);
        }

        public UntypedQuery Query<T1, T2, T3>(Expression<Func<T1, T2, T3, FormattableString>> queryFactory)
        {
            return this.QueryCore(queryFactory);
        }

        public UntypedQuery Query<T1, T2, T3, T4>(Expression<Func<T1, T2, T3, T4, FormattableString>> queryFactory)
        {
            return this.QueryCore(queryFactory);
        }

        public UntypedQuery Query<T1, T2, T3, T4, T5>(Expression<Func<T1, T2, T3, T4, T5, FormattableString>> queryFactory)
        {
            return this.QueryCore(queryFactory);
        }

        public UntypedQuery Query<T1, T2, T3, T4, T5, T6>(Expression<Func<T1, T2, T3, T4, T5, T6, FormattableString>> queryFactory)
        {
            return this.QueryCore(queryFactory);
        }

        public UntypedQuery Query<T1, T2, T3, T4, T5, T6, T7>(Expression<Func<T1, T2, T3, T4, T5, T6, T7, FormattableString>> queryFactory)
        {
            return this.QueryCore(queryFactory);
        }

        public UntypedQuery Query<T1, T2, T3, T4, T5, T6, T7, T8>(Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, FormattableString>> queryFactory)
        {
            return this.QueryCore(queryFactory);
        }

        public UntypedQuery Query<T1, T2, T3, T4, T5, T6, T7, T8, T9>(Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, FormattableString>> queryFactory)
        {
            return this.QueryCore(queryFactory);
        }

        public UntypedQuery Query<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, FormattableString>> queryFactory)
        {
            return this.QueryCore(queryFactory);
        }

        public UntypedQuery Query<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, FormattableString>> queryFactory)
        {
            return this.QueryCore(queryFactory);
        }

        public UntypedQuery Query<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, FormattableString>> queryFactory)
        {
            return this.QueryCore(queryFactory);
        }

        public UntypedQuery Query<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, FormattableString>> queryFactory)
        {
            return this.QueryCore(queryFactory);
        }

        public UntypedQuery Query<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, FormattableString>> queryFactory)
        {
            return this.QueryCore(queryFactory);
        }

        public UntypedQuery Query<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, FormattableString>> queryFactory)
        {
            return this.QueryCore(queryFactory);
        }

        public UntypedQuery Query<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, FormattableString>> queryFactory)
        {
            return this.QueryCore(queryFactory);
        }

        private UntypedQuery QueryCore(LambdaExpression queryFactory)
        {
            Check.NotNull(queryFactory, nameof(queryFactory));

            var fs = QueryAnalyzer.ExtractFormattableString(queryFactory, this.Environment);
            Action<IDbCommand> setup = cmd => QueryAnalyzer.SetQueryToDbCommand(fs, cmd, this.Environment);
            return new UntypedQuery(this.Environment, setup);
        }
    }
}
