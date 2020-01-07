using System.Collections.Generic;
using System.Reflection;

namespace Quelimb.Converters
{
    public interface ITableInfo
    {
        string TableName { get; }
        IReadOnlyList<IColumnInfo> ColumnsForSelect { get; }
        IColumnInfo GetColumnForSelect(MemberInfo member);
    }
}
