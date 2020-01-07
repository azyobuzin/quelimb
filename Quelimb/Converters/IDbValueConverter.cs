using System;
using System.Data;

namespace Quelimb.Converters
{
    // TODO: Avoid boxing
    public interface IDbValueConverter
    {
        bool CanConvert(Type type);

        object GetValueFromDbRecord(IDataRecord record, int ordinal);

        void SetValueToDbParameter(object value, IDbDataParameter parameter);
    }
}
