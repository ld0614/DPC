using DPCLibrary.Models;
using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace DPCDevClient
{
    //For up to date version install Windows SDK and look in C:\Program Files (x86)\Windows Kits\10\Include\<Version>\um\RtmV2.h
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct RTM_NET_ADDRESS
    {
        AddressFamily AddressFamily;
        ushort NumBits;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = RasConstants.RTM_MAX_ADDRESS_SIZE)]
        string AddrBits;
    }
}