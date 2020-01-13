using System;

namespace Quelimb
{
    public class InvalidProjectedObjectException : Exception
    {
        public InvalidProjectedObjectException(string message)
            : base(message) { }

        public InvalidProjectedObjectException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
