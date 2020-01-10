using System;
using System.Data;
using System.Reflection;
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
    }
}
