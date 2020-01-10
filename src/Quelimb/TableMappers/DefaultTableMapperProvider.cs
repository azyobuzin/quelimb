using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace Quelimb.TableMappers
{
    public class DefaultTableMapperProvider : TableMapperProvider
    {
        private static TableMapperProvider? s_default;
        public static TableMapperProvider Default => s_default ?? (s_default = new DefaultTableMapperProvider());

        private readonly ConcurrentDictionary<Type, TableMapper?> _cache = new ConcurrentDictionary<Type, TableMapper?>();
        private readonly Func<Type, TableMapper?> _factory;

        public DefaultTableMapperProvider()
        {
            this._factory = this.CreateTableMapper;
        }

        public override TableMapper? GetTableByType(Type type)
        {
            Check.NotNull(type, nameof(type));
            return this._cache.GetOrAdd(type, this._factory);
        }

        protected virtual TableMapper? CreateTableMapper(Type type)
        {
            Check.NotNull(type, nameof(type));

            if (type.IsGenericType && !type.IsConstructedGenericType)
                throw new ArgumentException("type is an open generic type.", nameof(type));

            if (Type.GetTypeCode(type) != TypeCode.Object)
                return null;

            if (TupleTableMapper.IsTupleType(type))
                return new TupleTableMapper(type, this);

            TableAttribute? tableAttribute = type.GetCustomAttribute<TableAttribute>();
            var tableName = tableAttribute?.Name ?? type.Name;
            var columns = new List<ColumnMapper>();

            foreach (var member in type.GetMembers(BindingFlags.Public | BindingFlags.Instance))
            {
                if (MemberIsColumn(member)) columns.Add(this.CreateColumnMapper(member));
            }

            return new DefaultTableMapper(tableName, type, columns);
        }

        private static bool MemberIsColumn(MemberInfo memberInfo)
        {
            switch (memberInfo.MemberType)
            {
                case MemberTypes.Property:
                    if (((PropertyInfo)memberInfo).GetIndexParameters().Length != 0)
                        return false;
                    break;
                case MemberTypes.Field:
                    break;
                default:
                    return false;
            }

            return !memberInfo.IsDefined(typeof(NotMappedAttribute));
        }

        private ColumnMapper CreateColumnMapper(MemberInfo memberInfo)
        {
            ColumnAttribute? columnAttribute = memberInfo.GetCustomAttribute<ColumnAttribute>();
            var columnName = columnAttribute?.Name ?? memberInfo.Name;
            return new ColumnMapper(columnName, memberInfo);
        }
    }
}
