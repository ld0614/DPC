using System.Runtime.InteropServices;

namespace DPCDevClient
{
    //For up to date version install Windows SDK and look in C:\Program Files (x86)\Windows Kits\10\Include\<Version>\um\RtmV2.h
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct RTM_REGN_PROFILE
    {
        public uint MaxNextHopsInRoute; // Max. number of equal cost nexthops in a route, & Max. number of local nexthops in any one remote nexthop

        public uint MaxHandlesInEnum;   // Max. handles returned in one call to RtmGetEnumDests, RtmGetChangedDests, RtmGetEnumRoutes,RtmGetRoutesInElist

        public RTM_VIEW_SET ViewsSupported;     // Views supported by this addr family

        public uint NumberOfViews;      // Number of views (# 1s in above mask)
    }
}