using System;
using System.Collections.Immutable;
using System.Linq.Expressions;

namespace Quelimb.QueryTypes
{
    public abstract class TypedTableQuery<TRecord> : TypedQuery<TRecord>
    {
        private readonly Tuple<QueryEnvironment, ImmutableArray<Type>> _inner;

        protected internal QueryEnvironment Environment => this._inner.Item1;
        protected internal ImmutableArray<Type> TableTypes => this._inner.Item2;

        protected TypedTableQuery(
            ImmutableList<FormattableString> queryStrings, RecordConverter<TRecord> recordConverter,
            QueryEnvironment environment, ImmutableArray<Type> tableTypes)
            : base(queryStrings, recordConverter)
        {
            this._inner = Tuple.Create(environment, tableTypes);
        }

        protected TypedTableQuery(TypedTableQuery<TRecord> source, FormattableString appendQuery)
            : base(source.QueryStrings.Add(appendQuery), source.RecordConverter)
        {
            this._inner = source._inner;
        }
    }

    public class TypedTableQuery<T1, TRecord> : TypedTableQuery<TRecord>
    {
        public TypedTableQuery(
            ImmutableList<FormattableString> queryStrings, RecordConverter<TRecord> recordConverter,
            QueryEnvironment environment, ImmutableArray<Type> tableTypes)
            : base(queryStrings, recordConverter, environment, tableTypes)
        {
        }

        public TypedTableQuery(TypedTableQuery<TRecord> source, FormattableString appendQuery)
            : base(source, appendQuery)
        {
        }

        public new TypedTableQuery<T1, TRecord> Append(FormattableString query)
        {
            return new TypedTableQuery<T1, TRecord>(this, query);
        }

        public TypedTableQuery<T1, TRecord> Append(Expression<Func<T1, FormattableString>> queryFactory)
        {
            throw new NotImplementedException();
        }
    }

    public class TypedTableQuery<T1, T2, TRecord> : TypedTableQuery<TRecord>
    {
        public TypedTableQuery(
            ImmutableList<FormattableString> queryStrings, RecordConverter<TRecord> recordConverter,
            QueryEnvironment environment, ImmutableArray<Type> tableTypes)
            : base(queryStrings, recordConverter, environment, tableTypes)
        {
        }

        public TypedTableQuery(TypedTableQuery<TRecord> source, FormattableString appendQuery)
            : base(source, appendQuery)
        {
        }

        public new TypedTableQuery<T1, T2, TRecord> Append(FormattableString query)
        {
            return new TypedTableQuery<T1, T2, TRecord>(this, query);
        }

        public TypedTableQuery<T1, T2, TRecord> Append(Expression<Func<T1, T2, FormattableString>> queryFactory)
        {
            throw new NotImplementedException();
        }
    }

    public class TypedTableQuery<T1, T2, T3, TRecord> : TypedTableQuery<TRecord>
    {
        public TypedTableQuery(
            ImmutableList<FormattableString> queryStrings, RecordConverter<TRecord> recordConverter,
            QueryEnvironment environment, ImmutableArray<Type> tableTypes)
            : base(queryStrings, recordConverter, environment, tableTypes)
        {
        }

        public TypedTableQuery(TypedTableQuery<TRecord> source, FormattableString appendQuery)
            : base(source, appendQuery)
        {
        }

        public new TypedTableQuery<T1, T2, TRecord> Append(FormattableString query)
        {
            return new TypedTableQuery<T1, T2, TRecord>(this, query);
        }

        public TypedTableQuery<T1, T2, T3, TRecord> Append(Expression<Func<T1, T2, T3, FormattableString>> queryFactory)
        {
            throw new NotImplementedException();
        }
    }
}
