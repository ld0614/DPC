using System;

namespace DPCLibrary.Enums
{
    [Flags]
    public enum InterfaceOffloadFlags : byte
    {
        None = 0x00000000,
        NlChecksumSupported = 0x00000001,
        NlOptionsSupported = 0x00000002,
        TlDatagramChecksumSupported = 0x00000004,
        TlStreamChecksumSupported = 0x00000008,
        TlStreamOptionsSupported = 0x00000010,
        FastPathCompatible = 0x00000020,
        TlLargeSendOffloadSupported = 0x00000040,
        TlGiantSendOffloadSupported = 0x00000080
    }
}
