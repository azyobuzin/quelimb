using System;
using System.Data;

namespace Quelimb.TableMappers
{
    // TODO: Avoid boxing
    public abstract class ValueConverter
    {
        public abstract bool CanConvertFrom(Type type);

        public abstract bool CanConvertTo(Type type);

        public abstract object ConvertFrom(IDataRecord record, int columnIndex, Type type);

        public abstract void ConvertTo(object value, IDbDataParameter destination);
    }
}
