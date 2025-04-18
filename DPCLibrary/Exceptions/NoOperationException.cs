using System;
using System.Runtime.Serialization;

namespace DPCLibrary.Exceptions
{
    [Serializable]
    public class NoOperationException : Exception
    {
        public NoOperationException()
        {
        }

        public NoOperationException(string message) : base(message)
        {
        }

        public NoOperationException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected NoOperationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
