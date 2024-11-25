using System.Globalization;

namespace DPCLibrary.Utils
{
    static public class IPUtils
    {
        public static string GetIPAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                //Invalid Address
                return null;
            }

            string[] splitAddress = address.Split('/');
            if (splitAddress.Length == 1)
            {
                return address;
            }
            else if (splitAddress.Length == 2)
            {
                return splitAddress[0];
            }

            //If there is more than 2 or less than 1 there is an issue
            return null;
        }

        public static string GetIPWithCIDR(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                //Invalid address
                return null;
            }

            return GetIPAddress(address) + "/" + GetIPCIDRSuffix(address).ToString(CultureInfo.InvariantCulture);
        }

        public static int GetIPCIDRSuffix(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                //Invalid address
                return -1;
            }

            string[] splitAddress = address.Split('/');
            if (splitAddress.Length == 1 && Validate.IPv4(address))
            {
                //If there is no CIDR then assume it is a single IPv4 hence /32
                return 32;
            }
            if (splitAddress.Length == 1 && Validate.IPv6(address))
            {
                //If there is no CIDR then assume it is a single IPv6 hence /128
                return 128;
            }
            else if (splitAddress.Length == 2)
            {
                if (Validate.IPv4(splitAddress[1]))
                {
                    //Mask may be in the format 255.255.0.0
                    return GetCIDRFromNetMask(splitAddress[1]);
                }
                else
                {
                    //Assume mask is in the format /24
                    //Throw exception if not valid
                    return int.Parse(splitAddress[1], CultureInfo.InvariantCulture);
                }
            }

            //If there is more than 2 or less than 1 there is an issue
            return -1;
        }

        //Based on https://stackoverflow.com/questions/36954345/get-cidr-from-netmask
        private static int GetCIDRFromNetMask(string netmask)
        {
            //Parse netmask into system IP address class to easily get IP address bytes out
            byte[] netmaskBytes = System.Net.IPAddress.Parse(netmask).GetAddressBytes();

            int cidrnet = 0; //CIDR Count
            bool zeroed = false;
            for (int i = 0; i < netmaskBytes.Length; i++)
            {
                for (int v = netmaskBytes[i]; (v & 0xFF) != 0; v <<= 1)
                {
                    if (zeroed)
                        // invalid netmask
                        return ~cidrnet;

                    if ((v & 0x80) == 0)
                        zeroed = true;
                    else
                        cidrnet++;
                }
            }
            return cidrnet;
        }
    }
}
