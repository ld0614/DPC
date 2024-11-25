using System;

namespace DPCLibrary.Enums
{
    [Flags]
    public enum ConnectionEvent
    {
        UNKNOWN = 0,
        RASCN_Connection = 0x00000001,
        RASCN_Disconnection = 0x00000002,
        RASCN_BandwidthAdded = 0x00000004,
        RASCN_BandwidthRemoved = 0x00000008,
        RASCN_Dormant = 0x00000010,
        RASCN_ReConnection = 0x00000020,
        RASCN_EPDGPacketArrival = 0x00000040
    }
}