using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Dawn;
using Quelimb.SqlGenerators;

namespace Quelimb
{
    public class JoinClause : FromOrJoinClause
    {
        /// <summary>
        /// INNER, LEFT, RIGHT, JOIN, ...
        /// </summary>
        public string JoinType { get; }

        public StringOrFormattableString OnCondition { get; }

        public ImmutableArray<string> UsingColumns { get; }

        public JoinClause(Type tableType, string tableName, string alias, string joinType,
            StringOrFormattableString onCondition, ImmutableArray<string> usingColumns)
            : base(tableType, tableName, alias)
        {
            Guard.Argument(joinType, nameof(joinType)).NotNull().NotEmpty();

            this.JoinType = joinType;
            this.OnCondition = onCondition;
            this.UsingColumns = usingColumns;
        }

        public override IEnumerable<StringOrFormattableString> CreateSql(ISqlGenerator generator)
        {
            Guard.Argument(generator, nameof(generator)).NotNull();
            return Iterator();

            IEnumerable<StringOrFormattableString> Iterator()
            {
                var sb = new StringBuilder(this.JoinType)
                  .Append(" JOIN ")
                  .Append(generator.EscapeIdentifier(this.TableName));

                if (this.Alias != null)
                    sb.Append(" AS ").Append(generator.EscapeIdentifier(this.Alias));

                if (!this.OnCondition.IsDefault)
                {
                    sb.Append(" ON ");

                    if (this.OnCondition.IsFormattable)
                    {
                        yield return new StringOrFormattableString(sb.ToString());
                        sb.Clear();
                        yield return this.OnCondition;
                    }
                    else
                    {
                        sb.Append(this.OnCondition.String);
                    }
                }

                if (!this.UsingColumns.IsDefaultOrEmpty)
                {
                    sb.Append(" USING (");

                    var first = true;
                    foreach (var column in this.UsingColumns)
                    {
                        if (first) first = false;
                        else sb.Append(", ");
                        sb.Append(generator.EscapeIdentifier(column));
                    }

                    sb.Append(")");
                }

                yield return new StringOrFormattableString(sb.ToString());
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder(this.JoinType)
               .Append(" JOIN");

            if (this.Alias != null)
                sb.Append(" AS ").Append(this.Alias);

            if (!this.OnCondition.IsDefault)
                sb.Append(" ON ").Append(this.OnCondition.ToString());

            if (!this.UsingColumns.IsDefaultOrEmpty)
            {
                sb.Append(" USING (");

                var first = true;
                foreach (var column in this.UsingColumns)
                {
                    if (first) first = false;
                    else sb.Append(", ");
                    sb.Append(column);
                }

                sb.Append(")");
            }

            return sb.ToString();
        }
    }
}
