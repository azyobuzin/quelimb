using Dawn;

namespace Quelimb.SqlGenerators
{
    public class SqlGenerator
    {
        public static SqlGenerator Instance { get; } = new SqlGenerator();

        public virtual string EscapeIdentifier(string identifier)
        {
            Guard.Argument(identifier, nameof(identifier)).NotNull();
            return "\"" + identifier.Replace("\"", "\"\"") + "\"";
        }
    }
}
