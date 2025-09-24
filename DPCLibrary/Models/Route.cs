using DPCLibrary.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;

namespace DPCLibrary.Models
{
    public class Route : IEquatable<Route>
    {
        public IPAddress Address { get; set; }
        public int Prefix { get; set; }
        public uint Metric { get; set; } = 1;
        public bool ExclusionRoute { get; set; }

        public Route(string binary)
        {
            if (string.IsNullOrWhiteSpace(binary) || binary.Length % 8 != 0)
            {
                throw new Exception("binary was the wrong size");
            }

            IEnumerable<string> RouteValuesIE = binary.SplitByLength(8, true);
            string[] RouteValues = RouteValuesIE.ToArray();
            uint metric = INIParser.FormatPBKByteAsUInt(RouteValues[0]);
            uint IPType = INIParser.FormatPBKByteAsUInt(RouteValues[1]);
            uint mask = INIParser.FormatPBKByteAsUInt(RouteValues[2]);
            bool excludeRoute = INIParser.FormatPBKByteAsBool(RouteValues[7]);
            //9th Byte not currently known about, typically 0

            IPAddress address;

            switch (IPType)
            {
                case 2:
                    {
                        address = new IPv4Address();
                        address.LoadFromPBKHex(RouteValues[3]);
                        break;
                    }
                case 23:
                    {
                        address = new IPv6Address();
                        address.LoadFromPBKHex(RouteValues[3] + RouteValues[4] + RouteValues[5] + RouteValues[6]);
                        break;
                    }
                default: throw new Exception("Unknown IP Type: " + IPType.ToString(CultureInfo.InvariantCulture));
            }

            Address = address;
            Prefix = (int)mask;
            Metric = metric;
            ExclusionRoute = excludeRoute;
        }

        public Route(IPAddress address, int prefix, bool excludeRoute, uint metric)
        {
            Address = address;
            Prefix = prefix;
            ExclusionRoute = excludeRoute;
            Metric = metric;
        }

        public Route(XElement routeNode)
        {
            if (routeNode != null)
            {
                string tempAddress = routeNode.XPathSelectElement("Address")?.Value;
                if (tempAddress.Contains("/"))
                {
                    Prefix = IPUtils.GetIPCIDRSuffix(tempAddress);
                    tempAddress = IPUtils.GetIPAddress(tempAddress);
                }
                else
                {
                    Prefix = Convert.ToInt32(routeNode.XPathSelectElement("PrefixSize")?.Value, CultureInfo.InvariantCulture);
                }

                if (Validate.IPv4Address(tempAddress))
                {
                    Address = new IPv4Address();
                    Address.LoadFromString(tempAddress);
                    if (Prefix < 0 || Prefix > 32)
                    {
                        throw new InvalidDataException("IPv4 Prefix " + Prefix + " was not considered a valid CIDR Suffix");
                    }
                }
                else if (Validate.IPv6Address(tempAddress))
                {
                    Address = new IPv6Address();
                    Address.LoadFromString(tempAddress);
                    if (Prefix < 0 || Prefix > 128)
                    {
                        throw new InvalidDataException("IPv6 Prefix " + Prefix + " was not considered a valid CIDR Suffix");
                    }
                }
                else
                {
                    throw new InvalidDataException("IP Address " + tempAddress + " was not considered a valid IPv4 or IPv6 Address");
                }


                uint tempMetric = Convert.ToUInt32(routeNode.XPathSelectElement("Metric")?.Value, CultureInfo.InvariantCulture);
                if (tempMetric > 0)
                {
                    Metric = tempMetric;
                }
                ExclusionRoute = Convert.ToBoolean(routeNode.XPathSelectElement("ExclusionRoute")?.Value, CultureInfo.InvariantCulture);
            }
        }

        public bool IsDefault()
        {
            return Address == null &&
                Prefix == 0 &&
                Metric == 1 &&
                ExclusionRoute == false;
        }

        public override bool Equals(object obj) //Comments are not considered equal as its not possible to extract
        {
            return Equals(obj as Route);
        }

        public override int GetHashCode()
        {
            int hashCode = 953204822;
            hashCode = hashCode * -1521134295 + Address.GetHashCode();
            hashCode = hashCode * -1521134295 + Prefix.GetHashCode();
            hashCode = hashCode * -1521134295 + Metric.GetHashCode();
            hashCode = hashCode * -1521134295 + ExclusionRoute.GetHashCode();
            return hashCode;
        }

        bool IEquatable<Route>.Equals(Route other)
        {
            return other != null &&
                   Address.Equals(other.Address) &&
                   Prefix == other.Prefix &&
                   ExclusionRoute == other.ExclusionRoute &&
                   Metric == other.Metric;
        }

        public override string ToString()
        {
            StringBuilder returnString = new StringBuilder();
            returnString.Append("Address: " + Address + " ");
            returnString.Append("Prefix: " + Prefix.ToString(CultureInfo.InvariantCulture) + " ");
            returnString.Append("Metric: " + Metric.ToString(CultureInfo.InvariantCulture) + " ");
            returnString.Append("Exclusion Route: " + ExclusionRoute);

            return returnString.ToString();
        }

        public static bool operator ==(Route left, Route right)
        {
            return EqualityComparer<Route>.Default.Equals(left, right);
        }

        public static bool operator !=(Route left, Route right)
        {
            return !(left == right);
        }
    }
}
