using System;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;

namespace Quelimb.Mappers
{
    public abstract class CustomObjectToDbMapper<T> : IGenericObjectToDbMapperProvider
    {
        private static readonly MethodInfo s_mapToDbMethod = typeof(CustomObjectToDbMapper<T>)
            .GetMethod(
                nameof(MapToDb),
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new[] { typeof(T), typeof(IDbDataParameter), typeof(ObjectToDbMapper) },
                null);

        private readonly bool _acceptSubclassOfT;

        protected CustomObjectToDbMapper(bool acceptSubclassOfT)
        {
            this._acceptSubclassOfT = acceptSubclassOfT;
        }

        protected CustomObjectToDbMapper() : this(false) { }

        public abstract void MapToDb(T obj, IDbDataParameter destination, ObjectToDbMapper rootMapper);

        public bool CanMapToDb(Type objectType)
        {
            Check.NotNull(objectType, nameof(objectType));
            return this._acceptSubclassOfT
                ? typeof(T).IsAssignableFrom(objectType)
                : Equals(objectType, typeof(T));
        }

        void ICustomObjectToDbMapper.MapToDb(object? obj, IDbDataParameter destination, ObjectToDbMapper rootMapper)
        {
            Check.NotNull(destination, nameof(destination));
            Check.NotNull(rootMapper, nameof(rootMapper));
            this.MapToDb((T)obj!, destination, rootMapper);
        }

        Action<U, IDbDataParameter, ObjectToDbMapper>? IGenericObjectToDbMapperProvider.CreateMapperToDb<U>()
        {
            if (!this.CanMapToDb(typeof(U)))
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
