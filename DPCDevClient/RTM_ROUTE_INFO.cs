using System;
using System.Runtime.InteropServices;

namespace DPCDevClient
{
    //For up to date version install Windows SDK and look in C:\Program Files (x86)\Windows Kits\10\Include\<Version>\um\RtmV2.h
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct RTM_ROUTE_INFO
    {
        IntPtr DestHandle;
        IntPtr RouteOwner;
        IntPtr Neighbour;
        uint State;
        uint Flags1;
        ushort Flags;
        RTM_PREF_INFO PrefInfo;
        RTM_VIEW_SET BelongsToViews;
        IntPtr EntitySpecificInfo;
        RTM_NEXTHOP_LIST NextHopsList;
    }
}