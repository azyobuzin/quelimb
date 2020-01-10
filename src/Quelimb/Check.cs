using System;

namespace Quelimb
{
    internal static class Check
    {
        public static void NotNull(object? value, string paramName)
        {
            if (value is null)
                throw new ArgumentNullException(paramName);
        }

        public static void NotNullOrEmpty(string? value, string paramName)
        {
            if (value == null)
                throw new ArgumentNullException(paramName);
            if (value.Length == 0)
                throw new ArgumentException(paramName + " cannot be empty.", paramName);
        }

        public static void NotNullOrEmpty(FormattableString? value, string paramName)
        {
            if (value == null)
                throw new ArgumentNullException(paramName);
            if (string.IsNullOrEmpty(value.Format))
                throw new ArgumentException(paramName + " cannot be empty.", paramName);
        }
    }
}
