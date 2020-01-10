using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Runtime.CompilerServices;
using Dawn;

namespace Quelimb.TableMappers
{
    internal sealed class DefaultCustomValueConverter : CustomValueConverter
    {
        private static CustomValueConverter? s_instance;
        public static CustomValueConverter Instance =>
            s_instance ?? (s_instance = new DefaultCustomValueConverter());

        private static ImmutableDictionary<Type, Func<IDataRecord, int, object?>>? s_defaultConvertes;
        private static ImmutableDictionary<Type, Func<IDataRecord, int, object?>> DefaultConverters
        {
            get
            {
                if (s_defaultConvertes == null)
                {
                    s_defaultConvertes = ImmutableDictionary.CreateRange(new[]
                    {
                        Kvf(typeof(bool), (r, c) => r.GetBoolean(c)),
                        Kvf(typeof(byte), (r, c) => r.GetByte(c)),
                        Kvf(typeof(char), (r, c) => r.GetChar(c)),
                        Kvf(typeof(DateTime), (r, c) => r.GetDateTime(c)),
                        Kvf(typeof(decimal), (r, c) => r.GetDecimal(c)),
                        Kvf(typeof(double), (r, c) => r.GetDouble(c)),
                        Kvf(typeof(float), (r, c) => r.GetFloat(c)),
                        Kvf(typeof(Guid), (r, c) => r.GetGuid(c)),
                        Kvf(typeof(short), (r, c) => r.GetInt16(c)),
                        Kvf(typeof(int), (r, c) => r.GetInt32(c)),
                        Kvf(typeof(long), (r, c) => r.GetInt64(c)),
                        Kvf(typeof(string), (r, c) => r.IsDBNull(c) ? null : r.GetString(c)),
                        Kvf(typeof(object), (r, c) => r.GetValue(c)),
                        Kvf(typeof(sbyte), (r, c) => checked((sbyte)r.GetInt16(c))),
                        Kvf(typeof(ushort), (r, c) => checked((ushort)r.GetInt32(c))),
                        Kvf(typeof(uint), (r, c) => checked((uint)r.GetInt64(c))),
                        Kvf(typeof(ulong), (r, c) => Convert.ChangeType(r.GetValue(c), typeof(ulong))),
                        Kvf(typeof(DateTimeOffset), ConvertFromDateTimeOffset),
                        Kvf(typeof(ValueTuple), (_, __) => ValueTuple.Create()),
                    });
                }
                return s_defaultConvertes;
            }
        }

        private static readonly ConditionalWeakTable<ValueConverter, ConcurrentDictionary<Type, Func<IDataRecord, int, ValueConverter, object?>?>>
            s_converterCache = new ConditionalWeakTable<ValueConverter, ConcurrentDictionary<Type, Func<IDataRecord, int, ValueConverter, object?>?>>();

        public override bool CanConvertFrom(Type type, ValueConverter converter)
        {
            Guard.Argument(type, nameof(type)).NotNull();
            Guard.Argument(converter, nameof(converter)).NotNull();

            return DefaultConverters.ContainsKey(type) || LookupConverterCache(type, converter) != null;
        }

        public override object? ConvertFrom(IDataRecord record, int columnIndex, Type type, ValueConverter converter)
        {
            Guard.Argument(record, nameof(record)).NotNull();
            Guard.Argument(type, nameof(type)).NotNull();
            Guard.Argument(converter, nameof(converter)).NotNull();

            if (DefaultConverters.TryGetValue(type, out var defaultConverter))
                return defaultConverter(record, columnIndex);

            var convertFunc = LookupConverterCache(type, converter);
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

        private static KeyValuePair<Type, Func<IDataRecord, int, object?>> Kvf(Type type, Func<IDataRecord, int, object?> convertFrom)
        {
            return new KeyValuePair<Type, Func<IDataRecord, int, object?>>(type, convertFrom);
        }

        private static Func<IDataRecord, int, ValueConverter, object?>? LookupConverterCache(Type type, ValueConverter valueConverter)
        {
            var cacheByConverter = s_converterCache.GetOrCreateValue(valueConverter);

            if (!cacheByConverter.TryGetValue(type, out var conv))
            {
                if (type.IsEnum)
                {
                    var enumUnderlyingType = type.GetEnumUnderlyingType();
                    if (valueConverter.CanConvertFrom(enumUnderlyingType))
                        conv = (r, c, v) => v.ConvertFrom(r, c, enumUnderlyingType);
                }
                else if (Nullable.GetUnderlyingType(type) is Type nullableUnderlyingType &&
                    valueConverter.CanConvertFrom(nullableUnderlyingType))
                {
                    // Create converter for Nullable<T>.
                    // We cannot capture `valueConverter` because of memory leak.
                    conv = (r, c, v) => r.IsDBNull(c) ? null : v.ConvertFrom(r, c, nullableUnderlyingType);
                }

                cacheByConverter.TryAdd(type, conv);
            }

            return conv;
        }

        private static object ConvertFromDateTimeOffset(IDataRecord record, int columnIndex)
        {
            var fieldType = record.GetFieldType(columnIndex);
            return Equals(fieldType, typeof(DateTimeOffset)) ? record.GetValue(columnIndex)
                : Equals(fieldType, typeof(string)) ? DateTimeOffset.Parse(record.GetString(columnIndex))
                : new DateTimeOffset(record.GetDateTime(columnIndex));
        }
    }
}
