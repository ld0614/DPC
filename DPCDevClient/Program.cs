using DPCLibrary.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DPCDevClient
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string profileName = "AOVPN User Tunnel";
            var profiles = ManageRasphonePBK.ListHiddenProfiles(profileName);
            foreach (var profile in profiles)
            {
                Console.WriteLine($"Profile: {profile}");
            }
        }
    }
}
