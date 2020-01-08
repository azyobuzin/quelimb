namespace Quelimb.SqlGenerators
{
    public class SqlGenerator
    {
        public static SqlGenerator Instance { get; } = new SqlGenerator();

        public virtual string EscapeIdentifier(string identifier)
        {
            return "\"" + identifier.Replace("\"", "\"\"") + "\"";
        }
    }
}
