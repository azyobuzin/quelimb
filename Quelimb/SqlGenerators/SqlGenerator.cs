using System.Text;
using Dawn;

namespace Quelimb.SqlGenerators
{
    public class SqlGenerator
    {
        public static SqlGenerator Instance { get; } = new SqlGenerator();

        public virtual void EscapeIdentifier(string identifier, StringBuilder destination)
        {
            Guard.Argument(identifier, nameof(identifier)).NotNull();
            Guard.Argument(destination, nameof(destination)).NotNull();

            destination.Append('"');
            var startIndex = destination.Length;
            destination.Append(identifier);
            destination.Replace("\"", "\"\"", startIndex, destination.Length - startIndex);
            destination.Append('"');
        }
    }
}
