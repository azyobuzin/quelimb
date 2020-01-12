using System;
using System.Data;

namespace Quelimb.TableMappers
{
    public interface IRecordToObjectMapper
    {
        Func<IDataRecord, DbToObjectMapper, T> CreateMapperFromRecord<T>();
    }
}
