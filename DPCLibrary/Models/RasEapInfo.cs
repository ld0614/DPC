﻿using System.Runtime.InteropServices;

namespace DPCLibrary.Models
{
    [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Auto)]
    public struct RasEapInfo
    {
        public int dwSize;
        public byte[] pbEapInfo;
    }
}