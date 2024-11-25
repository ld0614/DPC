using DPCService.Models;
using System;
using System.Runtime.InteropServices;

namespace DPCService.Utils
{
    internal static class NativeMethods
    {
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetServiceStatus(IntPtr handle, ref ServiceStatus serviceStatus);
    }
}
