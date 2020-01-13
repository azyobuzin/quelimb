using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Reflection;

namespace Quelimb.Mappers
{
    public class ObjectToDbMapper
    {
        private readonly ImmutableArray<ICustomObjectToDbMapper> _customMappers;
        private readonly ConcurrentDictionary<Type, Tuple<Delegate?, Action<object?, IDbDataParameter, ObjectToDbMapper>?>?> _cache =
            new ConcurrentDictionary<Type, Tuple<Delegate?, Action<object?, IDbDataParameter, ObjectToDbMapper>?>?>();
        private readonly Func<Type, Tuple<Delegate?, Action<object?, IDbDataParameter, ObjectToDbMapper>?>?> _factory;

        public ObjectToDbMapper(IEnumerable<ICustomObjectToDbMapper> customMappers)
        {
            this._customMappers = customMappers?.ToImmutableArray() ?? ImmutableArray<ICustomObjectToDbMapper>.Empty;
            this._factory = this.CreateCustomMapperDelegate;
        }

        public void MapToDb<T>(T obj, IDbDataParameter destination)
        {
            Check.NotNull(destination, nameof(destination));

            var (genericMapper, boxedMapper) = this.GetCustomMapper<T>();
            if (genericMapper != null) genericMapper(obj, destination, this);
            else if (boxedMapper != null) boxedMapper(obj, destination, this);
            else this.MapToDbDefault(obj, destination);
        }

        private static readonly MethodInfo s_mapToDbMethod = typeof(ObjectToDbMapper).GetMethod(nameof(MapToDb));

        // This method will be deleted when improving QueryAnalyzer has been completed.
        internal void MapToDbBoxed(object obj, IDbDataParameter destination)
        {
            if (obj == null)
            {
                this.MapToDbDefault(null, destination);
            }
            else
            {
                s_mapToDbMethod.MakeGenericMethod(obj.GetType()).Invoke(this, new object[] { obj, destination });
            }
        }

        protected virtual void MapToDbDefault(object? obj, IDbDataParameter destination)
        {
            Check.NotNull(destination, nameof(destination));
            destination.Value = obj ?? DBNull.Value;
        }

        protected internal virtual (Action<T, IDbDataParameter, ObjectToDbMapper>? genericMapper, Action<object?, IDbDataParameter, ObjectToDbMapper>? boxedMapper) GetCustomMapper<T>()
        {
            var t = this._cache.GetOrAdd(typeof(T), this._factory);
            return ((Action<T, IDbDataParameter, ObjectToDbMapper>?)t?.Item1, t?.Item2);
        }

        private Tuple<Delegate?, Action<object?, IDbDataParameter, ObjectToDbMapper>?>? CreateCustomMapperDelegate(Type objectType)
        {
            foreach (var customMapper in this._customMappers)
            {
                if (customMapper == null) continue;

                if (customMapper.CanMapToDb(objectType))
                {
                    Delegate? dlg = null;

                    if (customMapper is IGenericObjectToDbMapperProvider)
                    {
                        dlg = ReflectionUtils.IGenericObjectToDbMapperProviderCreateMapperToDbMethod
                            .MakeGenericMethod(objectType)
                            .Invoke(customMapper, null) as Delegate;
                    }

                    return Tuple.Create(
                        dlg,
                        dlg == null ? customMapper.MapToDb : (Action<object?, IDbDataParameter, ObjectToDbMapper>?)null);
                }
            }

            return null;
        }
    }
}
