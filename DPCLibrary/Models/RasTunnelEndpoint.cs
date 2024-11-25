using DPCLibrary.Enums;
using System.Runtime.InteropServices;

namespace DPCLibrary.Models
{
    [StructLayout(LayoutKind.Explicit, Pack = 4, CharSet = CharSet.Auto)]
    public struct RasTunnelEndpoint
    {
        [FieldOffset(0)]
        public RasTunnelEndpointType dwType;
        [FieldOffset(1)]
        public IPv4Address ipv4;
        [FieldOffset(1)]
        public IPv6Address ipv6;
    }
}
