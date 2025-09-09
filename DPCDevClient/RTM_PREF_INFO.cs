using System.Runtime.InteropServices;

namespace DPCDevClient

{
    //For up to date version install Windows SDK and look in C:\Program Files (x86)\Windows Kits\10\Include\<Version>\um\RtmV2.h
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct RTM_PREF_INFO
    {
        public ulong Metric;
        public ulong Preference;
    }
}