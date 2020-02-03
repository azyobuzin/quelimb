using Quelimb.Mappers;

namespace Quelimb.QueryFactory
{
    partial class QueryFactoryCompiler
    {
        internal sealed class TableReference
        {
            public IQueryableTable Table { get; }
            public string? EscapedAlias { get; set; }

            public TableReference(IQueryableTable table)
            {
                this.Table = table;
            }

            public override string ToString()
            {
                var s = this.Table.ToString();
                if (this.EscapedAlias != null)
                    s = $"{s} AS {this.EscapedAlias}";
                return s;
            }
        }
    }
}
