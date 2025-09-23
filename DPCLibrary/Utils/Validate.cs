using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace DPCLibrary.Utils
{
    public static class Validate
    {
        /// <summary>
        /// Validates if the address is either a valid IPv4 or IPv6 endpoint address, or a valid IPv4 or IPv6 CIDR address
        /// </summary>
        /// <param name="address">The address to validate</param>
        public static bool IPv4OrIPv6OrCIDR(string address)
        {
            return ValidateIPInternal(address, AddressFamily.InterNetwork, true, false) ||
                   ValidateIPInternal(address, AddressFamily.InterNetworkV6, true, false);
        }

        /// <summary>
        /// Validates if the address is either a valid IPv6 endpoint address, or a valid IPv6 CIDR address
        /// </summary>
        /// <param name="address">The address to validate</param>
        public static bool IPv6OrCIDR(string address)
        {
            return ValidateIPInternal(address, AddressFamily.InterNetworkV6, true, false);
        }

        /// <summary>
        /// Validates if the address is either a valid IPv4 endpoint address, or a valid IPv4 CIDR address
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static bool IPv4OrCIDR(string address)
        {
            return ValidateIPInternal(address, AddressFamily.InterNetwork, true, false);
        }

        //Ensure that comments don't have --> which could really mess up the XML as it will cancel the comment and allow arbitrary XML injection
        public static bool Comment(string potentialComment)
        {
            if (string.IsNullOrWhiteSpace(potentialComment))
            {
                return true; //If empty allow as valid as it can't be used to inject XML
            }

            return !potentialComment.Contains("-->"); //Return false if the comment includes -->
        }

        public static bool IPAddressCommaList(string IPList)
        {
            //Accept null as valid exclusion
            if (string.IsNullOrWhiteSpace(IPList))
            {
                return true;
            }

            string[] ipSplitList = IPList.Split(',');

            //Invalidate the entire record if there is 1 invalid IP
            foreach (string ip in ipSplitList)
            {
                if (!IPv4EndpointAddress(ip.Trim()))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Validates if the address is a valid IPv6 endpoint address
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static bool IPv6EndpointAddress(string address)
        {
            return ValidateIPInternal(address, AddressFamily.InterNetworkV6, false, false);
        }

        /// <summary>
        /// Validates if the address is a valid IPv6 endpoint address or :: (unspecified address)
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static bool IPv6Address(string address)
        {
            return ValidateIPInternal(address, AddressFamily.InterNetworkV6, false, true);
        }

        /// <summary>
        /// Validates if the address is a valid IPv4 endpoint address
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static bool IPv4EndpointAddress(string address)
        {
            return ValidateIPInternal(address, AddressFamily.InterNetwork, false, false);
        }

        /// <summary>
        /// Validates if the address is a valid IPv4 endpoint address or 0.0.0.0
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static bool IPv4Address(string address)
        {
            return ValidateIPInternal(address, AddressFamily.InterNetwork, false, true);
        }

        /// <summary>
        /// Validates if the address is a valid IP address of the specified family, with options for CIDR and unspecified address
        /// </summary>
        /// <param name="address">The string representation of the address to validate</param>
        /// <param name="addressFamily">The expected address family (IPv4 or IPv6)</param>
        /// <param name="allowCidr">Specifies if CIDR notation is allowed</param>
        /// <param name="allowUnspecifiedAddress">Specifies if the unspecified address (0.0.0.0 or ::) is allowed</param>
        /// <returns></returns>
        private static bool ValidateIPInternal(string address, AddressFamily addressFamily, bool allowCidr, bool allowUnspecifiedAddress)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return false;
            }

            IPAddress anyAddress;

            if (addressFamily == AddressFamily.InterNetwork)
            {
                anyAddress = IPAddress.Any;
            }
            else if (addressFamily == AddressFamily.InterNetworkV6)
            {
                anyAddress = IPAddress.IPv6Any;
            }
            else
            {
                return false;
            }

            string[] addressParts = address.Split('/');
            bool isCidr = addressParts.Length == 2;
            int prefix = -1;

            if (isCidr)
            {
                if (!allowCidr)
                {
                    return false;
                }

                if (!int.TryParse(addressParts[1], out prefix))
                {
                    // Unable to turn second part into int
                    return false;
                }

                if (addressFamily == AddressFamily.InterNetwork && (prefix < 0 || prefix > 32))
                {
                    // Not a valid IPv4 CIDR prefix
                    return false;
                }

                if (addressFamily == AddressFamily.InterNetworkV6 && (prefix < 0 || prefix > 128))
                {
                    // Not a valid IPv6 CIDR prefix
                    return false;
                }
            }

            if (!IPAddress.TryParse(addressParts[0], out IPAddress ipAddress))
            {
                // Unable to turn first part into IP
                return false;
            }

            if (ipAddress.AddressFamily != addressFamily)
            {
                // Not the correct address family
                return false;
            }

            if (anyAddress.Equals(ipAddress) && !(allowUnspecifiedAddress || (isCidr && allowCidr)))
            {
                // The unspecified address is only valid if allowAnyAddress is true or if it's a CIDR and allowCidr is true
                return false;
            }

            if (prefix == 0 && !anyAddress.Equals(ipAddress))
            {
                // A CIDR of 0 is only valid if the IP is the unspecified address
                return false;
            }

            if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
            {
                // v4 specific validations

                if (address?.Count(c => c == '.') != 3)
                {
                    return false; // There's a few unit tests for `10.0.0` which are valid shorthand IPs, but adding this check to ensure there are 4 octets for consistency with previous behaviour
                }
            }

            return true;
        }

        /// <summary>
        /// Validates if the address is a valid IPv6 CIDR address
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static bool IPv6CIDR(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return false;
            }

            if (!address.Contains("/"))
            {
                return false;
            }

            return ValidateIPInternal(address, AddressFamily.InterNetworkV6, true, true);
        }

        /// <summary>
        /// Validates if the address is a valid IPv4 CIDR address
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static bool IPv4CIDR(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return false;
            }

            if (!address.Contains("/"))
            {
                return false;
            }

            return ValidateIPInternal(address, AddressFamily.InterNetwork, true, true);
        }

        public static string Thumbprint(string potentialThumbprint)
        {
            if (string.IsNullOrWhiteSpace(potentialThumbprint))
            {
                return null;
            }

            potentialThumbprint = Regex.Replace(potentialThumbprint, "[^\\w]", ""); //remove all chars what are not a-z, A-Z, 0-9 or _
            potentialThumbprint = potentialThumbprint.Replace("_", "");  //\w also includes underscore which is not acceptable
            if (potentialThumbprint.Length != 40)
            {
                return null;
            }

            Match result = Regex.Match(potentialThumbprint, "[0-9a-fA-F]+");
            if (!result.Success)
            {
                return null;
            }

            if (result.Value != potentialThumbprint)
            {
                return null;
            }

            return potentialThumbprint;
        }

        public static bool ValidateConnectionURL(string address)
        {
            return ValidateFQDN(address, false);
        }

        public static bool ValidateFQDN(string address)
        {
            return ValidateFQDN(address, true);
        }

        //acceptSubdomainWildcard means allow .domain.com which is used by certain AOVPN components such as the Domain Name Information List to specify wildcards
        public static bool ValidateFQDN(string address, bool acceptSubdomainWildcard)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return false;
            }

            if (IPv4OrCIDR(address))
            {
                return false;
            }

            if (!address.Contains("."))
            {
                return false;
            }

            if (address.Length > 253)
            {
                //Total Length of the domain must be less than 253 characters https://webmasters.stackexchange.com/questions/16996/maximum-domain-name-length
                return false;
            }

            if (address.StartsWith(".") && acceptSubdomainWildcard)
            {
                if (address == ".")
                {
                    return true;
                }

                address = address.TrimStart('.');
            }

            string[] subdomains = address.Split('.');
            foreach (string subdomain in subdomains)
            {
                if (string.IsNullOrWhiteSpace(subdomain))
                {
                    return false;
                }

                //Each part of the domain must be less than 63 characters https://webmasters.stackexchange.com/questions/16996/maximum-domain-name-length
                Match result = Regex.Match(subdomain, "^(?![0-9]+$)(?!-)[a-zA-Z0-9_-]{1,63}(?<!-)$");
                if (result.Value != subdomain)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool ValidateTrustedNetwork(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return false;
            }

            if (IPv4OrCIDR(address))
            {
                return false;
            }

            if (address.Length > 253)
            {
                //Total Length of the domain must be less than 253 characters https://webmasters.stackexchange.com/questions/16996/maximum-domain-name-length
                return false;
            }

            if (address.StartsWith("."))
            {
                if (address == ".")
                {
                    return true;
                }

                address = address.TrimStart('.');
            }

            string[] subdomains = address.Split('.');
            foreach (string subdomain in subdomains)
            {
                if (string.IsNullOrWhiteSpace(subdomain))
                {
                    return false;
                }

                //Each part of the domain must be less than 63 characters https://webmasters.stackexchange.com/questions/16996/maximum-domain-name-length
                Match result = Regex.Match(subdomain, "^(?![0-9]+$)(?!-)[ \\p{L}0-9_-]{1,63}(?<!-)$");
                if (result.Value != subdomain)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool ProfileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }
            return AccessRasApi.ValidateProfileName(name);
        }

        public static bool OID(string potentialOID)
        {
            if (string.IsNullOrWhiteSpace(potentialOID))
            {
                return false;
            }

            Match result = Regex.Match(potentialOID, "^([1-9][0-9]{0,3}|0)(\\.([1-9][0-9]*|0)){4,20}$");
            return result.Value == potentialOID;
        }

        public static bool PortList(string list)
        {
            if (string.IsNullOrWhiteSpace(list))
            {
                //A portlist of blank is ignored in the profile build
                return true;
            }

            string[] PortElements = list.Split(',');
            int portNumber;
            foreach (string PortElement in PortElements)
            {
                string element = PortElement.Trim();
                string[] portRange = element.Split(new char[] { '-' }, 2);
                foreach (string portRangeElement in portRange)
                {
                    string rangeElement = portRangeElement.Trim();
                    if (string.IsNullOrEmpty(rangeElement))
                    {
                        return false;
                    }
                    portNumber = 0;
                    if (!int.TryParse(rangeElement, out portNumber))
                    {
                        return false;
                    }
                    if (portNumber < 0 || portNumber > 65535)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public static bool IPv4List(string list)
        {
            if (string.IsNullOrWhiteSpace(list))
            {
                //A ipv4list of blank is ignored in the profile build
                return true;
            }

            string[] listElements = list.Split(',');

            foreach (string listElement in listElements)
            {
                if (!IPv4OrCIDR(listElement))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool PackageId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                //A packageId of blank is ignored in the profile build
                return true;
            }

            if (id.ToUpperInvariant().Trim() == "SYSTEM")
            {
                return true;
            }

            Match result = Regex.Match(id, "^[a-zA-Z0-9.-]+[_][a-zA-Z0-9]+");
            if (result.Value == id)
            {
                return true;
            }

            try
            {
                string fullPath = Path.GetFullPath(id); //Check that Full path can be resolved
                string root = Path.GetPathRoot(id);
                return string.IsNullOrEmpty(root.Trim(new char[] { '\\', '/' })) == false;
            }
            catch
            {
                return false;
            }
        }
    }
}
