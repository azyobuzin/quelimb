using System;
using System.Collections.Generic;
using System.Text;
using Dawn;
using Quelimb.SqlGenerators;

namespace Quelimb
{
    public class FromClause : FromOrJoinClause
    {
        public bool Only { get; }

        public FromClause(Type tableType, string tableName, string alias, bool only)
            : base(tableType, tableName, alias)
        {
            this.Only = only;
        }

        public override IEnumerable<StringOrFormattableString> CreateSql(ISqlGenerator generator)
        {
            Guard.Argument(generator, nameof(generator)).NotNull();

            var sb = new StringBuilder();
            if (this.Only) sb.Append("ONLY ");
            sb.Append(generator.EscapeIdentifier(this.TableName));
            if (this.Alias != null) sb.Append(" AS ").Append(generator.EscapeIdentifier(this.Alias));
            return new[] { new StringOrFormattableString(sb.ToString()) };
        }

        public override string ToString()
        {
            var sb = new StringBuilder("FROM ");
            if (this.Only) sb.Append("ONLY ");
            sb.Append(this.TableName);
            if (this.Alias != null) sb.Append(" AS ").Append(this.Alias);
            return sb.ToString();
        }
    }
}
