using System;
using System.Data;
using System.Linq.Expressions;

namespace Quelimb.TableMappers
{
    public class DelegateDbToObjectMapper<T> : ICustomDbToObjectMapper
    {
        public int NumberOfColumnsUsed { get; }

        public Func<IDataRecord, int, DbToObjectMapper, T> MapFromDb { get; }

        public DelegateDbToObjectMapper(int numberOfColumnsUsed, Func<IDataRecord, int, DbToObjectMapper, T> mapFromDb)
        {
            if (numberOfColumnsUsed <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(numberOfColumnsUsed), numberOfColumnsUsed,
                    "numberOfColumnsUsed must be larger than zero.");

            this.NumberOfColumnsUsed = numberOfColumnsUsed;
            this.MapFromDb = mapFromDb ?? throw new ArgumentNullException(nameof(mapFromDb));
        }

        public DelegateDbToObjectMapper(Func<IDataRecord, int, DbToObjectMapper, T> mapFromDb)
            : this(1, mapFromDb)
        { }

        bool ICustomDbToObjectMapper.CanMapFromDb(Type objectType)
        {
            Check.NotNull(objectType, nameof(objectType));
            return objectType.IsAssignableFrom(typeof(T));
        }

        int ICustomDbToObjectMapper.GetNumberOfColumnsUsed(Type objectType, DbToObjectMapper rootMapper)
        {
            return this.NumberOfColumnsUsed;
        }

        Func<IDataRecord, int, DbToObjectMapper, U> ICustomDbToObjectMapper.CreateMapperFromDb<U>()
        {
            if (Equals(typeof(T), typeof(U)))
                return (Func<IDataRecord, int, DbToObjectMapper, U>)(object)this.MapFromDb;

            if (!typeof(U).IsAssignableFrom(typeof(T)))
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
