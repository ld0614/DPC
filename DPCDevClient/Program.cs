using DPCLibrary.Enums;
using DPCLibrary.Utils;
using DPCService.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace DPCDevClient
{
    internal class Program
    {
        static void Main(string[] args)
        {
            AddressChangedCallback(null, null);

            NetworkChange.NetworkAddressChanged += new NetworkAddressChangedEventHandler(AddressChangedCallback);

            IntPtr RoutingManager = RegisterWithRoutingManager();
            DeregisterFromRoutingManager(RoutingManager);

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
            IList<NetworkInterface> adapters = AccessNetInterface.GetLocalNetworkInterfaces();
            foreach (NetworkInterface n in adapters)
            {
                Console.WriteLine("Name: {0}", n.Name);
                Console.WriteLine("   Has IPv6 Gateway {0}", AccessNetInterface.InterfaceHasIPv6Gateway(n));
            }
        }

        
        /*
        
        */
    }
}
