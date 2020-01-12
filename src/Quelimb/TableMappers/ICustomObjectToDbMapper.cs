using System;
using System.Data;

namespace Quelimb.TableMappers
{
    public interface ICustomObjectToDbMapper
    {
        /// <summary>
        /// Gets whether this <see cref="ICustomObjectToDbMapper"/> can convert a value whose type is <paramref name="objectType"/> to a database field.
        /// </summary>
        bool CanMapToDb(Type objectType);

        /// <summary>
        /// Creates a delegate which converts a value to a database field.
        /// The converted value will be set to the second parameter of the delegate.
        /// </summary>
        /// <typeparam name="T">
        /// The source type.
        /// The implementation can assume that <typeparamref name="T"/> is a type which <see cref="CanMapToDb(Type)"/> returns <c>true</c> when is passed to the method.
        /// </typeparam>
        /// <returns>
        /// A delegate that sets a value (the first parameter) to a <see cref="IDbDataParameter"/> (the second parameter).
        /// The third parameter is a root mapper, which can be used to pass a value to anothor mapper.
        /// </returns>
        Action<T, IDbDataParameter, ObjectToDbMapper> CreateMapperToDb<T>();
    }
}
