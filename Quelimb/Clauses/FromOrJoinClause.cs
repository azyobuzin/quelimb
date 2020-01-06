namespace Quelimb
{
    public abstract class FromOrJoinClause
    {
        public string Alias { get; }

        protected FromOrJoinClause(string alias)
        {
            this.Alias = alias;
        }
    }
}
