using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Quelimb.TableMappers
{
    internal sealed class TupleTableMapper : TableMapper
    {
        private static readonly ImmutableHashSet<Type> s_tupleTypes = ImmutableHashSet.Create(
            typeof(Tuple<>), typeof(Tuple<,>), typeof(Tuple<,,>), typeof(Tuple<,,,>),
            typeof(Tuple<,,,,>), typeof(Tuple<,,,,,>), typeof(Tuple<,,,,,,>), typeof(Tuple<,,,,,,,>),
            typeof(ValueTuple<>), typeof(ValueTuple<,>), typeof(ValueTuple<,,>), typeof(ValueTuple<,,,>),
            typeof(ValueTuple<,,,,>), typeof(ValueTuple<,,,,,>), typeof(ValueTuple<,,,,,,>), typeof(ValueTuple<,,,,,,,>));

        private readonly Type _tupleType;
        private readonly Type[] _typeArguments;
        private readonly TableMapperProvider _tableMapperProvider;
        private Func<IDataRecord, int, ValueConverter, object>? _createObjectFromRecord;

        private TableMapper? _restMapper;
        private TableMapper? RestMapper
        {
            get
            {
                if (this._typeArguments.Length != 8) return null;
                return this._restMapper ??
                    (this._restMapper = this._tableMapperProvider.GetTableByType(this._typeArguments[7]));
            }
        }

        public TupleTableMapper(Type tupleType, TableMapperProvider tableMapperProvider)
        {
            Check.NotNull(tupleType, nameof(tupleType));

            if (!IsTupleType(tupleType))
                throw new ArgumentException("tupleType must be a Tuple or ValueTuple.", nameof(tupleType));

            this._tupleType = tupleType;
            this._typeArguments = tupleType.GetGenericArguments();
            this._tableMapperProvider = tableMapperProvider;
        }

        public override string GetTableName()
        {
            throw new NotSupportedException("Cannot get table name of a tuple type");
        }

        public override int GetColumnCountForSelect()
        {
            var restMapper = this.RestMapper;
            return restMapper == null
                ? this._typeArguments.Length
                : 7 + restMapper.GetColumnCountForSelect();
        }

        public override IEnumerable<string> GetColumnsNamesForSelect()
        {
            throw new NotSupportedException("Cannot get column names of a tuple type.");
        }

        public override string? GetColumnNameByMemberInfo(MemberInfo member)
        {
            throw new NotSupportedException("Cannot get column names of a tuple type.");
        }

        public override object? CreateObjectFromOrderedColumns(IDataRecord record, int columnIndex, ValueConverter converter)
        {
            if (this._createObjectFromRecord == null)
                this._createObjectFromRecord = this.CreateCreateObjectFunc();
            return this._createObjectFromRecord(record, columnIndex, converter);
        }

        public override object? CreateObjectFromUnorderedColumns(IDataRecord record, ValueConverter converter)
        {
            return this.CreateObjectFromOrderedColumns(record, 0, converter);
        }

        public override string ToString()
        {
            return $"TupleTableMapper({this._tupleType})";
        }

        internal static bool IsTupleType(Type type)
        {
            Check.NotNull(type, nameof(type));

            // ValueTuple (no generic parameter) is not supported
            if (!type.IsConstructedGenericType) return false;
            return s_tupleTypes.Contains(type.GetGenericTypeDefinition());
        }

        private Func<IDataRecord, int, ValueConverter, object> CreateCreateObjectFunc()
        {
            var recordParam = Expression.Parameter(typeof(IDataRecord), "record");
            var columnIndexParam = Expression.Parameter(typeof(int), "columnIndex");
            var converterParam = Expression.Parameter(typeof(ValueConverter), "converter");

            var argExprs = new List<Expression>(this._typeArguments.Length);
            argExprs.AddRange(
                this._typeArguments.Take(7).Select((t, i) =>
                    Expression.Convert(
                        Expression.Call(
                            converterParam,
                            ReflectionUtils.ValueConverterConvertFromMethod,
                            recordParam,
                            Expression.AddChecked(columnIndexParam, Expression.Constant(i)),
                            Expression.Constant(t)),
                        t)));

            var restMapper = this.RestMapper;
            if (restMapper != null)
            {
                argExprs.Add(
                    Expression.Convert(
                        Expression.Call(
                            Expression.Constant(restMapper),
                            ReflectionUtils.TableMapperCreateObjectFromOrderedColumns,
                            recordParam,
                            Expression.AddChecked(columnIndexParam, Expression.Constant(7)),
                            converterParam),
                        this._typeArguments[7]));
            }

            var lambda = Expression.Lambda<Func<IDataRecord, int, ValueConverter, object>>(
                Expression.Convert(
                    Expression.New(this._tupleType.GetConstructor(this._typeArguments), argExprs),
                    typeof(object)),
                recordParam, columnIndexParam, converterParam);
            return lambda.Compile();
        }
    }
}
