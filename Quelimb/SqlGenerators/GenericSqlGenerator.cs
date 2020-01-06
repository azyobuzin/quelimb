namespace Quelimb.SqlGenerators
{
    public class GenericSqlGenerator : ISqlGenerator
    {
        public static GenericSqlGenerator Instance { get; } = new GenericSqlGenerator();

        public virtual string EscapeIdentifier(string identifier)
        {
            return "\"" + identifier.Replace("\"", "\"\"") + "\"";
        }
    }
}
