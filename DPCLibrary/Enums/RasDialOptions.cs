using System;

namespace DPCLibrary.Enums
{
    //For up to date version install Windows SDK and look in C:\Program Files (x86)\Windows Kits\10\Include\<Version>\um\ras.h
    [Flags]
    public enum RasDialOptions : uint
    {
        RDEOPT_UsePrefixSuffix           = 0x00000001,
        RDEOPT_PausedStates              = 0x00000002,
        RDEOPT_IgnoreModemSpeaker        = 0x00000004,
        RDEOPT_SetModemSpeaker           = 0x00000008,
        RDEOPT_IgnoreSoftwareCompression = 0x00000010,
        RDEOPT_SetSoftwareCompression    = 0x00000020,
        RDEOPT_DisableConnectedUI        = 0x00000040,
        RDEOPT_DisableReconnectUI        = 0x00000080,
        RDEOPT_DisableReconnect          = 0x00000100,
        RDEOPT_NoUser                    = 0x00000200,
        RDEOPT_PauseOnScript             = 0x00000400,
        RDEOPT_Router                    = 0x00000800,
        RDEOPT_CustomDial                = 0x00001000,
        RDEOPT_UseCustomScripting        = 0x00002000,
        RDEOPT_InvokeAutoTriggerCredentialUI    = 0x00004000,
        RDEOPT_EapInfoCryptInCapable            = 0x00008000
    }
}
