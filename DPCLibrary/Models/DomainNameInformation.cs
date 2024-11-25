using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;

namespace DPCLibrary.Models
{
    public class DomainNameInformation : IEquatable<DomainNameInformation>
    {
        public string DomainName { get; set; }
        public string DnsServers { get; set; }
        public string WebProxyServers { get; set; }
        public bool AutoTrigger { get; set; }

        public DomainNameInformation(string domainName, string dnsServers)
        {
            if (string.IsNullOrWhiteSpace(domainName))
            {
                throw new ArgumentNullException(nameof(domainName));
            }

            DomainName = domainName;

            if (string.IsNullOrWhiteSpace(dnsServers))
            {
                //flatten to null to ensure that comparison checks can work correctly
                DnsServers = null;
            }
            else
            {
                DnsServers = dnsServers;
            }
            WebProxyServers = null;
            AutoTrigger = false;
        }

        public DomainNameInformation(XElement domainInfoNode)
        {
            if (domainInfoNode != null)
            {
                DomainName = domainInfoNode.XPathSelectElement("DomainName")?.Value;
                DnsServers = domainInfoNode.XPathSelectElement("DnsServers")?.Value;
                WebProxyServers = domainInfoNode.XPathSelectElement("WebProxyServers")?.Value;
                AutoTrigger = Convert.ToBoolean(domainInfoNode.XPathSelectElement("AutoTrigger")?.Value, CultureInfo.InvariantCulture);
            }
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as DomainNameInformation);
        }

        public bool Equals(DomainNameInformation other)
        {
            return other != null &&
                   DomainName == other.DomainName &&
                   DnsServers == other.DnsServers &&
                   WebProxyServers == other.WebProxyServers &&
                   AutoTrigger == other.AutoTrigger;
        }

        public override int GetHashCode()
        {
            int hashCode = -1963478847;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(DomainName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(DnsServers);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(WebProxyServers);
            hashCode = hashCode * -1521134295 + AutoTrigger.GetHashCode();
            return hashCode;
        }

        public override string ToString()
        {
            StringBuilder returnString = new StringBuilder();
            returnString.Append("DomainName: " + DomainName + " ");
            if (string.IsNullOrWhiteSpace(DnsServers))
            {
                returnString.Append("DNS Servers: <EMPTY> ");
            }
            else
            {
                returnString.Append("DNS Servers: " + DnsServers + " ");
            }

            if (string.IsNullOrWhiteSpace(WebProxyServers))
            {
                returnString.Append("WebProxyServers: <EMPTY> ");
            }
            else
            {
                returnString.Append("WebProxyServers: " + WebProxyServers + " ");
            }

            returnString.Append("AutoTrigger: " + AutoTrigger);

            return returnString.ToString();
        }

        public static bool operator ==(DomainNameInformation left, DomainNameInformation right)
        {
            return EqualityComparer<DomainNameInformation>.Default.Equals(left, right);
        }

        public static bool operator !=(DomainNameInformation left, DomainNameInformation right)
        {
            return !(left == right);
        }
    }
}
