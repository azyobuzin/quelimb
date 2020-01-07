using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Quelimb.Converters
{
    public class DefaultConverterProvider : IConverterProvider
    {
        private readonly ImmutableArray<Func<Type, ITableInfo>> _tableInfoProviders;
        private readonly ImmutableArray<IDbValueConverter> _valueConverters;
        private readonly ConcurrentDictionary<Type, ITableInfo> _tableInfoCache = new ConcurrentDictionary<Type, ITableInfo>();
        private readonly ConcurrentDictionary<Type, IDbValueConverter> _valueConverterCache = new ConcurrentDictionary<Type, IDbValueConverter>();

        public DefaultConverterProvider(IEnumerable<Func<Type, ITableInfo>> tableInfoProviders, IEnumerable<IDbValueConverter> valueConverters)
        {
            this._tableInfoProviders = tableInfoProviders == null
                ? ImmutableArray<Func<Type, ITableInfo>>.Empty
                : ImmutableArray.CreateRange(tableInfoProviders);
            this._valueConverters = valueConverters == null
                ? ImmutableArray<IDbValueConverter>.Empty
                : ImmutableArray.CreateRange(valueConverters);
        }

        public ITableInfo GetTableInfoByType(Type tableType)
        {
            throw new NotImplementedException();
        }

        public RecordConverter<TRecord> CreateRecordConverter<TRecord>(Type type)
        {
            throw new NotImplementedException();
        }
    }
}
