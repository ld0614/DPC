using System;
using System.Runtime.Serialization;

namespace DPCLibrary.Exceptions
{
    [Serializable]
    public class FileDeleteException : Exception
    {
        public FileDeleteException(string message) : base(message)
        {
        }

        public FileDeleteException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected FileDeleteException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
