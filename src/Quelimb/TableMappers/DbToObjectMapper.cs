using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace Quelimb.TableMappers
{
    public partial class DbToObjectMapper
    {
        private readonly ImmutableArray<ICustomDbToObjectMapper> _customMappers;
        private readonly ConcurrentDictionary<Type, Tuple<int, Delegate>?> _cache = new ConcurrentDictionary<Type, Tuple<int, Delegate>?>();
        private readonly Func<Type, Tuple<int, Delegate>?> _factory;

        protected DbToObjectMapper(IEnumerable<ICustomDbToObjectMapper> customMappers)
        {
            this._customMappers = customMappers?.ToImmutableArray() ?? ImmutableArray<ICustomDbToObjectMapper>.Empty;
            this._factory = this.CreateCustomMapperDelegate;
        }

        public static DbToObjectMapper Create(IEnumerable<ICustomDbToObjectMapper> customMappers)
        {
            customMappers = customMappers?.Concat(DefaultMappers) ?? DefaultMappers;
            return new DbToObjectMapper(customMappers);
        }

        public int GetNumberOfColumnsUsed(Type objectType)
        {
            Check.NotNull(objectType, nameof(objectType));
            return this._cache.GetOrAdd(objectType, this._factory)?.Item1 ?? 1;
        }

        public T MapFromDb<T>(IDataRecord record, int columnIndex)
        {
            Check.NotNull(record, nameof(record));
            if (columnIndex < 0) throw new ArgumentOutOfRangeException(nameof(columnIndex));

            var mapper = this.GetCustomMapper<T>();
            return mapper != null
                ? mapper(record, columnIndex, this)
                : this.MapFromDbDefault<T>(record, columnIndex);
        }

        protected virtual T MapFromDbDefault<T>(IDataRecord record, int columnIndex)
        {
            if (record is DbDataReader reader && typeof(T).IsAssignableFrom(record.GetFieldType(columnIndex)))
            {
                return reader.GetFieldValue<T>(columnIndex);
            }

            return (T)Convert.ChangeType(record.GetValue(columnIndex), typeof(T));
        }

        protected internal virtual Func<IDataRecord, int, DbToObjectMapper, T>? GetCustomMapper<T>()
        {
            return (Func<IDataRecord, int, DbToObjectMapper, T>?)this._cache.GetOrAdd(typeof(T), this._factory)?.Item2;
        }

        private Tuple<int, Delegate>? CreateCustomMapperDelegate(Type objectType)
        {
            foreach (var customMapper in this._customMappers)
            {
                if (customMapper == null) continue;

                if (customMapper.CanMapFromDb(objectType))
                {
                    var dlg = ReflectionUtils.ICustomDbToObjectMapperCreateMapperFromDbMethod
                        .MakeGenericMethod(objectType)
                        .Invoke(customMapper, null) as Delegate;

                    if (dlg == null)
                        throw new InvalidOperationException($"{customMapper.GetType()}.CreateMapperFromDb<{objectType}> returned null.");

                    return Tuple.Create(customMapper.GetNumberOfColumnsUsed(objectType, this), dlg);
                }
            }

            return null;
        }
    }
}
