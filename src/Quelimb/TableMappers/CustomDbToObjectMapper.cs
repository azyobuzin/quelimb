using System;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;

namespace Quelimb.TableMappers
{
    public abstract class CustomDbToObjectMapper<T> : ICustomDbToObjectMapper
    {
        private static readonly MethodInfo s_mapFromDbMethod = typeof(CustomDbToObjectMapper<T>)
              .GetMethod(
                  nameof(MapFromDb),
                  BindingFlags.Public | BindingFlags.Instance,
                  null,
                  new[] { typeof(IDataRecord), typeof(int), typeof(DbToObjectMapper) },
                  null);

        public virtual int GetNumberOfColumnsUsed(DbToObjectMapper rootMapper)
        {
            return 1;
        }

        public abstract T MapFromDb(IDataRecord record, int columnIndex, DbToObjectMapper rootMapper);

        bool ICustomDbToObjectMapper.CanMapFromDb(Type objectType)
        {
            Check.NotNull(objectType, nameof(objectType));
            return objectType.IsAssignableFrom(typeof(T));
        }

        int ICustomDbToObjectMapper.GetNumberOfColumnsUsed(Type objectType, DbToObjectMapper rootMapper)
        {
            Check.NotNull(rootMapper, nameof(rootMapper));
            return this.GetNumberOfColumnsUsed(rootMapper);
        }

        Func<IDataRecord, int, DbToObjectMapper, U> ICustomDbToObjectMapper.CreateMapperFromDb<U>()
        {
            if (!typeof(U).IsAssignableFrom(typeof(T)))
                throw new ArgumentException($"The type argument U is {typeof(U)}, which is not supported.");

            if (typeof(T).IsValueType == typeof(U).IsValueType)
                return (Func<IDataRecord, int, DbToObjectMapper, U>)s_mapFromDbMethod
                    .CreateDelegate(typeof(Func<IDataRecord, int, DbToObjectMapper, U>), this);

            // We need to box the return value
            var recordParam = Expression.Parameter(typeof(IDataRecord), "record");
            var columnIndexParam = Expression.Parameter(typeof(int), "columnIndex");
            var rootMapperParam = Expression.Parameter(typeof(DbToObjectMapper), "rootMapper");
            return
                Expression.Lambda<Func<IDataRecord, int, DbToObjectMapper, U>>(
                    Expression.Convert(
                        Expression.Call(
                            Expression.Constant(this),
                            s_mapFromDbMethod,
                            recordParam,
                            columnIndexParam,
                            rootMapperParam
                        ),
                        typeof(U)),
                    recordParam, columnIndexParam, rootMapperParam)
                .Compile();
        }
    }
}
