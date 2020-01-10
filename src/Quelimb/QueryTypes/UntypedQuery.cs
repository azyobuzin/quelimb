using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Quelimb.TableMappers;

namespace Quelimb
{
    public class UntypedQuery
    {
        protected QueryEnvironment Environment { get; }
        protected Action<IDbCommand> SetupCommandAction { get; }

        protected internal UntypedQuery(QueryEnvironment environment, Action<IDbCommand> setupCommand)
        {
            this.Environment = environment ?? throw new ArgumentNullException(nameof(environment));
            this.SetupCommandAction = setupCommand ?? throw new ArgumentNullException(nameof(setupCommand));
        }

        public void SetupCommand(IDbCommand command)
        {
            Check.NotNull(command, nameof(command));
            this.SetupCommandAction(command);
        }

        public TypedQuery<T> Map<T>()
        {
            var valueConverter = this.Environment.ValueConverter;
            Func<IDataRecord, T> recordConverter;

            if (this.Environment.ValueConverter.CanConvertFrom(typeof(T)))
            {
                // Use ValueConverter
                recordConverter = record => (T)valueConverter.ConvertFrom(record, 0, typeof(T))!;
            }
            else
            {
                // Use TableMapper
                var tableMapper = this.Environment.TableMapperProvider.GetTableByType(typeof(T));
                if (tableMapper == null) throw new ArgumentException($"Could not make TableMapper for {typeof(T)}.");
                recordConverter = record => (T)tableMapper.CreateObjectFromUnorderedColumns(record, valueConverter)!;
            }

            return new TypedQuery<T>(this.Environment, this.SetupCommandAction, recordConverter);
        }

        public TypedQuery<TResult> Map<T1, T2, TResult>(Func<T1, T2, TResult> mapper)
        {
            return this.MapCore<Func<T1, T2, TResult>, TResult>(mapper);
        }

        public TypedQuery<TResult> Map<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> mapper)
        {
            return this.MapCore<Func<T1, T2, T3, TResult>, TResult>(mapper);
        }

        public TypedQuery<TResult> Map<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, TResult> mapper)
        {
            return this.MapCore<Func<T1, T2, T3, T4, TResult>, TResult>(mapper);
        }

        public TypedQuery<TResult> Map<T1, T2, T3, T4, T5, TResult>(Func<T1, T2, T3, T4, T5, TResult> mapper)
        {
            return this.MapCore<Func<T1, T2, T3, T4, T5, TResult>, TResult>(mapper);
        }

        public TypedQuery<TResult> Map<T1, T2, T3, T4, T5, T6, TResult>(Func<T1, T2, T3, T4, T5, T6, TResult> mapper)
        {
            return this.MapCore<Func<T1, T2, T3, T4, T5, T6, TResult>, TResult>(mapper);
        }

        public TypedQuery<TResult> Map<T1, T2, T3, T4, T5, T6, T7, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, TResult> mapper)
        {
            return this.MapCore<Func<T1, T2, T3, T4, T5, T6, T7, TResult>, TResult>(mapper);
        }

        public TypedQuery<TResult> Map<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> mapper)
        {
            return this.MapCore<Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult>, TResult>(mapper);
        }

        public TypedQuery<TResult> Map<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult> mapper)
        {
            return this.MapCore<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>, TResult>(mapper);
        }

        public TypedQuery<TResult> Map<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult> mapper)
        {
            return this.MapCore<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>, TResult>(mapper);
        }

        public TypedQuery<TResult> Map<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult> mapper)
        {
            return this.MapCore<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult>, TResult>(mapper);
        }

        public TypedQuery<TResult> Map<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult> mapper)
        {
            return this.MapCore<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult>, TResult>(mapper);
        }

        public TypedQuery<TResult> Map<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult> mapper)
        {
            return this.MapCore<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult>, TResult>(mapper);
        }

        public TypedQuery<TResult> Map<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult> mapper)
        {
            return this.MapCore<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult>, TResult>(mapper);
        }

        public TypedQuery<TResult> Map<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult> mapper)
        {
            return this.MapCore<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult>, TResult>(mapper);
        }

        public TypedQuery<TResult> Map<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult> mapper)
        {
            return this.MapCore<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult>, TResult>(mapper);
        }

        private TypedQuery<TResult> MapCore<TDelegate, TResult>(TDelegate mapper)
            where TDelegate : Delegate
        {
            Check.NotNull(mapper, nameof(mapper));

            var recordConverter = this.GetOrCreateRecordConverterFactory<TDelegate, TResult>()(mapper);
            return new TypedQuery<TResult>(this.Environment, this.SetupCommandAction, recordConverter);
        }

        private Func<TDelegate, Func<IDataRecord, TResult>> GetOrCreateRecordConverterFactory<TDelegate, TResult>()
           where TDelegate : Delegate
        {
            var cache = this.Environment.RecordConverterCache;
            var key = typeof(TDelegate);

            if (!cache.TryGetValue(key, out var func))
            {
                func = this.CreateRecordConverterFactory(key, typeof(TResult));
                cache.TryAdd(key, func);
            }

            return (Func<TDelegate, Func<IDataRecord, TResult>>)func;
        }

        private Delegate CreateRecordConverterFactory(Type delegateType, Type tResult)
        {
            if (!delegateType.FullName.StartsWith("System.Func`", StringComparison.Ordinal))
                throw new ArgumentException("delegateType is not a System.Func.", nameof(delegateType));

            var genericArguments = delegateType.GetGenericArguments();
            var parameterCount = genericArguments.Length - 1; // The last argument is TResult

            if (!Equals(genericArguments[parameterCount], tResult))
                throw new ArgumentException("tResult is not the return type of delegateType.", nameof(tResult));

            var mapperParam = Expression.Parameter(delegateType, "mapper");
            var recordParam = Expression.Parameter(typeof(IDataRecord), "record");
            var valueConverter = Expression.Constant(this.Environment.ValueConverter, typeof(ValueConverter));

            var argExprs = new List<Expression>(parameterCount);
            var columnIndex = 0;
            for (var i = 0; i < parameterCount; i++)
            {
                var argType = genericArguments[i];

                if (this.Environment.ValueConverter.CanConvertFrom(argType))
                {
                    // Use ValueConverter
                    argExprs.Add(Expression.Convert(
                        Expression.Call(
                            valueConverter,
                            ReflectionUtils.ValueConverterConvertFromMethod,
                            recordParam,
                            Expression.Constant(columnIndex),
                            Expression.Constant(argType, typeof(Type))),
                        argType));
                    columnIndex++;
                }
                else
                {
                    // Use TableMapper
                    var tableMapper = this.Environment.TableMapperProvider.GetTableByType(argType);
                    if (tableMapper == null) throw new ArgumentException($"Could not make TableMapper for {argType}.");

                    argExprs.Add(Expression.Convert(
                        Expression.Call(
                            Expression.Constant(tableMapper, typeof(TableMapper)),
                            ReflectionUtils.TableMapperCreateObjectFromOrderedColumns,
                            recordParam,
                            Expression.Constant(columnIndex),
                            valueConverter),
                        argType));

                    columnIndex += tableMapper.GetColumnCountForSelect();
                }
            }

            var recordConverterType = typeof(Func<,>).MakeGenericType(typeof(IDataRecord), tResult);
            var lambda = Expression.Lambda(
                typeof(Func<,>).MakeGenericType(delegateType, recordConverterType),
                Expression.Lambda(
                    recordConverterType,
                    Expression.Invoke(mapperParam, argExprs),
                    recordParam),
                mapperParam);

            return lambda.Compile();
        }

        public int ExecuteNonQuery(DbConnection connection, DbTransaction? transaction = null)
        {
            Check.NotNull(connection, nameof(connection));
            return this.Environment.CommandExecutor.ExecuteNonQuery(this, connection, transaction);
        }

        public Task<int> ExecuteNonQueryAsync(DbConnection connection, DbTransaction? transaction = null, CancellationToken cancellationToken = default)
        {
            Check.NotNull(connection, nameof(connection));
            return this.Environment.CommandExecutor.ExecuteNonQueryAsync(this, connection, transaction, cancellationToken);
        }

        public object ExecuteScalar(DbConnection connection, DbTransaction? transaction = null)
        {
            Check.NotNull(connection, nameof(connection));
            return this.Environment.CommandExecutor.ExecuteScalar(this, connection, transaction);
        }

        public Task<object?> ExecuteScalarAsync(DbConnection connection, DbTransaction? transaction = null, CancellationToken cancellationToken = default)
        {
            Check.NotNull(connection, nameof(connection));
            return this.Environment.CommandExecutor.ExecuteScalarAsync(this, connection, transaction, cancellationToken);
        }
    }
}
