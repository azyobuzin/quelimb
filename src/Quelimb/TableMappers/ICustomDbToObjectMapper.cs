using System;
using System.Data;

namespace Quelimb.TableMappers
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

        /// <summary>
        /// Creates a delegate which converts a database field to a CLR object.
        /// </summary>
        /// <typeparam name="T">
        /// The source type.
        /// The implementation can assume that <typeparamref name="T"/> is a type which <see cref="CanMapFromDb(Type)"/> returns <c>true</c> when is passed to the method.</typeparam>
        /// <returns>
        /// A delegate that reads as many fields as <see cref="GetNumberOfColumnsUsed"/> returns from a record (the first parameter) and returns a converted value.
        /// The second parameter is a column index.
        /// The third parameter is a root mapper, which can be used to pass a value to anothor mapper.
        /// </returns>
        Func<IDataRecord, int, DbToObjectMapper, T> CreateMapperFromDb<T>();
    }
}
