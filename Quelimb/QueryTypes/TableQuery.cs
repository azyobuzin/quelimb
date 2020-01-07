using System;
using System.Collections.Immutable;
using System.Linq.Expressions;
using Dawn;

namespace Quelimb
{
    public abstract class TableQuery
    {
        protected internal QueryEnvironment Environment { get; }
        protected internal ImmutableList<Tuple<Type, FromOrJoinClause>> TableClauses { get; }

        protected TableQuery(
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

        /*
        public TypedQuery<TRecord> Select<TRecord>(FormattableString selectClause, FormattableString afterFromClause = null)
        {
            Guard.Argument(afterFromClause, nameof(afterFromClause)).NotNull();

            var builder = ImmutableArray.CreateBuilder<StringOrFormattableString>();
            builder.Add(new StringOrFormattableString("SELECT "));
            builder.Add(new StringOrFormattableString(selectClause));
            builder.Add(new StringOrFormattableString(" FROM "));
            this.CreateTableReferences(builder);

            if (afterFromClause != null)
            {
                builder.Add(new StringOrFormattableString(" "));
                builder.Add(new StringOrFormattableString(afterFromClause));
            }

            return new TypedQuery<TRecord>(builder.ToImmutable(), null); // TODO: recordConverter
        }
        */

        private void CreateTableReferences(ImmutableArray<StringOrFormattableString>.Builder sb)
        {
            // FROM
            var first = true;
            foreach (var table in this.TableClauses)
            {
                if (!(table.Item2 is FromClause)) continue;

                if (first) first = false;
                else sb.Add(new StringOrFormattableString(", "));

                sb.AddRange(table.Item2.CreateSql(this.Environment.Generator));
            }

            // JOIN
            foreach (var table in this.TableClauses)
            {
                if (table.Item2 is FromClause) continue;

                sb.Add(new StringOrFormattableString(" "));
                sb.AddRange(table.Item2.CreateSql(this.Environment.Generator));
            }
        }
    }

    public class TableQuery<T1> : TableQuery
    {
        public TableQuery(QueryEnvironment environment, FromClause fromClause)
            : base(environment, ImmutableList<Tuple<Type, FromOrJoinClause>>.Empty, typeof(T1), fromClause)
        {
        }

        public TableQuery<T1, T2> From<T2>(string alias = null, bool only = false)
        {
            var tableName = this.Environment.TableInfoProvider.GetTableInfoByType(typeof(T2)).TableName;
            return new TableQuery<T1, T2>(this, new FromClause(typeof(T2), tableName, alias, only));
        }

        public TableQuery<T1, T2> Join<T2>(Expression<Func<JoinBuilder, T1, T2, JoinBuilder>> builderExpr)
        {
            Guard.Argument(builderExpr, nameof(builderExpr)).NotNull();

            var tableName = this.Environment.TableInfoProvider.GetTableInfoByType(typeof(T2)).TableName;
            // TODO: builderExpr を変形
            var joinClause = builderExpr.Compile()(new JoinBuilder(), default, default).Build(typeof(T2), tableName);
            return new TableQuery<T1, T2>(this, joinClause);
        }

        public TypedQuery<TRecord> Select<TRecord>(Expression<Func<SelectBuilder, T1, SelectBuilder<TRecord>>> builderExpr)
        {
            Guard.Argument(builderExpr, nameof(builderExpr)).NotNull();
            throw new NotImplementedException();
        }

        public TypedQuery<TRecord> Select<S1, S2, TRecord>(Expression<Func<SelectBuilder, T1, SelectBuilder<S1, S2>>> builderExpr, Func<S1, S2, TRecord> mapper)
        {
            Guard.Argument(builderExpr, nameof(builderExpr)).NotNull();
            Guard.Argument(mapper, nameof(mapper)).NotNull();
            throw new NotImplementedException();
        }
    }

    public class TableQuery<T1, T2> : TableQuery
    {
        public TableQuery(TableQuery<T1> baseQuery, FromOrJoinClause tableClause)
            : base(baseQuery.Environment,
                  baseQuery.TableClauses,
                  typeof(T2), tableClause)
        { }

        public TableQuery<T1, T2, T3> From<T3>(string alias = null, bool only = false)
        {
            var tableName = this.Environment.TableInfoProvider.GetTableInfoByType(typeof(T3)).TableName;
            return new TableQuery<T1, T2, T3>(this, new FromClause(typeof(T3), tableName, alias, only));
        }

        public TableQuery<T1, T2, T3> Join<T3>(JoinClause joinClause)
        {
            return new TableQuery<T1, T2, T3>(this, joinClause);
        }

        public TypedQuery<TRecord> Select<TRecord>(Expression<Func<SelectBuilder, T1, T2, SelectBuilder<TRecord>>> builderExpr)
        {
            Guard.Argument(builderExpr, nameof(builderExpr)).NotNull();
            throw new NotImplementedException();
        }

        public TypedQuery<TRecord> Select<S1, S2, TRecord>(Expression<Func<SelectBuilder, T1, T2, SelectBuilder<S1, S2>>> builderExpr, Func<S1, S2, TRecord> mapper)
        {
            Guard.Argument(builderExpr, nameof(builderExpr)).NotNull();
            Guard.Argument(mapper, nameof(mapper)).NotNull();
            throw new NotImplementedException();
        }
    }

    public class TableQuery<T1, T2, T3> : TableQuery
    {
        public TableQuery(TableQuery<T1, T2> baseQuery, FromOrJoinClause tableClause)
            : base(baseQuery.Environment,
                  baseQuery.TableClauses,
                  typeof(T3), tableClause)
        { }
    }
}
