using System;

namespace DPCLibrary.Enums
{
    [Flags]
    public enum PBKOptions : uint
    {
        None = 0x00000000,
        Unknown1 = 0x00000001, //It is not currently known what this enum flag currently sets
        Unknown2 = 0x00000002, //It is not currently known what this enum flag currently sets
        Unknown3 = 0x00000004, //It is not currently known what this enum flag currently sets
        DisableUIEdit = 0x00000008,
        DisableUIDisconnect = 0x00000010
    }
}
