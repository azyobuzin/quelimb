﻿using System;
using System.Collections.Immutable;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;

namespace Quelimb.Mappers
{
    partial class DbToObjectMapper
    {
        private static ImmutableArray<ICustomDbToObjectMapper> s_defaultMappers;
        private static ImmutableArray<ICustomDbToObjectMapper> DefaultMappers
        {
            get
            {
                if (s_defaultMappers.IsDefault)
                {
                    s_defaultMappers = ImmutableArray.Create<ICustomDbToObjectMapper>(
                        new DelegateDbToObjectMapper<bool>((r, c, _) => r.GetBoolean(c)),
                        new DelegateDbToObjectMapper<byte>((r, c, _) => r.GetByte(c)),
                        new DelegateDbToObjectMapper<char>((r, c, _) => r.GetChar(c)),
                        new DelegateDbToObjectMapper<DateTime>((r, c, _) => r.GetDateTime(c)),
                        new DelegateDbToObjectMapper<decimal>((r, c, _) => r.GetDecimal(c)),
                        new DelegateDbToObjectMapper<double>((r, c, _) => r.GetDouble(c)),
                        new DelegateDbToObjectMapper<float>((r, c, _) => r.GetFloat(c)),
                        new DelegateDbToObjectMapper<Guid>((r, c, _) => r.GetGuid(c)),
                        new DelegateDbToObjectMapper<short>((r, c, _) => r.GetInt16(c)),
                        new DelegateDbToObjectMapper<int>((r, c, _) => r.GetInt32(c)),
                        new DelegateDbToObjectMapper<long>((r, c, _) => r.GetInt64(c)),
                        new DelegateDbToObjectMapper<string?>((r, c, _) => r.IsDBNull(c) ? null : r.GetString(c)),
                        new DelegateDbToObjectMapper<object?>((r, c, _) => r.GetValue(c)),
                        new DelegateDbToObjectMapper<sbyte>((r, c, _) => checked((sbyte)r.GetInt16(c))),
                        new DelegateDbToObjectMapper<ushort>((r, c, _) => checked((ushort)r.GetInt32(c))),
                        new DelegateDbToObjectMapper<uint>((r, c, _) => checked((uint)r.GetInt64(c))),
                        new DelegateDbToObjectMapper<ulong>(MapUInt64),
                        new DelegateDbToObjectMapper<DateTimeOffset>(MapDateTimeOffset),
                        new DelegateDbToObjectMapper<ValueTuple>((_, __, ___) => ValueTuple.Create()),
                        new NullableMapper()
                    );
                }
                return s_defaultMappers;
            }
        }

        private static ulong MapUInt64(IDataRecord record, int columnIndex, DbToObjectMapper rootMapper)
        {
            checked
            {
                return Type.GetTypeCode(record.GetFieldType(columnIndex)) switch
                {
                    TypeCode.Boolean => record.GetBoolean(columnIndex) ? 1UL : 0UL,
                    TypeCode.SByte => (ulong)record.GetInt16(columnIndex),
                    TypeCode.Byte => (ulong)record.GetByte(columnIndex),
                    TypeCode.Int16 => (ulong)record.GetInt16(columnIndex),
                    TypeCode.UInt16 => (ulong)record.GetInt32(columnIndex),
                    TypeCode.Int32 => (ulong)record.GetInt32(columnIndex),
                    TypeCode.UInt32 => (ulong)record.GetInt64(columnIndex),
                    TypeCode.Int64 => (ulong)record.GetInt64(columnIndex),
                    TypeCode.UInt64 => record is DbDataReader reader
                        ? reader.GetFieldValue<ulong>(columnIndex)
                        : (ulong)record.GetValue(columnIndex),
                    TypeCode.Single => (ulong)record.GetFloat(columnIndex),
                    TypeCode.Double => (ulong)record.GetDouble(columnIndex),
                    TypeCode.Decimal => (ulong)record.GetDecimal(columnIndex),
                    _ => Convert.ToUInt64(record.GetValue(columnIndex)),
                };
            }
        }

        private static DateTimeOffset MapDateTimeOffset(IDataRecord record, int columnIndex, DbToObjectMapper rootMapper)
        {
            var fieldType = record.GetFieldType(columnIndex);

            if (Equals(fieldType, typeof(DateTimeOffset)))
            {
                return record is DbDataReader reader
                    ? reader.GetFieldValue<DateTimeOffset>(columnIndex)
                    : (DateTimeOffset)record.GetValue(columnIndex);
            }

            return Equals(fieldType, typeof(string)) ? DateTimeOffset.Parse(record.GetString(columnIndex))
                : new DateTimeOffset(record.GetDateTime(columnIndex));
        }

        private sealed class NullableMapper : IGenericDbToObjectMapperProvider
        {
            public bool CanMapFromDb(Type objectType)
            {
                return Nullable.GetUnderlyingType(objectType) != null;
            }

            public int GetNumberOfColumnsUsed(Type objectType, DbToObjectMapper rootMapper)
            {
                Check.NotNull(objectType, nameof(objectType));
                Check.NotNull(rootMapper, nameof(rootMapper));

                var underlyingType = Nullable.GetUnderlyingType(objectType);

                if (underlyingType == null)
                    throw new ArgumentException($"{objectType} is not supported.", nameof(objectType));

                return rootMapper.GetNumberOfColumnsUsed(underlyingType);
            }

            public Func<IDataRecord, int, DbToObjectMapper, T>? CreateMapperFromDb<T>()
            {
                var underlyingType = Nullable.GetUnderlyingType(typeof(T));

                if (underlyingType == null)
                    throw new ArgumentException($"The type argument T is {typeof(T)}, which is not supported.");

                var recordParam = Expression.Parameter(typeof(IDataRecord), "record");
                var columnIndexParam = Expression.Parameter(typeof(int), "columnIndex");
                var rootMapperParam = Expression.Parameter(typeof(DbToObjectMapper), "rootMapper");

                var lambda = Expression.Lambda<Func<IDataRecord, int, DbToObjectMapper, T>>(
                    Expression.Condition(
                        Expression.Call(recordParam, ReflectionUtils.IDataRecordIsDBNullMethod, columnIndexParam),
                        Expression.Default(typeof(T)),
                        Expression.Convert(
                            Expression.Call(
                                rootMapperParam,
                                ReflectionUtils.DbToObjectMapperMapFromDbMethod
                                    .MakeGenericMethod(underlyingType),
                                recordParam,
                                columnIndexParam),
                            typeof(T))),
                    recordParam, columnIndexParam, rootMapperParam);
                return lambda.Compile();
            }

            public object? MapFromDb(Type objectType, IDataRecord record, int columnIndex, DbToObjectMapper rootMapper)
            {
                var underlyingType = Nullable.GetUnderlyingType(objectType);

                if (underlyingType == null)
                    throw new ArgumentException($"{objectType} is not supported.", nameof(objectType));

                return record.IsDBNull(columnIndex)
                    ? null
                    : rootMapper.MapFromDbBoxed(underlyingType, record, columnIndex);
            }
        }
    }
}
