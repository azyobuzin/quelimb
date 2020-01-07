using System.Text;
using Quelimb.SqlGenerators;

namespace Quelimb
{
    public class FromClause : FromOrJoinClause
    {
        public bool Only { get; }

        public FromClause(string alias, bool only)
            : base(alias)
        {
            this.Only = only;
        }

        public override string CreateSql(string tableName, ISqlGenerator generator)
        {
            var sb = new StringBuilder();
            if (this.Only) sb.Append("ONLY ");
            sb.Append(generator.EscapeIdentifier(tableName));
            if (this.Alias != null) sb.Append(" AS ").Append(generator.EscapeIdentifier(this.Alias));
            return sb.ToString();
        }

        public override string ToString()
        {
            var sb = new StringBuilder("FROM");
            if (this.Only) sb.Append(" ONLY");
            if (this.Alias != null) sb.Append(" AS ").Append(this.Alias);
            return sb.ToString();
        }
    }
}
