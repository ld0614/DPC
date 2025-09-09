using System;

namespace DPCDevClient
{
    //For up to date version install Windows SDK and look in C:\Program Files (x86)\Windows Kits\10\Include\<Version>\um\RtmV2.h
    [Flags]
    public enum RTM_ENUM_FLAGS : uint
    {
 RTM_ENUM_ALL_ROUTES  = 0x00000000,
 RTM_ENUM_OWN_ROUTES =  0x00010000
    }
}