using System;
using System.Data;

namespace Quelimb.Mappers
{
    public interface IGenericDbToObjectMapperProvider : ICustomDbToObjectMapper
    {
        /// <summary>
        /// Creates a delegate which converts a database field to a CLR object.
        /// </summary>
        /// <typeparam name="T">
        /// The destination type.
        /// The implementation can assume that <typeparamref name="T"/> is a type which <see cref="ICustomDbToObjectMapper.CanMapFromDb(Type)"/> returns <see langword="true"/> when is passed to the method.
        /// </typeparam>
        /// <returns>
        /// <para>
        /// A delegate that reads as many fields as <see cref="ICustomDbToObjectMapper.GetNumberOfColumnsUsed"/> returns from a record (the first parameter) and returns a converted value.
        /// The second parameter is a column index.
        /// The third parameter is a root mapper, which can be used to pass a value to anothor mapper.
        /// </para>
        /// <para>
        /// If the caller should use <see cref="ICustomDbToObjectMapper.MapFromDb(Type, IDataRecord, int, DbToObjectMapper)"/>, the implementation returns <see langword="null"/>.
        /// </para>
        /// </returns>
        Func<IDataRecord, int, DbToObjectMapper, T>? CreateMapperFromDb<T>();
    }
}
