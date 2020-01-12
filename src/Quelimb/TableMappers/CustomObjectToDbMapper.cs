using System;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;

namespace Quelimb.TableMappers
{
    public abstract class CustomObjectToDbMapper<T> : ICustomObjectToDbMapper
    {
        private static readonly MethodInfo s_mapToDbMethod = typeof(CustomObjectToDbMapper<T>)
            .GetMethod(
                nameof(MapToDb),
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new[] { typeof(T), typeof(IDbDataParameter), typeof(ObjectToDbMapper) },
                null);

        public abstract void MapToDb(T obj, IDbDataParameter destination, ObjectToDbMapper rootMapper);

        bool ICustomObjectToDbMapper.CanMapToDb(Type objectType)
        {
            Check.NotNull(objectType, nameof(objectType));
            return typeof(T).IsAssignableFrom(objectType);
        }

        Action<U, IDbDataParameter, ObjectToDbMapper> ICustomObjectToDbMapper.CreateMapperToDb<U>()
        {
            if (!typeof(T).IsAssignableFrom(typeof(U)))
                throw new ArgumentException($"The type argument U is {typeof(U)}, which is not supported.");

            if (typeof(T).IsValueType == typeof(U).IsValueType)
                return (Action<U, IDbDataParameter, ObjectToDbMapper>)s_mapToDbMethod
                    .CreateDelegate(typeof(Action<U, IDbDataParameter, ObjectToDbMapper>), this);

            // We need to box the argument
            var objParam = Expression.Parameter(typeof(U), "obj");
            var destinationParam = Expression.Parameter(typeof(IDbDataParameter), "destination");
            var rootMapperParam = Expression.Parameter(typeof(ObjectToDbMapper), "rootMapper");
            return Expression.Lambda<Action<U, IDbDataParameter, ObjectToDbMapper>>(
                Expression.Call(
                    Expression.Constant(this),
                    s_mapToDbMethod,
                    Expression.Convert(objParam, typeof(T)),
                    destinationParam,
                    rootMapperParam
                ),
                objParam, destinationParam, rootMapperParam).Compile();
        }
    }
}
