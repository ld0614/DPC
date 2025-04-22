using System;
using System.Runtime.Serialization;

namespace DPCLibrary.Exceptions
{
    [Serializable]
    public class NetShException : Exception
    {
        public NetShException()
        {
        }

        public NetShException(string message) : base(message)
        {
        }

        public NetShException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected NetShException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
