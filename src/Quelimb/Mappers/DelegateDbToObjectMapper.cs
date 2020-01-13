using System;
using System.Data;
using System.Linq.Expressions;

namespace Quelimb.Mappers
{
    public class DelegateDbToObjectMapper<T> : IGenericDbToObjectMapperProvider
    {
        public Func<DbToObjectMapper, int> NumberOfColumnsUsed { get; }

        public Func<IDataRecord, int, DbToObjectMapper, T> MapFromDb { get; }

        private readonly bool _acceptSuperclassOfT;

        public DelegateDbToObjectMapper(
            int numberOfColumnsUsed,
            Func<IDataRecord, int, DbToObjectMapper, T> mapFromDb,
            bool acceptSuperclassOfT = false)
        {
            if (numberOfColumnsUsed <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(numberOfColumnsUsed), numberOfColumnsUsed,
                    "numberOfColumnsUsed must be larger than zero.");

            this.NumberOfColumnsUsed = _ => numberOfColumnsUsed;
            this.MapFromDb = mapFromDb ?? throw new ArgumentNullException(nameof(mapFromDb));
            this._acceptSuperclassOfT = acceptSuperclassOfT;
        }

        public DelegateDbToObjectMapper(
            Func<IDataRecord, int, DbToObjectMapper, T> mapFromDb,
            bool acceptSuperclassOfT = false)
            : this(1, mapFromDb, acceptSuperclassOfT)
        { }

        public DelegateDbToObjectMapper(
            Func<DbToObjectMapper, int> numberOfColumnsUsed,
            Func<IDataRecord, int, DbToObjectMapper, T> mapFromDb,
            bool acceptSuperclassOfT = false)
        {
            this.NumberOfColumnsUsed = numberOfColumnsUsed ?? throw new ArgumentNullException(nameof(numberOfColumnsUsed));
            this.MapFromDb = mapFromDb ?? throw new ArgumentNullException(nameof(mapFromDb));
            this._acceptSuperclassOfT = acceptSuperclassOfT;
        }

        public bool CanMapFromDb(Type objectType)
        {
            Check.NotNull(objectType, nameof(objectType));
            return this._acceptSuperclassOfT
                ? objectType.IsAssignableFrom(typeof(T))
                : Equals(objectType, typeof(T));
        }

        int ICustomDbToObjectMapper.GetNumberOfColumnsUsed(Type objectType, DbToObjectMapper rootMapper)
        {
            return this.NumberOfColumnsUsed(rootMapper);
        }

        object? ICustomDbToObjectMapper.MapFromDb(Type objectType, IDataRecord record, int columnIndex, DbToObjectMapper rootMapper)
        {
            Check.NotNull(record, nameof(record));
            Check.NotNull(rootMapper, nameof(rootMapper));

            if (!this.CanMapFromDb(objectType))
                throw new ArgumentException($"{objectType} is not supported.", nameof(objectType));

            return this.MapFromDb(record, columnIndex, rootMapper);
        }

        Func<IDataRecord, int, DbToObjectMapper, U>? IGenericDbToObjectMapperProvider.CreateMapperFromDb<U>()
        {
            if (Equals(typeof(T), typeof(U)))
                return (Func<IDataRecord, int, DbToObjectMapper, U>)(object)this.MapFromDb;

            if (!this.CanMapFromDb(typeof(U)))
                throw new ArgumentException($"The type argument U is {typeof(U)}, which is not supported.");

            // We need to convert the return value
            var recordParam = Expression.Parameter(typeof(IDataRecord), "record");
            var columnIndexParam = Expression.Parameter(typeof(int), "columnIndex");
            var rootMapper = Expression.Parameter(typeof(DbToObjectMapper), "rootMapper");
            return
                Expression.Lambda<Func<IDataRecord, int, DbToObjectMapper, U>>(
                    Expression.Convert(
                        Expression.Invoke(
                            Expression.Constant(this.MapFromDb),
                            recordParam,
                            columnIndexParam,
                            rootMapper
                        ),
                        typeof(U)),
                    recordParam, columnIndexParam, rootMapper)
                .Compile();
        }
    }
}
