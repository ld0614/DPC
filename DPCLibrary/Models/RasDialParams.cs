using System;
using System.Runtime.InteropServices;

namespace DPCLibrary.Models
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct RasDialParams
    {
        public int dwSize;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = RasConstants.MaxEntryName + 1)]
        public string szEntryName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = RasConstants.MaxPhoneNumber + 1)]
        public string szPhoneNumber;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = RasConstants.MaxCallbackNumber + 1)]
        public string szCallbackNumber;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = RasConstants.UNLEN + 1)]
        public string szUserName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = RasConstants.PWLEN + 1)]
        public string szPassword;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = RasConstants.DNLEN + 1)]
        public string szDomain;
        public uint dwSubEntry;
        public IntPtr dwCallbackId;
        public uint dwIfIndex;
    }
}
