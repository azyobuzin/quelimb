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

        public TableQuery<T> From<T>(string alias = null, bool only = false)
        {
            var tableName = this.Environment.TableInfoProvider.GetTableInfoByType(typeof(T)).TableName;
            return new TableQuery<T>(this.Environment, new FromClause(typeof(T), tableName, alias, only));
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
