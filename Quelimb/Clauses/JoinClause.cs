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

        public string OnCondition { get; }

        public ImmutableArray<string> UsingColumns { get; }

        public JoinClause(string alias, string joinType)
            : base(alias)
        {
            Guard.Argument(joinType, nameof(joinType)).NotNull().NotEmpty();

            this.JoinType = joinType;
        }

        public JoinClause(string alias, string joinType, string onCondition)
            : base(alias)
        {
            Guard.Argument(joinType, nameof(joinType)).NotNull().NotEmpty();

            this.JoinType = joinType;
            this.OnCondition = onCondition;
        }

        public JoinClause(string alias, string joinType, ImmutableArray<string> usingColumns)
            : base(alias)
        {
            Guard.Argument(joinType, nameof(joinType)).NotNull().NotEmpty();

            this.JoinType = joinType;
            this.UsingColumns = usingColumns;
        }

        public static JoinClause Inner(string alias = null)
        {
            return new JoinClause(alias, "INNER");
        }

        public static JoinClause LeftOuter(string alias = null)
        {
            return new JoinClause(alias, "LEFT");
        }

        public static JoinClause RightOuter(string alias = null)
        {
            return new JoinClause(alias, "RIGHT");
        }

        public static JoinClause Cross(string alias = null)
        {
            return new JoinClause(alias, "CROSS");
        }

        public JoinClause On(string joinCondition)
        {
            return new JoinClause(this.Alias, this.JoinType, joinCondition);
        }

        public JoinClause Using(IEnumerable<string> columns)
        {
            var columnsArray = columns == null ? default : ImmutableArray.CreateRange(columns);
            return new JoinClause(this.Alias, this.JoinType, columnsArray);
        }

        public JoinClause Using(params string[] columns)
        {
            return this.Using((IEnumerable<string>)columns);
        }

        public JoinClause Natural()
        {
            return new JoinClause(this.Alias, "NATURAL " + this.JoinType);
        }

        public virtual string CreateSql(string tableName, ISqlGenerator generator)
        {
            Guard.Argument(tableName, nameof(tableName)).NotNull();
            Guard.Argument(generator, nameof(generator)).NotNull();

            var sb = new StringBuilder(this.JoinType)
                .Append(" JOIN")
                .Append(generator.EscapeIdentifier(tableName));

            if (this.Alias != null)
                sb.Append(" AS ").Append(generator.EscapeIdentifier(this.Alias));

            if (this.OnCondition != null)
                sb.Append(" ON ").Append(this.OnCondition);

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

            return sb.ToString();
        }

        public override string ToString()
        {
            var sb = new StringBuilder(this.JoinType)
               .Append(" JOIN");

            if (this.Alias != null)
                sb.Append(" AS ").Append(this.Alias);

            if (this.OnCondition != null)
                sb.Append(" ON ").Append(this.OnCondition);

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
