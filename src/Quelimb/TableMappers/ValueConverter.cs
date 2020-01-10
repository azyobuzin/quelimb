using System;
using System.Data;

namespace Quelimb.TableMappers
{
    // TODO: Avoid boxing
    // TODO: We can use a value IDataRecord.GetValue returns if the type of the value is matched.
    public abstract class ValueConverter
    {
        public virtual bool CanConvertFrom(Type type) => false;

        public virtual bool CanConvertTo(Type type) => false;

        public virtual object? ConvertFrom(IDataRecord record, int columnIndex, Type type)
        {
            Check.NotNull(type, nameof(type));
            throw new ArgumentException($"Cannot convert from {type}.", nameof(type));
        }

        public virtual void ConvertTo(object? value, IDbDataParameter destination)
        {
            Check.NotNull(destination, nameof(destination));

            if (value is null)
            {
                destination.Value = DBNull.Value;
                return;
            }

            throw new ArgumentException($"Cannot convert {value} ({value.GetType()}) to IDbDataParameter.", nameof(value));
        }
    }
}
