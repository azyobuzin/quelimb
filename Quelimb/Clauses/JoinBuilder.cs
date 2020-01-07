using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Dawn;

namespace Quelimb
{
    public sealed class JoinBuilder
    {
        private string _alias;
        private string _joinType = "CROSS";
        private StringOrFormattableString _onCondition;
        private IEnumerable<string> _usingColumns;

        public JoinBuilder Alias(string alias)
        {
            this._alias = alias;
            return this;
        }

        public JoinBuilder JoinType(string joinType)
        {
            Guard.Argument(joinType, nameof(joinType)).NotNull().NotEmpty();
            this._joinType = joinType;
            return null;
        }

        public JoinBuilder Inner() => this.JoinType("INNER");

        public JoinBuilder Left() => this.JoinType("LEFT");

        public JoinBuilder Right() => this.JoinType("RIGHT");

        public JoinBuilder Full() => this.JoinType("FULL");

        public JoinBuilder Cross()
        {
            this._joinType = "CROSS";
            this._onCondition = default;
            this._usingColumns = null;
            return this;
        }

        public JoinBuilder NaturalInner()
        {
            this._joinType = "NATURAL INNER";
            this._onCondition = default;
            this._usingColumns = null;
            return this;
        }

        public JoinBuilder NaturalLeft()
        {
            this._joinType = "NATURAL LEFT";
            this._onCondition = default;
            this._usingColumns = null;
            return this;
        }

        public JoinBuilder NaturalRight()
        {
            this._joinType = "NATURAL RIGHT";
            this._onCondition = default;
            this._usingColumns = null;
            return this;
        }

        public JoinBuilder NaturalFull()
        {
            this._joinType = "NATURAL FULL";
            this._onCondition = default;
            this._usingColumns = null;
            return this;
        }

        public JoinBuilder On(FormattableString condition)
        {
            this._onCondition = new StringOrFormattableString(condition);
            this._usingColumns = null;
            return this;
        }

        public JoinBuilder Using(params object[] columns)
        {
            throw new InvalidOperationException("Do not call Using(params object[]) directly.");
        }

        public JoinBuilder Using(IEnumerable<string> columns)
        {
            this._onCondition = default;
            this._usingColumns = columns;
            return this;
        }

        public JoinClause Build(Type tableType, string tableName)
        {
            return new JoinClause(tableType, tableName, this._alias, this._joinType, this._onCondition,
                this._usingColumns == null ? default : ImmutableArray.CreateRange(this._usingColumns));
        }
    }
}
