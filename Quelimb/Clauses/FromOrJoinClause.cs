using Quelimb.SqlGenerators;

namespace Quelimb
{
    public abstract class FromOrJoinClause
    {
        public string Alias { get; }

        protected FromOrJoinClause(string alias)
        {
            this.Alias = alias;
        }

        // TODO: 最初からテーブル名持っていていい気がしてきた
        public abstract string CreateSql(string tableName, ISqlGenerator generator);
    }
}
