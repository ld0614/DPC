using System;

namespace DPCDevClient
{
    //For up to date version install Windows SDK and look in C:\Program Files (x86)\Windows Kits\10\Include\<Version>\um\RtmV2.h
    [Flags]
    public enum RTM_MATCH_FLAGS : uint
    {
 RTM_MATCH_NONE       = 0x00000000,
 RTM_MATCH_OWNER      = 0x00000001,
 RTM_MATCH_NEIGHBOUR  = 0x00000002,
 RTM_MATCH_PREF      = 0x00000004,
 RTM_MATCH_NEXTHOP  =   0x00000008,
 RTM_MATCH_INTERFACE =  0x00000010,
 RTM_MATCH_FULL     =   0x0000FFFF
    }
}