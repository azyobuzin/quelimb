using System.Data;

namespace Quelimb
{
    public delegate TRecord RecordConverter<TRecord>(IDataRecord source);
}
