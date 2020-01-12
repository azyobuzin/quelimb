using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;

namespace Quelimb.TableMappers
{
    public class ObjectToDbMapper
    {
        private readonly ImmutableArray<ICustomObjectToDbMapper> _customMappers;
        private readonly ConcurrentDictionary<Type, Delegate?> _cache = new ConcurrentDictionary<Type, Delegate?>();
        private readonly Func<Type, Delegate?> _factory;

        public ObjectToDbMapper(IEnumerable<ICustomObjectToDbMapper> customMappers)
        {
            this._customMappers = customMappers?.ToImmutableArray() ?? ImmutableArray<ICustomObjectToDbMapper>.Empty;
            this._factory = this.CreateCustomMapperDelegate;
        }

        public void MapToDb<T>(T obj, IDbDataParameter destination)
        {
            Check.NotNull(destination, nameof(destination));

            var mapper = this.GetCustomMapper<T>();
            if (mapper != null) mapper(obj, destination, this);
            else this.MapToDbDefault(obj, destination);
        }

        protected virtual void MapToDbDefault(object? obj, IDbDataParameter destination)
        {
            Check.NotNull(destination, nameof(destination));
            destination.Value = obj;
        }

        protected internal virtual Action<T, IDbDataParameter, ObjectToDbMapper>? GetCustomMapper<T>()
        {
            return (Action<T, IDbDataParameter, ObjectToDbMapper>?)this._cache.GetOrAdd(typeof(T), this._factory);
        }

        private Delegate? CreateCustomMapperDelegate(Type objectType)
        {
            foreach (var customMapper in this._customMappers)
            {
                if (customMapper == null) continue;

                if (customMapper.CanMapToDb(objectType))
                {
                    var dlg = ReflectionUtils.ICustomObjectToDbMapperCreateMapperToDbMethod
                        .MakeGenericMethod(objectType)
                        .Invoke(customMapper, null) as Delegate;

                    if (dlg == null)
                        throw new InvalidOperationException($"{customMapper.GetType()}.CreateMapperToDb<{objectType}> returned null.");

                    return dlg;
                }
            }

            return null;
        }
    }
}
