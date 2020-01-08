using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using Dawn;

namespace Quelimb.TableMappers
{
    internal sealed class DefaultCustomValueConverter : CustomValueConverter
    {
        private static CustomValueConverter? s_instance;
        public static CustomValueConverter Instance =>
            s_instance ?? (s_instance = new DefaultCustomValueConverter());

        private readonly ConcurrentDictionary<Type, Func<IDataRecord, int, ValueConverter, object?>?> _convertFromCache;

        public DefaultCustomValueConverter()
        {
            this._convertFromCache = new ConcurrentDictionary<Type, Func<IDataRecord, int, ValueConverter, object?>?>(new[]
            {
                Kvf(typeof(bool), (r, c, _) => r.GetBoolean(c)),
                Kvf(typeof(byte), (r, c, _) => r.GetByte(c)),
                Kvf(typeof(char), (r, c, _) => r.GetChar(c)),
                Kvf(typeof(DateTime), (r, c, _) => r.GetDateTime(c)),
                Kvf(typeof(decimal), (r, c, _) => r.GetDecimal(c)),
                Kvf(typeof(double), (r, c, _) => r.GetDouble(c)),
                Kvf(typeof(float), (r, c, _) => r.GetFloat(c)),
                Kvf(typeof(Guid), (r, c, _) => r.GetGuid(c)),
                Kvf(typeof(short), (r, c, _) => r.GetInt16(c)),
                Kvf(typeof(int), (r, c, _) => r.GetInt32(c)),
                Kvf(typeof(long), (r, c, _) => r.GetInt64(c)),
                Kvf(typeof(string), (r, c, _) => r.GetString(c)),
                Kvf(typeof(object), (r, c, _) => r.GetValue(c)),
                Kvf(typeof(sbyte), (r, c, _) => checked((sbyte)r.GetInt16(c))),
                Kvf(typeof(ushort), (r, c, _) => checked((ushort)r.GetInt32(c))),
                Kvf(typeof(uint), (r, c, _) => checked((uint)r.GetInt64(c))),
                Kvf(typeof(ulong), (r, c, _) => Convert.ChangeType(r.GetValue(c), typeof(ulong))),
                Kvf(typeof(DateTimeOffset), ConvertFromDateTimeOffset),
            });
        }

        public override bool CanConvertFrom(Type type, ValueConverter converter)
        {
            Guard.Argument(type, nameof(type)).NotNull();
            Guard.Argument(converter, nameof(converter)).NotNull();

            return this.LookupCacheToConvertFrom(type, converter) != null;
        }

        public override object? ConvertFrom(IDataRecord record, int columnIndex, Type type, ValueConverter converter)
        {
            Guard.Argument(record, nameof(record)).NotNull();
            Guard.Argument(type, nameof(type)).NotNull();
            Guard.Argument(converter, nameof(converter)).NotNull();

            var convertFunc = this.LookupCacheToConvertFrom(type, converter);
            if (convertFunc == null) return base.ConvertFrom(record, columnIndex, type, converter);
            return convertFunc(record, columnIndex, converter);
        }

        public override bool CanConvertTo(Type type, ValueConverter converter)
        {
            return true;
        }

        public override void ConvertTo(object? value, IDbDataParameter destination, ValueConverter converter)
        {
            Guard.Argument(destination, nameof(destination)).NotNull();

            destination.Value = value;
        }

        private static KeyValuePair<Type, Func<IDataRecord, int, ValueConverter, object?>?> Kvf(Type type, Func<IDataRecord, int, ValueConverter, object?> convertFrom)
        {
            return new KeyValuePair<Type, Func<IDataRecord, int, ValueConverter, object?>?>(type, convertFrom);
        }

        private Func<IDataRecord, int, ValueConverter, object?>? LookupCacheToConvertFrom(Type type, ValueConverter converter)
        {
            return this._convertFromCache.GetOrAdd(
                type,
                key =>
                {
                    Type? underlyingType = Nullable.GetUnderlyingType(key);
                    return underlyingType != null && converter.CanConvertFrom(underlyingType)
                        ? (r, c, v) => v.ConvertFrom(r, c, underlyingType!)
                        : (Func<IDataRecord, int, ValueConverter, object?>?)null;
                });
        }

        private static object ConvertFromDateTimeOffset(IDataRecord record, int columnIndex, ValueConverter _)
        {
            var fieldType = record.GetFieldType(columnIndex);
            return Equals(fieldType, typeof(DateTimeOffset)) ? record.GetValue(columnIndex)
                : Equals(fieldType, typeof(string)) ? DateTimeOffset.Parse(record.GetString(columnIndex))
                : new DateTimeOffset(record.GetDateTime(columnIndex));
        }
    }
}
