using System;
using System.Data;
using System.Linq.Expressions;

namespace Quelimb.TableMappers
{
    public class DelegateObjectToDbMapper<T> : ICustomObjectToDbMapper
    {
        public Action<T, IDbDataParameter, ObjectToDbMapper> MapToDb { get; }

        public DelegateObjectToDbMapper(Action<T, IDbDataParameter, ObjectToDbMapper> mapToDb)
        {
            Check.NotNull(mapToDb, nameof(mapToDb));
            this.MapToDb = mapToDb;
        }

        bool ICustomObjectToDbMapper.CanMapToDb(Type objectType)
        {
            Check.NotNull(objectType, nameof(objectType));
            return typeof(T).IsAssignableFrom(objectType);
        }

        Action<U, IDbDataParameter, ObjectToDbMapper> ICustomObjectToDbMapper.CreateMapperToDb<U>()
        {
            if (Equals(typeof(T), typeof(U)))
                return (Action<U, IDbDataParameter, ObjectToDbMapper>)(object)this.MapToDb;

            if (!typeof(T).IsAssignableFrom(typeof(U)))
                throw new ArgumentException($"The type argument U is {typeof(U)}, which is not supported.");

            // We need to convert the argument
            var objParam = Expression.Parameter(typeof(U), "obj");
            var destinationParam = Expression.Parameter(typeof(IDbDataParameter), "destination");
            var rootMapperParam = Expression.Parameter(typeof(ObjectToDbMapper), "rootMapper");
            return Expression.Lambda<Action<U, IDbDataParameter, ObjectToDbMapper>>(
                Expression.Invoke(
                    Expression.Constant(this.MapToDb),
                    Expression.Convert(objParam, typeof(T)),
                    destinationParam,
                    rootMapperParam
                ),
                objParam, destinationParam, rootMapperParam).Compile();
        }
    }
}
