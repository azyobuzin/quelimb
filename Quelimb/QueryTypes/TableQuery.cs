using System;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Dawn;
using Quelimb.QueryTypes;

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

        public TypedQuery<TRecord> Select<TRecord>(FormattableString selectClause, FormattableString afterFromClause = null)
        {
            Guard.Argument(afterFromClause, nameof(afterFromClause)).NotNull();

            var builder = ImmutableArray.CreateBuilder<FormattableString>(4);
            builder.Add($"SELECT ");
            builder.Add(selectClause);
            builder.Add($" {new RawQuery(this.CreateTableReferences())}");

            if (afterFromClause != null)
            {
                builder.Add($" ");
                builder.Add(afterFromClause);
            }

            return new TypedQuery<TRecord>(builder.ToImmutable(), null); // TODO: recordConverter
        }

        public UntypedQuery Update(FormattableString afterTableReferences)
        {
            return this.Update("UPDATE", afterTableReferences);
        }

        public UntypedQuery Update(string verb, FormattableString afterTableReferences)
        {
            Guard.Argument(verb, nameof(verb)).NotNull().NotEmpty();
            Guard.Argument(afterTableReferences, nameof(afterTableReferences)).NotNull();

            return new UntypedQuery(ImmutableArray.Create<FormattableString>(
                $"{new RawQuery(verb)} {new RawQuery(this.CreateTableReferences())} ",
                afterTableReferences));
        }

        public UntypedQuery Delete(FormattableString afterTableReferences = null)
        {
            if (this.TableClauses.Any(x => !(x.Item2 is FromClause)))
                throw new InvalidOperationException("DELETE cannot be contain JOINs.");

            var sb = new StringBuilder("DELETE FROM ");

            using (var enumerator = this.TableClauses.GetEnumerator())
            {
                if (!enumerator.MoveNext()) throw new InvalidOperationException("TableClauses is empty.");

                sb.Append(enumerator.Current.Item2.CreateSql(
                    this.Environment.TableInfoProvider.GetTableInfoByType(enumerator.Current.Item1).TableName,
                    this.Environment.Generator));

                if (enumerator.MoveNext())
                {
                    sb.Append(" USING ");
                    var first = true;
                    do
                    {
                        if (first) first = false;
                        else sb.Append(", ");

                        var t = enumerator.Current;
                        var tableName = this.Environment.TableInfoProvider.GetTableInfoByType(t.Item1).TableName;
                        sb.Append(t.Item2.CreateSql(tableName, this.Environment.Generator));
                    } while (enumerator.MoveNext());
                }
            }

            return new UntypedQuery(afterTableReferences == null
                ? ImmutableArray.Create<FormattableString>($"{new RawQuery(sb.ToString())}")
                : ImmutableArray.Create<FormattableString>($"{new RawQuery(sb.ToString())} ", afterTableReferences));
        }

        private string CreateTableReferences()
        {
            // FROM
            var sb = new StringBuilder(" FROM ");
            var first = true;
            foreach (var table in this.TableClauses)
            {
                if (!(table.Item2 is FromClause)) continue;

                if (first) first = false;
                else sb.Append(", ");

                var tableName = this.Environment.TableInfoProvider.GetTableInfoByType(table.Item1).TableName;
                sb.Append(table.Item2.CreateSql(tableName, this.Environment.Generator));
            }

            // JOIN
            first = true;
            foreach (var table in this.TableClauses)
            {
                if (table.Item2 is FromClause) continue;

                if (first) first = false;
                else sb.Append(" ");

                var tableName = this.Environment.TableInfoProvider.GetTableInfoByType(table.Item1).TableName;
                sb.Append(table.Item2.CreateSql(tableName, this.Environment.Generator));
            }

            return sb.ToString();
        }
    }

    public class TableQuery<T1> : TableQuery
    {
        public TableQuery(QueryEnvironment environment, FromClause fromClause)
            : base(environment, ImmutableList<Tuple<Type, FromOrJoinClause>>.Empty, typeof(T1), fromClause)
        {
        }

        public TableQuery<T1, T2> FromTable<T2>(string alias = null, bool only = false)
        {
            return new TableQuery<T1, T2>(this, new FromClause(alias, only));
        }

        // TODO: ON で FormattableString 使いたい
        public TableQuery<T1, T2> Join<T2>(JoinClause joinClause)
        {
            return new TableQuery<T1, T2>(this, joinClause);
        }

        public TypedQuery<TRecord> Select<TRecord>(Expression<Func<T1, FormattableString>> selectClause, Expression<Func<T1, FormattableString>> afterFromClause = null)
        {
            throw new NotImplementedException();
        }

        public TypedQuery<TRecord> Select<TRecord>(Expression<Func<T1, TRecord>> mapper, Expression<Func<T1, FormattableString>> afterFromClause = null)
        {
            throw new NotImplementedException();
        }

        public UntypedQuery Update(Expression<Func<T1, FormattableString>> afterTableReferences)
        {
            return this.Update("UPDATE", afterTableReferences);
        }

        public UntypedQuery Update(string verb, Expression<Func<T1, FormattableString>> afterTableReferences)
        {
            throw new NotImplementedException();
        }

        public UntypedQuery Delete(Expression<Func<T1, FormattableString>> afterTableReferences)
        {
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

        public TableQuery<T1, T2, T3> FromTable<T3>(string alias = null, bool only = false)
        {
            return new TableQuery<T1, T2, T3>(this, new FromClause(alias, only));
        }

        public TableQuery<T1, T2, T3> Join<T3>(JoinClause joinClause)
        {
            return new TableQuery<T1, T2, T3>(this, joinClause);
        }

        public TypedQuery<TRecord> Select<TRecord>(Expression<Func<T1, T2, FormattableString>> selectClause, Expression<Func<T1, T2, FormattableString>> afterFromClause = null)
        {
            throw new NotImplementedException();
        }

        public TypedQuery<TRecord> Select<TRecord>(Expression<Func<T1, T2, TRecord>> mapper, Expression<Func<T1, T2, FormattableString>> afterFromClause = null)
        {
            throw new NotImplementedException();
        }

        public UntypedQuery Update(Expression<Func<T1, T2, FormattableString>> afterTableReferences)
        {
            return this.Update("UPDATE", afterTableReferences);
        }

        public UntypedQuery Update(string verb, Expression<Func<T1, T2, FormattableString>> afterTableReferences)
        {
            throw new NotImplementedException();
        }

        public UntypedQuery Delete(Expression<Func<T1, T2, FormattableString>> afterTableReferences)
        {
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
