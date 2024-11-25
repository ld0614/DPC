using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace DPCLibrary.Models
{
    public class WMIProfile : VPNProfile
    {
        public WMIProfile(ProfileInfo profile, CancellationToken cancelToken)
        {
            LoadProfileFromVpnConfiguration(profile, cancelToken);
        }

        private void LoadProfileFromVpnConfiguration(ProfileInfo profile, CancellationToken cancelToken)
        {
            if (profile == null)
            {
                LoadError += "ProfileInfo is Empty";
                //if there is no profile return a blank WMIProfile Object
                return;
            }

            RASPhonePBKProfile pbkProfile = new RASPhonePBKProfile(profile);

            LoadError = ""; //Clear existing load errors
            try
            {
                string profileXML = profile.GetVpnProfileFromWMI(cancelToken);

                XDocument doc = XDocument.Parse(profileXML);
                ProfileName = doc.Root.XPathSelectElement("/VpnProfile/VpnConfiguration/Name")?.Value;
                AlwaysOn = Convert.ToBoolean(doc.Root.XPathSelectElement("/VpnProfile/VpnConfiguration/AutoTriggerEnabled")?.Value); //Device Tunnel may not show this correctly so also load from pbk where available which is more accurate
                RememberCredentials = Convert.ToBoolean(doc.Root.XPathSelectElement("/VpnProfile/VpnConfiguration/RememberCredential")?.Value, CultureInfo.InvariantCulture);
                SplitTunnel = Convert.ToBoolean(doc.Root.XPathSelectElement("/VpnProfile/VpnConfiguration/SplitTunneling")?.Value, CultureInfo.InvariantCulture);
                AuthenticationMethod = doc.Root.XPathSelectElement("/VpnProfile/VpnConfiguration/AuthenticationMethod/Method")?.Value;

                DisableUIEditButton = Convert.ToBoolean(doc.Root.XPathSelectElement("/VpnProfile/VpnConfiguration/DisableAdvancedOptionsEditButton")?.Value, CultureInfo.InvariantCulture);
                DisableUIDisconnectButton = Convert.ToBoolean(doc.Root.XPathSelectElement("/VpnProfile/VpnConfiguration/DisableDisconnectButton")?.Value, CultureInfo.InvariantCulture);

                //Check if the authentication is certificate as WMI uses MachineCertificate and CSP uses Certificate to mean the same thing
                if (!string.IsNullOrWhiteSpace(AuthenticationMethod) && AuthenticationMethod.ToLowerInvariant().Contains("certificate"))
                {
                    //Match WMI Output
                    AuthenticationMethod = "Certificate";
                }

                string autoProxyServer = doc.Root.XPathSelectElement("/VpnProfile/VpnConfiguration/VpnProxy/AutoConfigurationScript")?.Value;
                string manualProxyServer = doc.Root.XPathSelectElement("/VpnProfile/VpnConfiguration/VpnProxy/ProxyServer")?.Value;
                //It is theoretically possible to set both the Manual Proxy server and PAC in the same tunnel, if this happens the proxy will ignore the PAC file
                if (!string.IsNullOrEmpty(manualProxyServer))
                {
                    ProxyServer = manualProxyServer;
                    ProxyType = Enums.ProxyType.Manual;
                }
                else if (!string.IsNullOrEmpty(autoProxyServer))
                {
                    ProxyServer = autoProxyServer;
                    ProxyType = Enums.ProxyType.PAC;
                }
                else
                {
                    ProxyType = Enums.ProxyType.None;
                }

                NormaliseProxyServer();

                NativeProtocolType = doc.Root.XPathSelectElement("/VpnProfile/VpnConfiguration/TunnelType")?.Value;
                L2tpPsk = doc.Root.XPathSelectElement("/VpnProfile/VpnConfiguration/L2tpPsk")?.Value;
                FormatAndSaveEapConfig(doc.Root.XPathSelectElement("/VpnProfile/EapConfiguration"));
                List<string> DNSSuffixTemp = doc.Root.XPathSelectElements("/VpnProfile/VpnConfiguration/Trigger/DnsSuffixSearchList/DnsSuffix")?.Select(e => e.Value).ToList();
                if (DNSSuffixTemp != null)
                {
                    DNSSuffix = DNSSuffixTemp;
                }

                List<string> trustedNetworkDetectionTemp = doc.Root.XPathSelectElements("/VpnProfile/VpnConfiguration/Trigger/TrustedNetworkList/TrustedNetwork")?.Select(e => e.Value).ToList();
                if (trustedNetworkDetectionTemp != null)
                {
                    TrustedNetworkDetection = trustedNetworkDetectionTemp;
                }

                List<string> serverListTemp = doc.Root.XPathSelectElements("/VpnProfile/VpnConfiguration/VpnServerList/ServerName")?.Select(e => e.Value).ToList();
                if (serverListTemp != null && serverListTemp.Count > 0)
                {
                    ServerList = serverListTemp;
                }

                CryptographySuite newSuite = new CryptographySuite(doc.Root.XPathSelectElement("/VpnProfile/VpnConfiguration/CryptographySuite"));
                if (newSuite != new CryptographySuite())
                {
                    CryptographySuite = newSuite;
                }

                XElement domainNameListElements = doc.Root.XPathSelectElement("/VpnProfile/VpnConfiguration/Trigger/NrptRuleList");
                XNode domainNameNode = domainNameListElements.FirstNode;
                string dnsSuffix = null;
                List<string> dnsServers = new List<string>();
                while (domainNameNode != null)
                {
                    if (domainNameNode.NodeType == XmlNodeType.Element)
                    {
                        XElement xElement = domainNameNode as XElement;
                        switch (xElement.Name.ToString())
                        {
                            case "DnsSuffix":
                                {
                                    if (!string.IsNullOrEmpty(dnsSuffix))
                                    {
                                        DomainNameInformation.Add(new DomainNameInformation(dnsSuffix, string.Join(",", dnsServers)));
                                        dnsServers.Clear();
                                    }
                                    dnsSuffix = xElement.Value;
                                    break;
                                }

                            case "DnsServer": dnsServers.Add(xElement.Value); break;
                            default: throw new InvalidDataException("Unexpected element " + xElement.Name.ToString() + " Found in DomainNameInformation");
                        }
                    }
                    domainNameNode = domainNameNode.NextNode;
                }

                //Add final settings to Domain Name Info List
                if (!string.IsNullOrEmpty(dnsSuffix))
                {
                    DomainNameInformation.Add(new DomainNameInformation(dnsSuffix, string.Join(",", dnsServers)));
                    dnsServers = null;
                }

                IList<Route> pbkRouteList = pbkProfile.GetRouteList();
                if (pbkRouteList != null)
                {
                    RouteList = pbkRouteList;
                }

                RegisterDNS = pbkProfile.GetRegisterDNSStatus();
                DisableClassBasedDefaultRoute = pbkProfile.GetDisableClassBasedDefaultRouteStatus();
                AlwaysOn = pbkProfile.GetAlwaysOnCapable();
                DeviceComplianceEnabled = pbkProfile.GetDeviceComplianceEnabled();
                DeviceComplianceSSOEnabled = pbkProfile.GetDeviceComplianceSSOEnabled();
                DeviceComplianceSSOEKU = pbkProfile.GetDeviceComplianceEKU();
                DeviceComplianceSSOIssuerHash = pbkProfile.GetDeviceComplianceIssuerHash();
            }
            catch (Exception e)
            {
                LoadError += "Error Loading Profile: " + e.Message + Environment.NewLine;
                LoadError += e.StackTrace + Environment.NewLine;
            }

            try
            {
                bool? pbkDeviceTunnel = pbkProfile.GetDeviceTunnelStatus();
                if (pbkDeviceTunnel != null)
                {
                    DeviceTunnel = (bool)pbkDeviceTunnel;
                }
                else
                {
                    LoadError += "Error Device Tunnel Status Missing" + Environment.NewLine;
                }
            }
            catch (Exception e)
            {
                LoadError += "Error Loading Device Tunnel Status: " + e.Message + Environment.NewLine;
            }

            try
            {
                IList<TrafficFilter> pbkTrafficFilterList = pbkProfile.GetTrafficFilterList();
                if (pbkTrafficFilterList != null)
                {
                    TrafficFilterList = pbkTrafficFilterList;
                }
            }
            catch (Exception e)
            {
                LoadError += "Error Loading Traffic Filter Details: " + e.Message + Environment.NewLine;
            }
        }
    }
}
