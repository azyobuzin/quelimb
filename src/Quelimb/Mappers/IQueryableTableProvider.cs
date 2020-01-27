using System;

namespace Quelimb.Mappers
{
    public interface IQueryableTableProvider : ICustomDbToObjectMapper
    {
        /// <summary>
        /// Gets a <seealso cref="IQueryableTable"/> corresponding to <paramref name="objectType"/>.
        /// </summary>
        /// <param name="objectType">
        /// A type of the table.
        /// The implementation can assume that this argument is a type which <see cref="ICustomDbToObjectMapper.CanMapFromDb(Type)"/> returns <see langword="true"/> when is passed to the method.
        /// </param>
        /// <returns>
        /// A corresponding <seealso cref="IQueryableTable"/>.
        /// If <paramref name="objectType"/> cannot be treated as a table, returns <see langword="null"/>.
        /// </returns>
        IQueryableTable? GetQueryableTable(Type objectType);
    }
}
