using System;
using System.Data;

namespace Quelimb.TableMappers
{
    public abstract class CustomValueConverter
    {
        public virtual bool CanConvertFrom(Type type, ValueConverter converter) => false;

        public virtual bool CanConvertTo(Type type, ValueConverter converter) => false;

        public virtual object ConvertFrom(IDataRecord record, int columnIndex, Type type, ValueConverter converter)
        {
            throw new ArgumentException($"Cannot convert from {type}.", nameof(type));
        }

        public virtual void ConvertTo(object value, IDbDataParameter destination, ValueConverter converter)
        {
            if (value is null)
            {
                destination.Value = DBNull.Value;
                return;
            }

            throw new ArgumentException($"Cannot convert {value} ({value.GetType()}) to IDbDataParameter.", nameof(value));
        }
    }
}
