using System;
using System.Data;
using System.Reflection;

namespace Quelimb.TableMappers
{
    public class ColumnMapper
    {
        public string ColumnName { get; }
        public MemberInfo MemberInfo { get; }

        public ColumnMapper(string columnName, MemberInfo memberInfo)
        {
            Check.NotNull(columnName, nameof(columnName));

            if (!(memberInfo is PropertyInfo || memberInfo is FieldInfo))
                throw new ArgumentException("memberInfo is required to be a PropertyInfo or a FieldInfo.", nameof(memberInfo));

            this.ColumnName = columnName;
            this.MemberInfo = memberInfo;
        }

        public virtual void SetValue(object target, IDataRecord record, int columnIndex, ValueConverter converter)
        {
            object? converted;

            switch (this.MemberInfo)
            {
                case PropertyInfo prop:
                    converted = converter.ConvertFrom(record, columnIndex, prop.PropertyType);
                    prop.SetValue(target, converted);
                    break;
                case FieldInfo field:
                    converted = converter.ConvertFrom(record, columnIndex, field.FieldType);
                    field.SetValue(target, converted);
                    break;
                default:
                    throw new InvalidOperationException("Invalid MemberInfo.");
            }
        }

        public override string ToString()
        {
            return $"ColumnMapper({this.ColumnName}, {this.MemberInfo})";
        }
    }
}
