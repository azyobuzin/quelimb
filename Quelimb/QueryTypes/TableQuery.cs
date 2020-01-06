using System;
using System.Collections.Immutable;
using Dawn;
using Quelimb.QueryTypes;

namespace Quelimb
{
    public class TableQuery<T1> : TypedQuery<T1>
    {
        protected internal QueryEnvironment Environment { get; }
        protected internal FromClause FromClause { get; }

        public TableQuery(QueryEnvironment environment, FromClause fromClause)
        {
            Guard.Argument(environment, nameof(environment)).NotNull();
            Guard.Argument(fromClause, nameof(fromClause)).NotNull();

            this.Environment = environment;
            this.FromClause = fromClause;
        }

        public TableQuery<T1, T2> FromTable<T2>(string alias = null)
        {
            return new TableQuery<T1, T2>(this, new FromClause(alias));
        }

        public TableQuery<T1, T2> Join<T2>(JoinClause joinClause)
        {
            return new TableQuery<T1, T2>(this, joinClause);
        }
    }

    public class JoinQuery
    {
        protected internal QueryEnvironment Environment { get; }
        protected internal ImmutableList<Tuple<Type, FromOrJoinClause>> TableClauses { get; }

        protected JoinQuery(
            QueryEnvironment environment,
            ImmutableList<Tuple<Type, FromOrJoinClause>> baseTableClauses,
            Type tableType, FromOrJoinClause tableClause)
        {
            Guard.Argument(environment, nameof(environment)).NotNull();
            Guard.Argument(baseTableClauses, nameof(baseTableClauses)).NotNull();
            Guard.Argument(tableType, nameof(tableType)).NotNull();
            Guard.Argument(tableClause, nameof(tableClause)).NotNull();

            this.Environment = environment;
            this.TableClauses = baseTableClauses.Add(Tuple.Create(tableType, tableClause));
        }
    }

    public class TableQuery<T1, T2> : JoinQuery
    {
        public TableQuery(TableQuery<T1> baseQuery, FromOrJoinClause tableClause)
            : base(baseQuery.Environment,
                  ImmutableList.Create(new Tuple<Type, FromOrJoinClause>(typeof(T1), baseQuery.FromClause)),
                  typeof(T2), tableClause)
        { }

        public TableQuery<T1, T2, T3> FromTable<T3>(string alias = null)
        {
            return new TableQuery<T1, T2, T3>(this, new FromClause(alias));
        }

        public TableQuery<T1, T2, T3> Join<T3>(JoinClause joinClause)
        {
            return new TableQuery<T1, T2, T3>(this, joinClause);
        }
    }

    public class TableQuery<T1, T2, T3> : JoinQuery
    {
        public TableQuery(TableQuery<T1, T2> baseQuery, FromOrJoinClause tableClause)
            : base(baseQuery.Environment,
                  baseQuery.TableClauses,
                  typeof(T3), tableClause)
        { }
    }
}
