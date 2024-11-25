using System;

namespace DPCLibrary.Enums
{
    //For up to date version install Windows SDK and look in C:\Program Files (x86)\Windows Kits\10\Include\<Version>\um\ras.h
    //Details on the meaning of these flags https://learn.microsoft.com/en-us/windows/win32/api/ras/nf-ras-rasgeteapuseridentitya
    [Flags]
    public enum RasEapDialFlags : uint
    {
        RASEAPF_NonInteractive          = 0x00000002,
        RASEAPF_Logon                   = 0x00000004,
        RASEAPF_Preview                 = 0x00000008
    }
}
