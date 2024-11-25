using DPCLibrary.Enums;
using System.Runtime.InteropServices;

namespace DPCLibrary.Models
{
    [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Auto)]
    public struct RasConnStatus
    {
        public int dwSize;
        public RasConnState rasConnState;
        public RasError dwError;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = RasConstants.MaxDeviceType + 1)]
        public string szDeviceType;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = RasConstants.MaxDeviceName + 1)]
        public string szDeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = RasConstants.MaxPhoneNumber + 1)]
        public string szPhoneNumber;
        public RasTunnelEndpoint localEndPoint;
        public RasTunnelEndpoint remoteEndPoint;
        public RasConnSubState rasconnsubstate;
    }
}
