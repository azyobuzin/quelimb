using System;
using System.Data;

namespace Quelimb.Mappers
{
    public interface IGenericObjectToDbMapperProvider : ICustomObjectToDbMapper
    {
        /// <summary>
        /// Creates a delegate which converts a value to a database field.
        /// The converted value will be set to the second parameter of the delegate.
        /// </summary>
        /// <typeparam name="T">
        /// The source type.
        /// The implementation can assume that <typeparamref name="T"/> is a type which <see cref="ICustomObjectToDbMapper.CanMapToDb(Type)"/> returns <c>true</c> when is passed to the method.
        /// </typeparam>
        /// <returns>
        /// <para>
        /// A delegate that sets a value (the first parameter) to a <see cref="IDbDataParameter"/> (the second parameter).
        /// The third parameter is a root mapper, which can be used to pass a value to anothor mapper.
        /// </para>
        /// <para>
        /// If the caller should use <see cref="ICustomObjectToDbMapper.MapToDb(object, IDbDataParameter, ObjectToDbMapper)"/>, the implementation returns <c>null</c>.
        /// </para>
        /// </returns>
        Action<T, IDbDataParameter, ObjectToDbMapper>? CreateMapperToDb<T>();
    }
}
