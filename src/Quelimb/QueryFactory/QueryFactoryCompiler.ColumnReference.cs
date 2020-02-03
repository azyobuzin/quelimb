namespace Quelimb.QueryFactory
{
	partial class QueryFactoryCompiler
	{
		internal sealed class ColumnReference
		{
			public TableReference Table { get; }
			public string ColumnName { get; }

			public ColumnReference(TableReference table, string columnName)
			{
				this.Table = table;
				this.ColumnName = columnName;
			}

			public override string ToString()
			{
				return $"ColumnReference({this.ColumnName}, {this.Table})";
			}
		}
	}
}
