using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using Dawn;

namespace Quelimb.TableMappers
{
    public class DefaultValueConverter : ValueConverter
    {
        private static ValueConverter? s_default;
        public static ValueConverter Default => s_default ?? (s_default = new DefaultValueConverter(null));

        protected ImmutableArray<CustomValueConverter> CustomConverters { get; }
        private readonly ConcurrentDictionary<Type, CustomValueConverter?> _convertFromCache = new ConcurrentDictionary<Type, CustomValueConverter?>();
        private readonly ConcurrentDictionary<Type, CustomValueConverter?> _convertToCache = new ConcurrentDictionary<Type, CustomValueConverter?>();
        private readonly Func<Type, CustomValueConverter?> _getConverterToConvertFrom;
        private readonly Func<Type, CustomValueConverter?> _getConverterToConvertTo;

        public DefaultValueConverter(IEnumerable<CustomValueConverter>? customConverters)
        {
            this.CustomConverters = customConverters?.ToImmutableArray() ?? ImmutableArray<CustomValueConverter>.Empty;
            this._getConverterToConvertFrom = this.GetConverterToConvertFrom;
            this._getConverterToConvertTo = this.GetConverterToConvertTo;
        }

        public override bool CanConvertFrom(Type type)
        {
            Guard.Argument(type, nameof(type)).NotNull();
            return this.LookupCacheToConvertFrom(type) != null;
        }

        public override bool CanConvertTo(Type type)
        {
            Guard.Argument(type, nameof(type)).NotNull();
            return this.LookupCacheToConvertTo(type) != null;
        }

        public override object? ConvertFrom(IDataRecord record, int columnIndex, Type type)
        {
            Guard.Argument(record, nameof(record)).NotNull();
            Guard.Argument(type, nameof(type)).NotNull();

            var converter = this.LookupCacheToConvertFrom(type);
            if (converter == null) throw new ArgumentException($"Cannot convert from {type}.", nameof(type));
            return converter.ConvertFrom(record, columnIndex, type, this);
        }

        public override void ConvertTo(object? value, IDbDataParameter destination)
        {
            if (value is null)
            {
                destination.Value = DBNull.Value;
                return;
            }

            var type = value.GetType();
            var converter = this.LookupCacheToConvertTo(value.GetType());

            if (converter == null)
                throw new ArgumentException($"Cannot convert {value} ({type}) to IDbParameter.", nameof(value));

            converter.ConvertTo(value, destination, this);
        }

        protected CustomValueConverter? LookupCacheToConvertFrom(Type type)
        {
            return this._convertFromCache.GetOrAdd(type, this._getConverterToConvertFrom);
        }

        protected CustomValueConverter? LookupCacheToConvertTo(Type type)
        {
            return this._convertToCache.GetOrAdd(type, this._getConverterToConvertTo);
        }

        protected virtual CustomValueConverter? GetConverterToConvertFrom(Type type)
        {
            foreach (var converter in this.CustomConverters)
            {
                if (converter?.CanConvertFrom(type, this) == true)
                    return converter;
            }

            var defaultConverter = DefaultCustomValueConverter.Instance;
            return defaultConverter.CanConvertFrom(type, this)
                ? defaultConverter : null;
        }

        protected virtual CustomValueConverter? GetConverterToConvertTo(Type type)
        {
            foreach (var converter in this.CustomConverters)
            {
                if (converter?.CanConvertTo(type, this) == true)
                    return converter;
            }

            var defaultConverter = DefaultCustomValueConverter.Instance;
            return defaultConverter.CanConvertTo(type, this)
                ? defaultConverter : null;
        }
    }
}
