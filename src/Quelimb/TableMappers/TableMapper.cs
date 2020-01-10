using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace Quelimb.TableMappers
{
    public abstract class TableMapper
    {
        public abstract string GetTableName();

        /// <summary>
        /// Counts the columns used by <c>COLUMNS</c> format.
        /// </summary>
        public abstract int GetColumnCountForSelect();

        /// <summary>
        /// Gets the columns used by <c>COLUMNS</c> format.
        /// </summary>
        public abstract IEnumerable<string> GetColumnsNamesForSelect();

        public abstract string? GetColumnNameByMemberInfo(MemberInfo member);

        public abstract object? CreateObjectFromOrderedColumns(IDataRecord record, int columnIndex, ValueConverter converter);

        public abstract object? CreateObjectFromUnorderedColumns(IDataRecord record, ValueConverter converter);
    }
}
