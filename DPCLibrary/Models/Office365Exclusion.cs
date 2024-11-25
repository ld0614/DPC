using DPCLibrary.Enums;

namespace DPCLibrary.Models
{
    public class Office365Exclusion
    {
        public int Id { get; set; }
        public string ServiceArea { get; set; }
        public string ServiceAreaDisplayName { get; set; }
        public string[] Urls { get; set; }
        public string[] Ips { get; set; }
        public string TcpPorts { get; set; }
        public string UdpPorts { get; set; }
        public bool ExpressRoute { get; set; }
        public Office365EndpointCategory Category { get; set; }
        public bool Required { get; set; }
    }
}