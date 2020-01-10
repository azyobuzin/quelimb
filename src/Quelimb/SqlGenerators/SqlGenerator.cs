using System.Globalization;
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

        /// <param name="parameterIndex">An index of the parameter in the query.</param>
        /// <param name="queryDestination">
        /// The destination <see cref="StringBuilder"/>.
        /// This method appends a placeholder representing the parameter to <paramref name="queryDestination"/>.
        /// </param>
        /// <returns>The parameter name which will be set to <see cref="System.Data.IDataParameter.ParameterName">.</returns>
        public virtual string AddParameterToQuery(int parameterIndex, StringBuilder queryDestination)
        {
            var parameterName = "@QuelimbParam" + parameterIndex.ToString(CultureInfo.InvariantCulture);
            queryDestination.Append(parameterName);
            return parameterName;
        }
    }
}
