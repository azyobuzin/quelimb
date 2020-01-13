﻿using System;
using System.Data;
using System.Reflection;
using Quelimb.Mappers;
using Quelimb.TableMappers;

namespace Quelimb
{
    internal static class ReflectionUtils
    {
        private static MethodInfo? s_valueConverterConvertFromMethod;
        public static MethodInfo ValueConverterConvertFromMethod =>
            s_valueConverterConvertFromMethod ?? (s_valueConverterConvertFromMethod =
                 typeof(ValueConverter).GetMethod(
                     nameof(ValueConverter.ConvertFrom),
                     BindingFlags.Public | BindingFlags.Instance,
                     null,
                     new[] { typeof(IDataRecord), typeof(int), typeof(Type) },
                     null) ?? throw new Exception("Could not get MethodInfo for ValueConverter.ConvertFrom."));

        private static MethodInfo? s_tableMapperCreateObjectFromOrderedColumns;
        public static MethodInfo TableMapperCreateObjectFromOrderedColumns =>
            s_tableMapperCreateObjectFromOrderedColumns ?? (s_tableMapperCreateObjectFromOrderedColumns =
                typeof(TableMapper).GetMethod(
                    nameof(TableMapper.CreateObjectFromOrderedColumns),
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    new[] { typeof(IDataRecord), typeof(int), typeof(ValueConverter) },
                    null) ?? throw new Exception("Could not get MethodInfo for TableMapper.CreateObjectFromOrderedColumns."));

        private static MethodInfo? s_iGenericObjectToDbMapperProviderCreateMapperToDbMethod;
        public static MethodInfo IGenericObjectToDbMapperProviderCreateMapperToDbMethod =>
            s_iGenericObjectToDbMapperProviderCreateMapperToDbMethod ?? (s_iGenericObjectToDbMapperProviderCreateMapperToDbMethod =
                typeof(IGenericObjectToDbMapperProvider).GetMethod(nameof(IGenericObjectToDbMapperProvider.CreateMapperToDb))
                ?? throw new Exception("Could not get MethodInfo for IGenericObjectToDbMapperProvider.CreateMapperToDb."));

        private static MethodInfo? s_iGenericDbToObjectMapperProviderCreateMapperFromDbMethod;
        public static MethodInfo IGenericDbToObjectMapperProviderCreateMapperFromDbMethod =>
            s_iGenericDbToObjectMapperProviderCreateMapperFromDbMethod ?? (s_iGenericDbToObjectMapperProviderCreateMapperFromDbMethod =
                typeof(IGenericDbToObjectMapperProvider).GetMethod(nameof(IGenericDbToObjectMapperProvider.CreateMapperFromDb))
                ?? throw new Exception("Could not get MethodInfo for IGenericDbToObjectMapperProvider.CreateMapperFromDb."));

        private static MethodInfo? s_dbToObjectMapperMapFromDbMethod;
        public static MethodInfo DbToObjectMapperMapFromDbMethod =>
            s_dbToObjectMapperMapFromDbMethod ?? (s_dbToObjectMapperMapFromDbMethod =
                typeof(DbToObjectMapper).GetMethod(
                    nameof(DbToObjectMapper.MapFromDb),
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    new[] { typeof(IDataRecord), typeof(int) },
                    null) ?? throw new Exception("Could not get MethodInfo for DbToObjectMapper.MapFromDb."));

        private static MethodInfo? s_dbToObjectMapperGetNumberOfColumnsUsedMethod;
        public static MethodInfo DbToObjectMapperGetNumberOfColumnsUsedMethod =>
            s_dbToObjectMapperGetNumberOfColumnsUsedMethod ?? (s_dbToObjectMapperGetNumberOfColumnsUsedMethod =
                typeof(DbToObjectMapper).GetMethod(
                    nameof(DbToObjectMapper.GetNumberOfColumnsUsed),
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    new[] { typeof(Type) },
                    null) ?? throw new Exception("Could not get MethodInfo for DbToObjectMapper.GetNumberOfColumnsUsed."));

        private static MethodInfo? s_iDataRecordIsDBNullMethod;
        public static MethodInfo IDataRecordIsDBNullMethod =>
            s_iDataRecordIsDBNullMethod ?? (s_iDataRecordIsDBNullMethod =
                typeof(IDataRecord).GetMethod(
                    nameof(IDataRecord.IsDBNull),
                    new[] { typeof(int) })
                ?? throw new Exception("Could not get MethodInfo for IDataRecord.IsDBNull."));
    }
}
