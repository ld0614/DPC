using System;
using System.Runtime.Serialization;

namespace DPCLibrary.Exceptions
{
    [Serializable]
    public class CriticalException : Exception
    {
        public CriticalException()
        {
        }

        public CriticalException(string message) : base(message)
        {
        }

        public CriticalException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected CriticalException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
