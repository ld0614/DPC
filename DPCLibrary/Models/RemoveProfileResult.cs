using DPCLibrary.Exceptions;
using System;

namespace DPCLibrary.Models
{
    public class RemoveProfileResult
    {
        public bool Status { get; set; }
        public Exception Error { get; set; }

        public RemoveProfileResult()
        {
            Status = true;
        }

        public RemoveProfileResult(bool status)
        {
            Status = status;
        }

        public RemoveProfileResult(string path, Exception e)
        {
            Status = false;
            Error = new FileDeleteException(path, e);
        }
    }
}
