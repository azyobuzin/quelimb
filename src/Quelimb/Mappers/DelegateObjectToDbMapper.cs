using System;
using System.Data;
using System.Linq.Expressions;

namespace Quelimb.Mappers
{
    public class DelegateObjectToDbMapper<T> : IGenericObjectToDbMapperProvider
    {
        public Action<T, IDbDataParameter, ObjectToDbMapper> MapToDb { get; }

        private readonly bool _acceptSubclassOfT;

        public DelegateObjectToDbMapper(
            Action<T, IDbDataParameter, ObjectToDbMapper> mapToDb,
            bool acceptSubclassOfT = false)
        {
            Check.NotNull(mapToDb, nameof(mapToDb));
            this.MapToDb = mapToDb;
            this._acceptSubclassOfT = acceptSubclassOfT;
        }

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
            if (Equals(typeof(T), typeof(U)))
                return (Action<U, IDbDataParameter, ObjectToDbMapper>)(object)this.MapToDb;

            if (!this.CanMapToDb(typeof(U)))
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
