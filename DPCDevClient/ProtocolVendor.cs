using System;

namespace DPCDevClient
{
    [Flags]
    public enum ProtocolVendor : uint
    {
 PROTO_VENDOR_MS0     =       0x0000,
 PROTO_VENDOR_MS1     =       0x137,   // 311
 PROTO_VENDOR_MS2     =       0x3FFF,
        PROTO_FROM_PROTO_ID =     0xFFFF
    }
}