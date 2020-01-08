using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace Quelimb.TableMappers
{
    public abstract class TableMapper
    {
        public abstract string TableName { get; }

        /// <summary>
        /// Gets columns used by <c>T.*</c> query.
        /// </summary>
        public abstract IEnumerable<string> GetColumnsNamesForSelect();

        public abstract string? GetColumnNameByMemberInfo(MemberInfo member);

        public abstract object? CreateObjectFromRecord(IDataRecord record, int columnIndex, ValueConverter converter);
    }
}
