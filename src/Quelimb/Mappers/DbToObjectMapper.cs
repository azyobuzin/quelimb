using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;

namespace Quelimb.Mappers
{
    public partial class DbToObjectMapper
    {
        private static DbToObjectMapper? s_default;
        public static DbToObjectMapper Default => s_default ?? (s_default = Create(null));

        private ImmutableList<ICustomDbToObjectMapper> _customMappers;
        private readonly ConcurrentDictionary<Type, int?> _numberOfColumnsCache = new ConcurrentDictionary<Type, int?>();
        private readonly ConcurrentDictionary<Type, Tuple<Delegate?, Func<IDataRecord, int, DbToObjectMapper, object?>?>?> _mapFromDbCache =
            new ConcurrentDictionary<Type, Tuple<Delegate?, Func<IDataRecord, int, DbToObjectMapper, object?>?>?>();
        private readonly ConcurrentDictionary<Type, Delegate?> _mapFromRecordCache = new ConcurrentDictionary<Type, Delegate?>();
        private readonly ConcurrentDictionary<Type, IQueryableTable?> _queryableTableCache = new ConcurrentDictionary<Type, IQueryableTable?>();
        private readonly Func<Type, int?> _getNumberOfColumnsUsedCore;
        private readonly Func<Type, Tuple<Delegate?, Func<IDataRecord, int, DbToObjectMapper, object?>?>?> _createMapFromDbDelegate;
        private readonly Func<Type, Delegate?> _createMapFromRecordDelegate;
        private readonly Func<Type, IQueryableTable?> _getQueryableCore;

        protected DbToObjectMapper(IEnumerable<ICustomDbToObjectMapper>? customMappers)
        {
            this._customMappers = customMappers?.ToImmutableList() ?? ImmutableList<ICustomDbToObjectMapper>.Empty;
            this._getNumberOfColumnsUsedCore = this.GetNumberOfColumnsUsedCore;
            this._createMapFromDbDelegate = this.CreateMapFromDbDelegate;
            this._createMapFromRecordDelegate = this.CreateMapFromRecordDelegate;
            this._getQueryableCore = this.GetQueryableTableCore;
        }

        public static DbToObjectMapper Create(IEnumerable<ICustomDbToObjectMapper>? customMappers)
        {
            customMappers = customMappers?.Concat(DefaultMappers) ?? DefaultMappers;
            return new DbToObjectMapper(customMappers);
        }

        public int GetNumberOfColumnsUsed(Type objectType)
        {
            Check.NotNull(objectType, nameof(objectType));
            var n = this._numberOfColumnsCache.GetOrAdd(
                objectType,
                this._getNumberOfColumnsUsedCore);
            return n ?? 1;
        }

        public T MapFromDb<T>(IDataRecord record, int columnIndex)
        {
            Check.NotNull(record, nameof(record));
            if (columnIndex < 0) throw new ArgumentOutOfRangeException(nameof(columnIndex));

            var (genericMapper, boxedMapper) = this.GetCustomMapper<T>();
            return genericMapper != null ? genericMapper(record, columnIndex, this)
                : boxedMapper != null ? (T)boxedMapper(record, columnIndex, this)!
                : this.MapFromDbDefault<T>(record, columnIndex);
        }

        public IQueryableTable? GetQueryableTable(Type objectType)
        {
            Check.NotNull(objectType, nameof(objectType));
            return this._queryableTableCache.GetOrAdd(objectType, this._getQueryableCore);
        }

        private static readonly MethodInfo s_mapFromDbMethod = typeof(DbToObjectMapper).GetMethod(nameof(MapFromDb));

        // This method will be deleted when improving QueryAnalyzer has been completed.
        internal object? MapFromDbBoxed(Type type, IDataRecord record, int columnIndex)
        {
            return s_mapFromDbMethod.MakeGenericMethod(type).Invoke(this, new object[] { record, columnIndex });
        }

        public Func<IDataRecord, T> CreateMapperFromRecord<T>(IReadOnlyList<string> columnNames)
        {
            Check.NotNull(columnNames, nameof(columnNames));

            var recordMapper = (Func<IReadOnlyList<string>, DbToObjectMapper, Func<IDataRecord, T>>?)
                this._mapFromRecordCache.GetOrAdd(typeof(T), this._createMapFromRecordDelegate);
            if (recordMapper != null) return recordMapper(columnNames, this);

            var (genericMapper, boxedMapper) = this.GetCustomMapper<T>();
            if (genericMapper != null) return record => genericMapper(record, 0, this);
            if (boxedMapper != null) return record => (T)boxedMapper(record, 0, this)!;

            return record => this.MapFromDbDefault<T>(record, 0);
        }

        protected virtual T MapFromDbDefault<T>(IDataRecord record, int columnIndex)
        {
            if (record is DbDataReader reader && typeof(T).IsAssignableFrom(record.GetFieldType(columnIndex)))
            {
                return reader.GetFieldValue<T>(columnIndex);
            }

            return (T)Convert.ChangeType(record.GetValue(columnIndex), typeof(T));
        }

        protected internal virtual (Func<IDataRecord, int, DbToObjectMapper, T>? genericMapper, Func<IDataRecord, int, DbToObjectMapper, object?>? boxedMapper) GetCustomMapper<T>()
        {
            var t = this._mapFromDbCache.GetOrAdd(typeof(T), this._createMapFromDbDelegate);
            return ((Func<IDataRecord, int, DbToObjectMapper, T>?)t?.Item1, t?.Item2);
        }

        /// <summary>
        /// Creates a custom mapper for <paramref name="objectType"/>.
        /// This method is called when there is no custom mapper for <paramref name="objectType"/>.
        /// </summary>
        protected virtual ICustomDbToObjectMapper? TryCreateCustomMapper(Type objectType)
        {
            Check.NotNull(objectType, nameof(objectType));

            // TODO: these can be single CustomDbToObjectMapper

            var isTable = objectType.IsDefined(typeof(TableAttribute));
            var isProjectedTuple = objectType.IsDefined(typeof(ProjectedTupleAttribute));

            if (isTable && isProjectedTuple)
                throw new InvalidProjectedObjectException($"{objectType} has both TableAttribute and ProjectedTupleAttribute.");

            if (isTable) return TableToObjectMapper.Create(objectType);
            if (isProjectedTuple) return ProjectedTupleToObjectMapper.Create(objectType);

            // Tuple or ValueTuple
            if (objectType.IsConstructedGenericType && ReflectionUtils.TupleTypes.Contains(objectType.GetGenericTypeDefinition()))
            {
                var tupleSize = objectType.GetGenericArguments().Length;
                var constructor = objectType.GetConstructors()
                    .Single(x => x.GetParameters().Length == tupleSize);
                return ProjectedTupleToObjectMapper.CreateWithConstructor(objectType, constructor, false);
            }

            return null;
        }

        private int? GetNumberOfColumnsUsedCore(Type objectType)
        {
            return this.FindCustomMapper(objectType)
                ?.GetNumberOfColumnsUsed(objectType, this);
        }

        private Tuple<Delegate?, Func<IDataRecord, int, DbToObjectMapper, object?>?>? CreateMapFromDbDelegate(Type objectType)
        {
            var customMapper = this.FindCustomMapper(objectType);
            if (customMapper == null) return null;

            Delegate? dlg = null;

            if (customMapper is IGenericDbToObjectMapperProvider)
            {
                dlg = ReflectionUtils.IGenericDbToObjectMapperProviderCreateMapperFromDbMethod
                    .MakeGenericMethod(objectType)
                    .Invoke(customMapper, null) as Delegate;
            }

            return Tuple.Create(
                dlg,
                dlg == null
                    ? (r, c, m) => customMapper.MapFromDb(objectType, r, c, m)
                    : (Func<IDataRecord, int, DbToObjectMapper, object?>?)null);
        }

        private Delegate? CreateMapFromRecordDelegate(Type objectType)
        {
            var customMapper = this.FindCustomMapper(objectType);
            if (customMapper == null) return null;

            if (!(customMapper is IRecordToObjectMapperProvider))
                return null;

            return ReflectionUtils.IRecordToObjectMapperProviderCreateMapperFromRecordMethod
                .MakeGenericMethod(objectType)
                .Invoke(customMapper, null) as Delegate;
        }

        private IQueryableTable? GetQueryableTableCore(Type objectType)
        {
            return (this.FindCustomMapper(objectType) as IQueryableTableProvider)
               ?.GetQueryableTable(objectType);
        }

        private ICustomDbToObjectMapper? FindCustomMapper(Type targetType)
        {
            foreach (var customMapper in this._customMappers)
            {
                if (customMapper?.CanMapFromDb(targetType) == true)
                    return customMapper;
            }

            var newMapper = this.TryCreateCustomMapper(targetType);
            if (newMapper == null) return null;

            // Add to _customMappers
            ImmutableInterlocked.Update(ref this._customMappers, (l, m) => l.Add(m), newMapper);

            if (newMapper.CanMapFromDb(targetType))
                return newMapper;

            throw new InvalidOperationException("A custom mapper created by TryCreateCustomMapper cannot map to the specified type.");
        }
    }
}
