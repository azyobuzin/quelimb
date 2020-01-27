using System;
using System.Collections.Generic;
using System.Data;

namespace Quelimb.Mappers
{
    public interface IRecordToObjectMapperProvider : ICustomDbToObjectMapper
    {
        /// <summary>
        /// Creates a delegate which converts a projected record with unordered columns to a CLR object.
        /// </summary>
        /// <typeparam name="T">
        /// The destination type.
        /// The implementation can assume that <typeparamref name="T"/> is a type which <see cref="ICustomDbToObjectMapper.CanMapFromDb(Type)"/> returns <see langword="true"/> when is passed to the method.
        /// </typeparam>
        /// <returns>
        /// <para>
        /// A delegate that takes a list of column names (the first parameter) and a root mapper (the second parameter) and returns a delegate that reads a record and returns a converted value.
        /// The first parameter is a list of column names, which retrieved from the first record.
        /// The second parameter is a root mapper, which can be used to pass a value to anothor mapper.
        /// </para>
        /// <para>
        /// If the caller should use <see cref="IGenericDbToObjectMapperProvider.CreateMapperFromDb{T}"/> or <see cref="ICustomDbToObjectMapper.MapFromDb"/>, the implementation returns <see langword="null"/>.
        /// </para>
        /// </returns>
        Func<IReadOnlyList<string>, DbToObjectMapper, Func<IDataRecord, T>>? CreateMapperFromRecord<T>();
    }
}
