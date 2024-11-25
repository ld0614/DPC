using DPCLibrary.Enums;
using DPCLibrary.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;

namespace DPCLibrary.Models
{
    public class TrafficFilter : IEquatable<TrafficFilter>
    {
        private string remoteAddresses;
        private string localAddresses;
        private readonly List<string> remotePorts = new List<string>();
        private readonly List<string> localPorts = new List<string>();

        public string RuleName { get; set; }
        public string AppId { get; set; }
        public Protocol Protocol { get; set; } = Protocol.ANY;
        public string LocalPorts
        {
            get
            {
                localPorts.Sort(); //CSP and WMI versions must return in the same order to enable string comparison checking
                return string.Join(",", localPorts);
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    //Clear Array
                    localPorts.Clear();
                }
                else
                {
                    //Split comma separated strings into list items
                    localPorts.AddRange(value.Replace(" ", "").Split(','));
                }
            }
        } //Traffic Filters can't handle spaces between elements
        public string RemotePorts
        {
            get
            {
                remotePorts.Sort(); //CSP and WMI versions must return in the same order to enable string comparison checking
                return string.Join(",", remotePorts);
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    //Clear Array
                    remotePorts.Clear();
                }
                else
                {
                    //Split comma separated strings into list items
                    remotePorts.AddRange(value.Replace(" ", "").Split(','));
                }
            }
        } //Traffic Filters can't handle spaces between elements
        public string LocalAddresses
        {
            get => localAddresses; set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    localAddresses = value;
                }
                else
                {
                    string[] addressList = value.Replace(" ", "").Split(',');
                    string tempList = "";
                    foreach (string address in addressList)
                    {
                        tempList += IPUtils.GetIPWithCIDR(address);
                        tempList += ",";
                    }
                    localAddresses = tempList.TrimEnd(',');

                }
            }
        } //Traffic Filters can't handle spaces between elements and needs /32 adding to single IPs
        public string RemoteAddresses
        {
            get => remoteAddresses; set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    remoteAddresses = value;
                }
                else
                {
                    string[] addressList = value.Replace(" ", "").Split(',');
                    string tempList = "";
                    foreach (string address in addressList)
                    {
                        tempList += IPUtils.GetIPWithCIDR(address);
                        tempList += ",";
                    }
                    remoteAddresses = tempList.TrimEnd(',');
                }
            }
        } //Traffic Filters can't handle spaces between elements and needs /32 adding to single IPs
        public TunnelType RoutingPolicyType { get; set; } = TunnelType.SplitTunnel;
        public ProtocolDirection Direction { get; set; } = ProtocolDirection.Outbound;
        public bool Invalid { get; set; } = false;

        public TrafficFilter(string ruleName)
        {
            RuleName = ruleName;
        }

        //When saved to the PBK file, Traffic Filter data is stored in the Firewall Rule format defined https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-gpfas/2efe0b76-7b4a-41ff-9050-1023f8196d16
        public TrafficFilter(Dictionary<string, string> ruleData)
        {
            //Ignore Action attribute as DPC assumes all rules are Allow
            //Ignore Active attribute as DPC assumes all rules are active
            if(ruleData.ContainsKey("Name")) RuleName = ruleData["Name"];

            if (ruleData.ContainsKey("App")) AppId = ruleData["App"];

            if (ruleData.ContainsKey("Protocol"))
            {
                Protocol = (Protocol)Enum.ToObject(typeof(Protocol), Convert.ToInt16(ruleData["Protocol"], CultureInfo.InvariantCulture));
            }
            else
            {
                Protocol = Protocol.ANY;
            }

            if (ruleData.ContainsKey("LPort")) LocalPorts = ruleData["LPort"];
            if (ruleData.ContainsKey("LPort2_10"))
            {
                localPorts.AddRange(ruleData["LPort2_10"].Replace(" ", "").Split(',')); //Add Port Range (1000-2000) to existing Port List
            }
            if (ruleData.ContainsKey("RPort")) RemotePorts = ruleData["RPort"];
            if (ruleData.ContainsKey("RPort2_10"))
            {
                remotePorts.AddRange(ruleData["RPort2_10"].Replace(" ", "").Split(',')); //Add Port Range (1000-2000) to existing Port List
            }
            if (ruleData.ContainsKey("LA4")) LocalAddresses = ruleData["LA4"];
            if (ruleData.ContainsKey("RA4")) RemoteAddresses = ruleData["RA4"];
            //IPv6 not currently supported for Traffic Filters https://directaccess.richardhicks.com/2021/07/19/always-on-vpn-traffic-filters-and-ipv6/

            if (ruleData.ContainsKey("BtoIf"))
            {
                if (Convert.ToBoolean(ruleData["BtoIf"], CultureInfo.InvariantCulture))
                {
                    RoutingPolicyType = TunnelType.ForceTunnel;
                }
            }

            if (ruleData.ContainsKey("Dir"))
            {
                switch (ruleData["Dir"])
                {
                    case "In": Direction= ProtocolDirection.Inbound; break;
                    case "Out": Direction= ProtocolDirection.Outbound; break;
                    default: throw new InvalidDataException("Unknown Traffic Filter Direction: " + ruleData["Dir"]);
                }
            }
        }

        public bool IsDefault()
        {
            return string.IsNullOrWhiteSpace(AppId) &&
                string.IsNullOrWhiteSpace(LocalPorts) &&
                string.IsNullOrWhiteSpace(RemotePorts) &&
                string.IsNullOrWhiteSpace(LocalAddresses) &&
                string.IsNullOrWhiteSpace(RemoteAddresses) &&
                Protocol == Protocol.ANY &&
                RoutingPolicyType == TunnelType.SplitTunnel &&
                Direction == ProtocolDirection.Outbound;
        }

        public TrafficFilter(XElement trafficFilterNode)
        {
            if (trafficFilterNode != null)
            {
                RuleName = Guid.NewGuid().ToString(); //Give unique name as the actual name is lost in the XML comments/completely removed
                AppId = trafficFilterNode.XPathSelectElement("App/Id")?.Value;
                Protocol = (Protocol)Enum.ToObject(typeof(Protocol), Convert.ToInt32(trafficFilterNode.XPathSelectElement("Protocol")?.Value, CultureInfo.InvariantCulture));
                LocalPorts = trafficFilterNode.XPathSelectElement("LocalPortRanges")?.Value;
                RemotePorts = trafficFilterNode.XPathSelectElement("RemotePortRanges")?.Value;
                LocalAddresses = trafficFilterNode.XPathSelectElement("LocalAddressRanges")?.Value;
                RemoteAddresses = trafficFilterNode.XPathSelectElement("RemoteAddressRanges")?.Value;
                string routeType = trafficFilterNode.XPathSelectElement("RoutingPolicyType")?.Value;
                if (!string.IsNullOrWhiteSpace(routeType))
                {
                    RoutingPolicyType = (TunnelType)Enum.Parse(typeof(TunnelType), routeType);
                }
                string direction = trafficFilterNode.XPathSelectElement("Direction")?.Value;
                if (!string.IsNullOrWhiteSpace(direction))
                {
                    Direction = (ProtocolDirection)Enum.Parse(typeof(ProtocolDirection), direction, true);
                }
            }
        }

        public override bool Equals(object obj) //Comments are not considered equal as its not possible to extract
        {
            return Equals(obj as Route);
        }

        public override int GetHashCode()
        {
            int hashCode = -1336070151;
            //hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(RuleName); //Don't compare rule names as we don't care if the name is the same or not
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(AppId);
            hashCode = hashCode * -1521134295 + Protocol.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(LocalPorts);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(RemotePorts);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(LocalAddresses);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(RemoteAddresses);
            hashCode = hashCode * -1521134295 + RoutingPolicyType.GetHashCode();
            hashCode = hashCode * -1521134295 + Direction.GetHashCode();
            return hashCode;
        }

        bool IEquatable<TrafficFilter>.Equals(TrafficFilter other)
        {
            return other is TrafficFilter filter &&
                   //RuleName == filter.RuleName && //Don't compare rule names as we don't care if the name is the same or not
                   AppId == filter.AppId &&
                   Protocol == filter.Protocol &&
                   LocalPorts == filter.LocalPorts && //Compare underlying array directly to avoid sequence issues
                   RemotePorts == filter.RemotePorts && //Compare underlying array directly to avoid sequence issues
                   LocalAddresses == filter.LocalAddresses &&
                   RemoteAddresses == filter.RemoteAddresses &&
                   RoutingPolicyType == filter.RoutingPolicyType &&
                   Direction == filter.Direction;
        }

        public static bool operator ==(TrafficFilter left, TrafficFilter right)
        {
            return EqualityComparer<TrafficFilter>.Default.Equals(left, right);
        }

        public static bool operator !=(TrafficFilter left, TrafficFilter right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            StringBuilder returnString = new StringBuilder();
            if (string.IsNullOrWhiteSpace(RuleName))
            {
                returnString.Append("Rule Name: <EMPTY> ");
            }
            else
            {
                returnString.Append("Rule Name: " + RuleName + " ");
            }

            if (string.IsNullOrWhiteSpace(AppId))
            {
                returnString.Append("App ID: <EMPTY> ");
            }
            else
            {
                returnString.Append("App ID: " + AppId + " ");
            }

            returnString.Append("Protocol: " + Protocol + " ");

            if (string.IsNullOrWhiteSpace(LocalPorts))
            {
                returnString.Append("Local Ports: <EMPTY> ");
            }
            else
            {
                returnString.Append("Local Ports: " + LocalPorts + " ");
            }

            if (string.IsNullOrWhiteSpace(RemotePorts))
            {
                returnString.Append("Remote Ports: <EMPTY> ");
            }
            else
            {
                returnString.Append("Remote Ports: " + RemotePorts + " ");
            }

            if (string.IsNullOrWhiteSpace(LocalAddresses))
            {
                returnString.Append("Local Addresses: <EMPTY> ");
            }
            else
            {
                returnString.Append("Local Addresses: " + LocalAddresses + " ");
            }

            if (string.IsNullOrWhiteSpace(RemoteAddresses))
            {
                returnString.Append("Remote Addresses: <EMPTY> ");
            }
            else
            {
                returnString.Append("Remote Addresses: " + RemoteAddresses + " ");
            }

            returnString.Append("Routing Policy Type: " + RoutingPolicyType + " ");
            returnString.Append("Direction: " + Direction + " ");

            return returnString.ToString();
        }
    }
}
