using System;
using System.Collections.Generic;
using Quelimb.SqlGenerators;

namespace Quelimb
{
    public abstract class FromOrJoinClause
    {
        public Type TableType { get; }
        public string TableName { get; }
        public string Alias { get; }

        protected FromOrJoinClause(Type tableType, string tableName, string alias)
        {
            this.TableType = tableType;
            this.TableName = tableName;
            this.Alias = alias;
        }

        public abstract IEnumerable<StringOrFormattableString> CreateSql(ISqlGenerator generator);
    }
}
