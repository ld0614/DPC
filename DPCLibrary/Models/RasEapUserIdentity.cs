using System.Runtime.InteropServices;

namespace DPCLibrary.Models
{
    internal class RasEapUserIdentity
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = RasConstants.UNLEN + 1)]
        public string szUserName;
        public uint dwSize;
        public byte[] pbEapInfo;
    }
}
