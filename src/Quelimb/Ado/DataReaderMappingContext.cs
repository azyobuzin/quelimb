using System.Collections.Generic;
using System.Data.Common;

namespace Quelimb.Ado
{
    public class DataReaderMappingContext : MappingContext
    {
        private readonly DbDataReader _dataReader;
        private string[]? _columnNames;

        public DataReaderMappingContext(DbDataReader dataReader)
        {
            this._dataReader = dataReader;
        }

        public override int ColumnCount => this._dataReader.FieldCount;

        public override IReadOnlyList<string>? ColumnNames
        {
            get
            {
                if (this._columnNames == null)
                {
                    var arr = new string[this.ColumnCount];
                    for (var i = 0; i < arr.Length; i++)
                        arr[i] = this._dataReader.GetName(i);
                    this._columnNames = arr;
                }

                return this._columnNames;
            }
        }
    }
}
