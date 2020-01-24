using System.Globalization;
using System.Text;

namespace Quelimb.SqlGenerators
{
    public class SqlGenerator
    {
        public static SqlGenerator Default { get; } = new SqlGenerator();

        public virtual void EscapeIdentifier(string identifier, StringBuilder destination)
        {
            Check.NotNull(identifier, nameof(identifier));
            Check.NotNull(destination, nameof(destination));

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
        /// <returns>The parameter name which will be set to <see cref="System.Data.IDataParameter.ParameterName"/>.</returns>
        public virtual string AddParameterToQuery(int parameterIndex, StringBuilder queryDestination)
        {
            var initialLength = queryDestination.Length;
            queryDestination.Append("@QuelimbParam").Append(parameterIndex.ToString(CultureInfo.InvariantCulture));
            var parameterName = queryDestination.ToString(initialLength, queryDestination.Length - initialLength);
            return parameterName;
        }
    }
}
