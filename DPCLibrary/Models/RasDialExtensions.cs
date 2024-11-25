using DPCLibrary.Enums;
using System;
using System.Runtime.InteropServices;

namespace DPCLibrary.Models
{
    [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Auto)]
    public class RasDialExtensions
    {
        public int dwSize;
        public RasDialOptions dwfOptions;
        public IntPtr hwndParent;
        public ulong reserved;
        public ulong reserved1;
        public RasEapInfo rasEapInfo;
        public bool skipPPPAuth;
        public RasDevSpecificInfo rasDevSpecificInfo;
    }
}
