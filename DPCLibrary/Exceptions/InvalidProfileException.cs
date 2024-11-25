using System;
using System.Runtime.Serialization;

namespace DPCLibrary.Exceptions
{
    [Serializable]
    public class InvalidProfileException : Exception
    {
        public InvalidProfileException()
        {
        }

        public InvalidProfileException(string message) : base(message)
        {
        }

        public InvalidProfileException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidProfileException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
