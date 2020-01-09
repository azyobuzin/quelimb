using System;
using System.Data;
using Dawn;

namespace Quelimb.TableMappers
{
    // TODO: Avoid boxing
    public abstract class ValueConverter
    {
        public virtual bool CanConvertFrom(Type type) => false;

        public virtual bool CanConvertTo(Type type) => false;

        public virtual object? ConvertFrom(IDataRecord record, int columnIndex, Type type)
        {
            Guard.Argument(type, nameof(type)).NotNull();
            throw new ArgumentException($"Cannot convert from {type}.", nameof(type));
        }

        public virtual void ConvertTo(object? value, IDbDataParameter destination)
        {
            Guard.Argument(destination, nameof(destination)).NotNull();

            if (value is null)
            {
                destination.Value = DBNull.Value;
                return;
            }

            throw new ArgumentException($"Cannot convert {value} ({value.GetType()}) to IDbDataParameter.", nameof(value));
        }
    }
}
