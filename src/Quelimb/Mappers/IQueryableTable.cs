using System.Collections.Generic;
using System.Reflection;

namespace Quelimb.Mappers
{
    /// <summary>
    /// Represents a table type that can be used as a type argument of <see cref="QueryBuilder.Query{T1}(System.Linq.Expressions.Expression{System.Func{T1, System.FormattableString}})"/>.
    /// </summary>
    public interface IQueryableTable
    {
        string TableName { get; }

        /// <summary>
        /// Gets the columns used by <c>*</c> format.
        /// </summary>
        IReadOnlyList<string> ColumnNames { get; }

        /// <summary>
        /// Gets the column name corresponding to the <paramref name="member"/>.
        /// </summary>
        /// <returns>A column name. If there is no corresponding column name, returns <c>null</c>.</returns>
        string? GetColumnNameByMemberInfo(MemberInfo member);
    }
}
