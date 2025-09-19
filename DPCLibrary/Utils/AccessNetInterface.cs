using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;

namespace DPCLibrary.Utils
{
    public static class AccessNetInterface
    {
        public static IList<NetworkInterface> GetAllNetworkInterfaces()
        {
            IList<NetworkInterface> adapters = NetworkInterface.GetAllNetworkInterfaces().ToList();
            return adapters;
        }

        public static IList<NetworkInterface> GetLocalNetworkInterfaces()
        {
            IList<NetworkInterface> adapters = GetAllNetworkInterfaces().Where(ni => ni.NetworkInterfaceType != NetworkInterfaceType.Ppp).ToList();
            return adapters;
        }

        public static IList<NetworkInterface> GetVPNNetworkInterfaces()
        {
            IList<NetworkInterface> adapters = GetAllNetworkInterfaces().Where(ni => ni.NetworkInterfaceType == NetworkInterfaceType.Ppp).ToList();
            return adapters;
        }

        public static UnicastIPAddressInformation[] ValidIPs(NetworkInterface ni)
        {
            IPInterfaceProperties IPDetails = ni.GetIPProperties();
            UnicastIPAddressInformation[] validIPs = IPDetails.UnicastAddresses.Where(ip => ip.PrefixOrigin != PrefixOrigin.WellKnown || (ip.SuffixOrigin != SuffixOrigin.WellKnown && ip.SuffixOrigin != SuffixOrigin.LinkLayerAddress)).ToArray();
            return validIPs;
        }

        public static IPAddress[] ValidGateways(NetworkInterface ni)
        {
            IPInterfaceProperties IPDetails = ni.GetIPProperties();
            IPAddress[] validGateways = IPDetails.GatewayAddresses.Where(gw => !gw.Address.IsIPv6LinkLocal && !gw.Address.IsIPv6Multicast).Select(gw => gw.Address).ToArray();
            return validGateways;
        }

        public static bool InterfaceHasIPv4Gateway(NetworkInterface ni)
        {
            IPAddress[] validGateways = ValidGateways(ni);
            return validGateways.Where(gw => gw.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).Count() > 0;
        }

        public static bool InterfaceHasIPv6Gateway(NetworkInterface ni)
        {
            IPAddress[] validGateways = ValidGateways(ni);
            return validGateways.Where(gw => gw.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6).Count() > 0;
        }
    }
}
