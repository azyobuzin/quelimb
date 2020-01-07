using System;

namespace Quelimb.Converters
{
    public interface IConverterProvider
    {
        ITableInfo GetTableInfoByType(Type tableType);
        RecordConverter<TRecord> CreateRecordConverter<TRecord>(Type type);
    }
}
