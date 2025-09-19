using DPCLibrary.Enums;
using DPCLibrary.Utils;
using DPCService.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;

namespace DPCDevClient
{
    internal class Program
    {
        static void Main(string[] args)
        {
            AddressChangedCallback(null, null);

            NetworkChange.NetworkAddressChanged += new NetworkAddressChangedEventHandler(AddressChangedCallback);

            string value;
            do
            {
                Console.WriteLine("Startup Complete, Type 'quit' to Shutdown...");
                value = Console.ReadLine();
            } while (value.ToUpperInvariant() != "QUIT");
        }

        static void AddressChangedCallback(object sender, EventArgs e)
        {
            Console.WriteLine("Network change detected");

            Console.WriteLine("All Gateway Settings");
            IList<NetworkInterface> allAdapters = AccessNetInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface n in allAdapters)
            {
                Console.WriteLine("Name: {0}", n.Name);
                Console.WriteLine("   Has IPv4 Gateway {0}", AccessNetInterface.InterfaceHasIPv4Gateway(n));
                Console.WriteLine("   Has IPv6 Gateway {0}", AccessNetInterface.InterfaceHasIPv6Gateway(n));
            }

            Console.WriteLine("Local Gateway Settings");
            IList<NetworkInterface> localAdapters = AccessNetInterface.GetLocalNetworkInterfaces();
            foreach (NetworkInterface n in localAdapters)
            {
                Console.WriteLine("Name: {0}", n.Name);
                Console.WriteLine("   Has IPv4 Gateway {0}", AccessNetInterface.InterfaceHasIPv4Gateway(n));
                Console.WriteLine("   Has IPv6 Gateway {0}", AccessNetInterface.InterfaceHasIPv6Gateway(n));
            }
        }
    }
}
