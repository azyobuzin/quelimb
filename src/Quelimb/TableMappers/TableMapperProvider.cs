using System;

namespace Quelimb.TableMappers
{
    public abstract class TableMapperProvider
    {
        public abstract TableMapper? GetTableByType(Type type);
    }
}
