using System;
using System.Data;

namespace Quelimb.Mappers
{
    public interface ICustomDbToObjectMapper
    {
        /// <summary>
        /// Gets whether this <see cref="ICustomDbToObjectMapper"/> can convert a database field to a value whose type is <paramref name="objectType"/>.
        /// </summary>
        bool CanMapFromDb(Type objectType);

        /// <summary>
        /// Gets the number of columns used in converting to <paramref name="objectType"/>.
        /// </summary>
        int GetNumberOfColumnsUsed(Type objectType, DbToObjectMapper rootMapper);

        object? MapFromDb(Type objectType, IDataRecord record, int columnIndex, DbToObjectMapper rootMapper);
        // TODO: これ要らない
    }
}
