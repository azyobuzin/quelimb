using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace Quelimb.TableMappers
{
    public class DefaultTableMapperProvider : TableMapperProvider
    {
        private static TableMapperProvider s_default;
        public static TableMapperProvider Default => s_default ??
            (s_default = new DefaultTableMapperProvider(DefaultValueConverter.Default));

        protected ValueConverter ValueConverter { get; }
        private readonly ConcurrentDictionary<Type, TableMapper> _cache = new ConcurrentDictionary<Type, TableMapper>();
        private readonly Func<Type, TableMapper> _factory;

        public DefaultTableMapperProvider(ValueConverter valueConverter)
        {
            this.ValueConverter = valueConverter;
            this._factory = this.CreateTableMapper;
        }

        public override TableMapper GetTableByType(Type type)
        {
            return this._cache.GetOrAdd(type, this._factory);
        }

        protected virtual TableMapper CreateTableMapper(Type type)
        {
            var tableAttribute = type.GetCustomAttribute<TableAttribute>();
            var tableName = tableAttribute?.Name ?? type.Name;
            var columns = new List<ColumnMapper>();

            foreach (var member in type.GetMembers(BindingFlags.Public | BindingFlags.Instance))
            {
                switch (member.MemberType)
                {
                    case MemberTypes.Property:
                    case MemberTypes.Field:
                        if (!member.IsDefined(typeof(NotMappedAttribute)))
                            columns.Add(this.CreateColumnMapper(member));
                        break;
                }
            }

            return new DefaultTableMapper(tableName, type, columns);
        }

        private ColumnMapper CreateColumnMapper(MemberInfo memberInfo)
        {
            var columnAttribute = memberInfo.GetCustomAttribute<ColumnAttribute>();
            var columnName = columnAttribute?.Name ?? memberInfo.Name;
            return new ColumnMapper(columnName, memberInfo);
        }
    }
}
