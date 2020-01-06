using System;

namespace Quelimb.TableInfoProviders
{
    public interface ITableInfoProvider
    {
        ITableInfo GetTableInfoByType(Type tableType);
    }
}
