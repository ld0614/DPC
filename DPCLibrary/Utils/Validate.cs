using System.IO;
using System.Text.RegularExpressions;
using System.Net;
using System.Net.Sockets;

namespace DPCLibrary.Utils
{
    public static class Validate
    {
        public static bool IPv4OrIPv6OrCIDR(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return false;
            }
            //If address is either ipv4 or ipv6 in either IP or CIDR format allow it
            if (IPv4OrCIDR(address) || IPv6OrCIDR(address))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool IPv6OrCIDR(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return false;
            }

            string[] SplitCIDR = address.Split('/');

            if (SplitCIDR.Length == 1)
            {
                return IPv6(address);
            }
            else
            {
                return IPv6CIDR(address);
            }
        }

        public static bool IPv4OrCIDR(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return false;
            }

            string[] SplitCIDR = address.Split('/');
            if (SplitCIDR.Length == 1)
            {
                return IPv4(address);
            }
            else
            {
                return IPv4CIDR(address);
            }
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
                if (!IPv4(ip.Trim()))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IPv6(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return false;
            }

            if (address == "::")
            {
                return false; //reject simple IPv6 Zero
            }

            //Find and reject other ways of making IPv6 Zero
            Match zeroResult = Regex.Match(address, "^s*(((0{1,4}:){7}(0{1,4}|:))|((0{1,4}:){6}|(:))|((0{1,4}:){5}(((:0]{1,4}){1,2})|:))|((0{1,4}:){4}(((:0{1,4}){1,3})|((:0{1,4})?:)|:))|((0{1,4}:){3}(((:0{1,4}){1,4})|((:0{1,4}){0,2}:)|:))|((0{1,4}:){2}(((:0{1,4}){1,5})|((:0{1,4}){0,3}:)|:))|((0{1,4}:){1}(((:0{1,4}){1,6})|((:0{1,4}){0,4}:)|:))|(:(((:0{1,4}){1,7})|((:0{1,4}){0,5}:)|:)))(%.+)?s*");
            if (zeroResult.Value == address)
            {
                return false;
            }

            //Check that the IPAddress class accepts the value
            if (!IPAddress.TryParse(address, out IPAddress potentialv6))
            {
                return false;
            }

            //Checks that the address is actually considered IPv6 and not IPv4 etc
            return potentialv6.AddressFamily == AddressFamily.InterNetworkV6;
        }

        public static bool IPv4(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return false;
            }

            if (address == "0.0.0.0")
            {
                return false; //reject all 0s as its only valid as a CIDR
            }

            Match result = Regex.Match(address, "\\b(25[0-5]|2[0-4]\\d|1\\d\\d|[1-9]?\\d)(\\.(25[0-5]|2[0-4]\\d|1\\d\\d|[1-9]?\\d)){3}\\b");
            return result.Value == address;
        }

        public static bool IPv6CIDR(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return false;
            }

            string[] SplitCIDR = address.Split('/');
            if (SplitCIDR.Length != 2)
            {
                return false;
            }

            if (!IPv6(SplitCIDR[0]))
            {
                //Front part isn't a valid address
                return false;
            }

            if (!int.TryParse(SplitCIDR[1], out int CIDRVal))
            {
                //Unable to turn second part into int
                return false;
            }

            if (CIDRVal > 128)
            {
                return false;
            }

            if (CIDRVal <= 0)
            {
                return false;
            }

            return true;
        }

        public static bool IPv4CIDR(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return false;
            }

            string[] SplitCIDR = address.Split('/');
            if (SplitCIDR.Length != 2)
            {
                return false;
            }

            if (!IPv4(SplitCIDR[0]))
            {
                //Front part isn't a valid address
                return false;
            }

            if (!int.TryParse(SplitCIDR[1], out int CIDRVal))
            {
                //Unable to turn second part into int
                return false;
            }

            if (CIDRVal > 32)
            {
                return false;
            }

            if (CIDRVal <= 0)
            {
                return false;
            }

            return true;
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
                Match result = Regex.Match(subdomain, "^(?![0-9]+$)(?!-)[ a-zA-Z0-9_-]{1,63}(?<!-)$");
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
                string[] portRange = element.Split(new char[]{ '-' }, 2);
                foreach(string portRangeElement in portRange)
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
            if(result.Value == id)
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