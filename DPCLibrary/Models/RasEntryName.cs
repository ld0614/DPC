using System.Runtime.InteropServices;

namespace DPCLibrary.Models
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    struct RasEntryName
    {
        public int dwSize;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = RasConstants.MaxEntryName + 1)]
        public string szEntryName;
        public int dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = RasConstants.MAX_PATH + 1)]
        public string szPhonebook;
    }
}
