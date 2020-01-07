using System;
using System.Collections.Generic;

namespace Quelimb.Converters
{
    public class ConverterConfiguration
    {
        public List<Func<Type, ITableInfo>> TableInfoProviders { get; } = new List<Func<Type, ITableInfo>>();
        public List<IDbValueConverter> ValueConverters { get; } = new List<IDbValueConverter>();

        public IConverterProvider BuildConverterProvider()
        {
            return new DefaultConverterProvider(this.TableInfoProviders, this.ValueConverters);
        }
    }
}
