using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Quelimb.Mappers
{
    [StructLayout(LayoutKind.Auto)]
    public readonly struct NamedColumn
    {
        public string ColumnName { get; }
        public MemberInfo MemberInfo { get; }

        public NamedColumn(string columnName, MemberInfo memberInfo)
        {
            this.ColumnName = columnName ?? throw new ArgumentNullException(nameof(columnName));
            this.MemberInfo = memberInfo ?? throw new ArgumentNullException(nameof(memberInfo));
        }

        public override string ToString()
        {
            return $"ColumnInfo({this.ColumnName}, {this.MemberInfo})";
        }
    }
}
