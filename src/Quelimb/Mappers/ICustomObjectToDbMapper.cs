using System;
using System.Data;

namespace Quelimb.Mappers
{
    public interface ICustomObjectToDbMapper
    {
        /// <summary>
        /// Gets whether this <see cref="ICustomObjectToDbMapper"/> can convert a value whose type is <paramref name="objectType"/> to a database field.
        /// </summary>
        bool CanMapToDb(Type objectType);

        void MapToDb(object? obj, IDbDataParameter destination, ObjectToDbMapper rootMapper);
    }
}
