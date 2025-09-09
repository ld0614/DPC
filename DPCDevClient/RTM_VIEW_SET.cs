using System;

namespace DPCDevClient
{
    //For up to date version install Windows SDK and look in C:\Program Files (x86)\Windows Kits\10\Include\<Version>\um\RtmV2.h
    [Flags]
    public enum RTM_VIEW_SET : uint
    {
        RTM_VIEW_ID_UCAST = 0,
        RTM_VIEW_ID_MCAST = 1,
        RTM_VIEW_MASK_SIZE = 0x20,
        RTM_VIEW_MASK_NONE = 0x00000000,
        RTM_VIEW_MASK_ANY = 0x00000000,
        RTM_VIEW_MASK_UCAST = 0x00000001,
        RTM_VIEW_MASK_MCAST = 0x00000002,
        RTM_VIEW_MASK_ALL = 0xFFFFFFFF
    }
}