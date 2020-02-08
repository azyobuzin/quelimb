using Quelimb.Mappers;

namespace Quelimb.QueryFactory
{
    internal sealed class TableReference
    {
        public IQueryableTable Table { get; }
        public string ParameterName { get; }
        public string? EscapedAlias { get; set; }

        public TableReference(IQueryableTable table, string parameterName)
        {
            this.Table = table;
            this.ParameterName = parameterName;
        }

        public override string ToString()
        {
            var s = this.Table.ToString();
            return this.EscapedAlias == null
                ? $"{this.ParameterName} : {s}"
                : $"{s} AS {this.EscapedAlias}";
        }
    }
}
