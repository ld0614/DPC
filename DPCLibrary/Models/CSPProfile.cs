using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;

namespace DPCLibrary.Models
{
    public class CSPProfile : VPNProfile
    {
        public CSPProfile(string profile, string profileName)
        {
            if (!string.IsNullOrWhiteSpace(profile))
            {
                LoadProfileFromCSP(profile, profileName);
            }
            //else return empty profile
        }

        private void LoadProfileFromCSP(string profileXML, string profileName)
        {
            LoadError = ""; //Clear existing load errors
            try
            {
                XDocument doc = XDocument.Parse(profileXML);
                ProfileName = doc.Root.XPathSelectElement("/VPNProfile/ProfileName")?.Value;
                if (string.IsNullOrWhiteSpace(ProfileName))
                {
                    //Update to use the provided name if not specified in the profile directly
                    ProfileName = profileName;
                }
                if (string.IsNullOrWhiteSpace(ProfileName))
                {
                    LoadError += "Profile Name must not be Empty";
                    return;
                }
                RememberCredentials = Convert.ToBoolean(doc.Root.XPathSelectElement("/VPNProfile/RememberCredentials")?.Value, CultureInfo.InvariantCulture);
                AlwaysOn = Convert.ToBoolean(doc.Root.XPathSelectElement("/VPNProfile/AlwaysOn")?.Value, CultureInfo.InvariantCulture);
                RegisterDNS = Convert.ToBoolean(doc.Root.XPathSelectElement("/VPNProfile/RegisterDNS")?.Value, CultureInfo.InvariantCulture);
                DeviceTunnel = Convert.ToBoolean(doc.Root.XPathSelectElement("/VPNProfile/DeviceTunnel")?.Value, CultureInfo.InvariantCulture);

                DisableUIEditButton = Convert.ToBoolean(doc.Root.XPathSelectElement("/VPNProfile/DisableAdvancedOptionsEditButton")?.Value, CultureInfo.InvariantCulture);
                DisableUIDisconnectButton = Convert.ToBoolean(doc.Root.XPathSelectElement("/VPNProfile/DisableDisconnectButton")?.Value, CultureInfo.InvariantCulture);

                string autoProxyServer = doc.Root.XPathSelectElement("/VPNProfile/Proxy/AutoConfigUrl")?.Value;
                string manualProxyServer = doc.Root.XPathSelectElement("/VPNProfile/Proxy/Manual/Server")?.Value;
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

                DeviceComplianceEnabled = Convert.ToBoolean(doc.Root.XPathSelectElement("/VPNProfile/DeviceCompliance/Enabled")?.Value, CultureInfo.InvariantCulture);
                DeviceComplianceSSOEnabled = Convert.ToBoolean(doc.Root.XPathSelectElement("/VPNProfile/DeviceCompliance/Sso/Enabled")?.Value, CultureInfo.InvariantCulture);
                DeviceComplianceSSOEKU = doc.Root.XPathSelectElement("/VPNProfile/DeviceCompliance/Sso/Eku")?.Value;
                DeviceComplianceSSOIssuerHash = doc.Root.XPathSelectElement("/VPNProfile/DeviceCompliance/Sso/IssuerHash")?.Value;

                string routingPolicy = doc.Root.XPathSelectElement("/VPNProfile/NativeProfile/RoutingPolicyType")?.Value;
                if (routingPolicy != null)
                {
                    SplitTunnel = string.Equals(routingPolicy, "SplitTunnel", StringComparison.CurrentCultureIgnoreCase);
                }
                else
                {
                    SplitTunnel = true; //If the policy is not specified it is interpreted as split tunnel by Windows OS
                }

                NativeProtocolType = doc.Root.XPathSelectElement("/VPNProfile/NativeProfile/NativeProtocolType")?.Value;

                AuthenticationMethod = doc.Root.XPathSelectElement("/VPNProfile/NativeProfile/Authentication/UserMethod")?.Value;
                if (string.IsNullOrWhiteSpace(AuthenticationMethod))
                {
                    AuthenticationMethod = doc.Root.XPathSelectElement("/VPNProfile/NativeProfile/Authentication/MachineMethod")?.Value;
                }

                FormatAndSaveEapConfig(doc.Root.XPathSelectElement("/VPNProfile/NativeProfile/Authentication/Eap/Configuration"));
                List<string> DNSSuffixTemp = doc.Root.XPathSelectElement("/VPNProfile/DnsSuffix")?.Value?.Split(',')?.ToList();
                if (DNSSuffixTemp != null)
                {
                    DNSSuffix = DNSSuffixTemp;
                }

                List<XElement> domainInfoElements = doc.Root.XPathSelectElements("/VPNProfile/DomainNameInformation")?.ToList();
                foreach (XElement domainInfoElement in domainInfoElements)
                {
                    try
                    {
                        DomainNameInformation.Add(new DomainNameInformation(domainInfoElement));
                    }
                    catch (Exception ed)
                    {
                        LoadError += "Error Loading Domain Name Information: " + ed.Message + Environment.NewLine;
                        LoadError += ed.StackTrace + Environment.NewLine;
                    }
                }

                List<XElement> trafficFilterElements = doc.Root.XPathSelectElements("/VPNProfile/TrafficFilter")?.ToList();
                foreach (XElement trafficFilterElement in trafficFilterElements)
                {
                    try
                    {
                        TrafficFilterList.Add(new TrafficFilter(trafficFilterElement));
                    }
                    catch (Exception et)
                    {
                        LoadError += "Error Loading Traffic Filter Information: " + et.Message + Environment.NewLine;
                        LoadError += et.StackTrace + Environment.NewLine;
                    }
                }

                List<string> trustedNetworkDetectionTemp = doc.Root.XPathSelectElement("/VPNProfile/TrustedNetworkDetection")?.Value?.Split(',')?.ToList();
                if (trustedNetworkDetectionTemp != null)
                {
                    TrustedNetworkDetection = trustedNetworkDetectionTemp;
                }
                else if (DomainNameInformation.Count > 0)
                {
                    //If Trusted Network Detection is not explicitly defined by Domain Name Information is defined the Domain Name Include list is used by Windows as the Trusted Network Detection List
                    TrustedNetworkDetection = DomainNameInformation.Where(d => !string.IsNullOrWhiteSpace(d.DnsServers) && !string.IsNullOrWhiteSpace(d.DomainName)).Select(d => d.DomainName).ToList();
                }

                DisableClassBasedDefaultRoute = Convert.ToBoolean(doc.Root.XPathSelectElement("/VPNProfile/NativeProfile/DisableClassBasedDefaultRoute")?.Value, CultureInfo.InvariantCulture);

                List<string> serverListTemp = doc.Root.XPathSelectElement("/VPNProfile/NativeProfile/Servers")?.Value?.Split(',')?.Select(s => s.Split(';')[0].Trim()).ToList();
                if (serverListTemp != null)
                {
                    ServerList = serverListTemp;
                }

                CryptographySuite newSuite = new CryptographySuite(doc.Root.XPathSelectElement("/VPNProfile/NativeProfile/CryptographySuite"));
                if (newSuite != new CryptographySuite())
                {
                    CryptographySuite = newSuite;
                }

                List<XElement> routeListElements = doc.Root.XPathSelectElements("/VPNProfile/Route")?.ToList();
                foreach (XElement routeElement in routeListElements)
                {
                    try
                    {
                        RouteList.Add(new Route(routeElement));
                    }
                    catch (Exception er)
                    {
                        LoadError += "Error Loading Route: " + er.Message + Environment.NewLine;
                        LoadError += er.StackTrace + Environment.NewLine;
                    }
                }

                if (IsDefaultProfile(this))
                {
                    LoadError += "Profile load failed as no values were set";
                }
            }
            catch (Exception e)
            {
                LoadError += "Error Loading Profile: " + e.Message + Environment.NewLine;
                LoadError += e.StackTrace + Environment.NewLine;
            }
        }
    }
}
