using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;

namespace Quelimb.Mappers
{
    public abstract class CustomDbToObjectMapper<T>
        : IGenericDbToObjectMapperProvider, IRecordToObjectMapperProvider
    {
        private static readonly MethodInfo s_mapFromDbMethod = typeof(CustomDbToObjectMapper<T>)
              .GetMethod(
                  nameof(MapFromDb),
                  BindingFlags.Public | BindingFlags.Instance,
                  null,
                  new[] { typeof(IDataRecord), typeof(int), typeof(DbToObjectMapper) },
                  null);

        private static readonly MethodInfo s_createMapperFromMethod = typeof(CustomDbToObjectMapper<T>)
              .GetMethod(
                  nameof(CreateMapperFromRecord),
                  BindingFlags.Public | BindingFlags.Instance,
                  null,
                  new[] { typeof(IDataRecord), typeof(int), typeof(DbToObjectMapper) },
                  null);

        private readonly bool _acceptSuperclassOfT;

        protected CustomDbToObjectMapper(bool acceptSuperclassOfT)
        {
            this._acceptSuperclassOfT = acceptSuperclassOfT;
        }

        protected CustomDbToObjectMapper() : this(false) { }

        public virtual int GetNumberOfColumnsUsed(DbToObjectMapper rootMapper)
        {
            return 1;
        }

        public abstract T MapFromDb(IDataRecord record, int columnIndex, DbToObjectMapper rootMapper);

        public bool CanMapFromDb(Type objectType)
        {
            Check.NotNull(objectType, nameof(objectType));
            return this._acceptSuperclassOfT
                ? objectType.IsAssignableFrom(typeof(T))
                : Equals(objectType, typeof(T));
        }

        object? ICustomDbToObjectMapper.MapFromDb(Type objectType, IDataRecord record, int columnIndex, DbToObjectMapper rootMapper)
        {
            Check.NotNull(record, nameof(record));
            Check.NotNull(rootMapper, nameof(rootMapper));

            if (!this.CanMapFromDb(objectType))
                throw new ArgumentException($"{objectType} is not supported.", nameof(objectType));

            return this.MapFromDb(record, columnIndex, rootMapper);
        }

        int ICustomDbToObjectMapper.GetNumberOfColumnsUsed(Type objectType, DbToObjectMapper rootMapper)
        {
            Check.NotNull(rootMapper, nameof(rootMapper));
            return this.GetNumberOfColumnsUsed(rootMapper);
        }

        Func<IDataRecord, int, DbToObjectMapper, U>? IGenericDbToObjectMapperProvider.CreateMapperFromDb<U>()
        {
            if (!this.CanMapFromDb(typeof(U)))
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

        public virtual Func<IReadOnlyList<string>, DbToObjectMapper, Func<IDataRecord, T>>? CreateMapperFromRecord()
        {
            return null;
        }

        Func<IReadOnlyList<string>, DbToObjectMapper, Func<IDataRecord, U>>? IRecordToObjectMapperProvider.CreateMapperFromRecord<U>()
        {
            if (!this.CanMapFromDb(typeof(U)))
                throw new ArgumentException($"The type argument U is {typeof(U)}, which is not supported.");

            var mapper = this.CreateMapperFromRecord();

            if (mapper == null) return null;

            if (typeof(T).IsValueType == typeof(U).IsValueType)
                return (Func<IReadOnlyList<string>, DbToObjectMapper, Func<IDataRecord, U>>)(object)mapper;

            // We need to convert the return value
            var columnNamesParam = Expression.Parameter(typeof(IReadOnlyList<string>), "columnNames");
            var rootMapperParam = Expression.Parameter(typeof(IReadOnlyList<DbToObjectMapper>), "rootMapper");
            var recordParam = Expression.Parameter(typeof(IDataRecord), "record");
            var innerMapperVar = Expression.Variable(typeof(Func<IDataRecord, T>), "innerMapper");
            return Expression.Lambda<Func<IReadOnlyList<string>, DbToObjectMapper, Func<IDataRecord, U>>>(
                Expression.Block(
                    new[] { innerMapperVar },
                    Expression.Assign(
                        innerMapperVar,
                        Expression.Invoke(
                            Expression.Constant(mapper, typeof(Func<IReadOnlyList<string>, DbToObjectMapper, Func<IDataRecord, T>>)),
                            columnNamesParam,
                            rootMapperParam
                        )),
                    Expression.Lambda<Func<IDataRecord, U>>(
                        Expression.Convert(
                            Expression.Invoke(innerMapperVar, recordParam),
                            typeof(U)
                        ),
                        recordParam)
                ),
                columnNamesParam, rootMapperParam).Compile();
        }
    }
}
