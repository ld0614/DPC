using System;

namespace DPCDevClient
{
    [Flags]
    public enum ProtocolType : uint
    {
 PROTO_TYPE_UCAST         =   0,
 PROTO_TYPE_MCAST       =     1,
 PROTO_TYPE_MS0        =      2,
 PROTO_TYPE_MS1       =       3
    }
}