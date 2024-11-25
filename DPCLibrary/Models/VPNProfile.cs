using DPCLibrary.Enums;
using DPCLibrary.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;
using System.Xml.XPath;

namespace DPCLibrary.Models
{
    public class VPNProfile : IEquatable<VPNProfile>
    {
        public string ProfileName { get; set; }
        public bool RememberCredentials { get; set; }
        public bool AlwaysOn { get; set; }
        public IList<string> DNSSuffix = new List<string>();
        public IList<string> TrustedNetworkDetection = new List<string>();
        public bool LockDown { get; set; }
        public bool DeviceTunnel { get; set; }
        public bool RegisterDNS { get; set; }
        public bool ByPassForLocal { get; set; }
        public ProxyType ProxyType { get; set; }
        public string ProxyServer { get; set; }
        public IList<DomainNameInformation> DomainNameInformation = new List<DomainNameInformation>();
        public IList<TrafficFilter> TrafficFilterList = new List<TrafficFilter>();
        //Native Profile
        public IList<string> ServerList = new List<string>();
        public bool SplitTunnel { get; set; }
        public string NativeProtocolType { get; set; }
        public string L2tpPsk { get; set; }
        public bool DisableClassBasedDefaultRoute { get; set; }
        public CryptographySuite CryptographySuite = new CryptographySuite();
        public string AuthenticationMethod { get; set; }
        public string EapConfig { get; set; }
        public bool FastReconnect { get; set; }
        public IList<Route> RouteList = new List<Route>(); //Covers both Include and Exclude Routes
        public string LoadError { get; set; }

        //public string EdpModeId { get; set; }
        //public bool PlumbIKEv2TSAsRoutes{ get; set; }
        //public string APNBinding { get; set; }
        public bool DeviceComplianceEnabled { get; set; }
        public bool DeviceComplianceSSOEnabled { get; set; }
        public string DeviceComplianceSSOEKU { get; set; }
        public string DeviceComplianceSSOIssuerHash { get; set; }
        //public string PluginProfile { get; set; }
        //public string AppTrigger { get; set; }
        //public bool RequireVpnClientAppUI { get; set; }
        public bool DisableUIDisconnectButton { get; set; }
        public bool DisableUIEditButton { get; set; }

        public VPNProfile()
        {
            SplitTunnel = true;
        }

        public static bool IsDefaultProfile(VPNProfile profile)
        {
            VPNProfile defaultProfile = new VPNProfile
            {
                ProfileName = profile.ProfileName
            };
            return profile == defaultProfile;
        }

        public static bool CompareProfiles(VPNProfile profile1, VPNProfile profile2)
        {
            //Do full check
            return profile1 == profile2;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as VPNProfile);
        }

        //Don't check NativeProtocolType as this is now controlled via VPNStrategy which is outside of the tunnel
        public bool Equals(VPNProfile other)
        {
            return other != null &&
                   //EdpModeId.EqualsString(other.EdpModeId) &&
                   RememberCredentials == other.RememberCredentials &&
                   AlwaysOn == other.AlwaysOn &&
                   DNSSuffix.EqualsArray(other.DNSSuffix) &&
                   TrustedNetworkDetection.EqualsArray(other.TrustedNetworkDetection) &&
                   LockDown == other.LockDown &&
                   DeviceTunnel == other.DeviceTunnel &&
                   RegisterDNS == other.RegisterDNS &&
                   ByPassForLocal == other.ByPassForLocal &&
                   ServerList.EqualsArray(other.ServerList) &&
                   SplitTunnel == other.SplitTunnel &&
                   L2tpPsk.EqualsString(other.L2tpPsk) &&
                   ProxyType == other.ProxyType &&
                   ProxyServer.EqualsString(other.ProxyServer) &&
                   DisableClassBasedDefaultRoute == other.DisableClassBasedDefaultRoute &&
                   CryptographySuite == other.CryptographySuite &&
                   AuthenticationMethod.EqualsString(other.AuthenticationMethod) &&
                   EapConfig.EqualsString(other.EapConfig) &&
                   DomainNameInformation.EqualsArray(other.DomainNameInformation) &&
                   RouteList.EqualsArray(other.RouteList) &&
                   FastReconnect == other.FastReconnect &&
                   //APNBinding == other.APNBinding &&
                   DeviceComplianceEnabled == other.DeviceComplianceEnabled &&
                   DeviceComplianceSSOEnabled == other.DeviceComplianceSSOEnabled &&
                   DeviceComplianceSSOEKU == other.DeviceComplianceSSOEKU &&
                   DeviceComplianceSSOIssuerHash == other.DeviceComplianceSSOIssuerHash &&
                   //PluginProfile == other.PluginProfile &&
                   //AppTrigger == other.AppTrigger &&
                   TrafficFilterList.EqualsArray(other.TrafficFilterList) &&
                   ProfileName == other.ProfileName &&
                   //RequireVpnClientAppUI == other.RequireVpnClientAppUI &&
                   //PlumbIKEv2TSAsRoutes == other.PlumbIKEv2TSAsRoutes;
                   DisableUIDisconnectButton == other.DisableUIDisconnectButton &&
                   DisableUIEditButton == other.DisableUIEditButton;
        }

        //Don't check NativeProtocolType as this is now controlled via VPNStrategy which is outside of the tunnel
        public Dictionary<string, string> EqualsResults(VPNProfile other)
        {
            Dictionary<string, string> results = new Dictionary<string, string>();
            if (other == null)
            {
                results.Add("Other Profile", "Is Null");
            }

            if (RememberCredentials != other.RememberCredentials)
            {
                results.Add("RememberCredentials", RememberCredentials + " does not match " + other.RememberCredentials);
            }

            if (AlwaysOn != other.AlwaysOn)
            {
                results.Add("AlwaysOn", AlwaysOn + " does not match " + other.AlwaysOn);
            }

            if (!DNSSuffix.EqualsArray(other.DNSSuffix))
            {
                results.Add("DNSSuffix", "DNS Suffix List does not match");
            }

            if (!TrustedNetworkDetection.EqualsArray(other.TrustedNetworkDetection))
            {
                results.Add("TrustedNetworkDetection", "Trusted Network Detection List does not match");
            }

            if (LockDown != other.LockDown)
            {
                results.Add("LockDown", LockDown + " does not match " + other.LockDown);
            }

            if (DeviceTunnel != other.DeviceTunnel)
            {
                results.Add("DeviceTunnel", DeviceTunnel + " does not match " + other.DeviceTunnel);
            }

            if (RegisterDNS != other.RegisterDNS)
            {
                results.Add("RegisterDNS", RegisterDNS + " does not match " + other.RegisterDNS);
            }

            if (ByPassForLocal != other.ByPassForLocal)
            {
                results.Add("ByPassForLocal", ByPassForLocal + " does not match " + other.ByPassForLocal);
            }

            if (!ServerList.EqualsArray(other.ServerList))
            {
                results.Add("ServerList", "Server List does not match");
            }

            if (!SplitTunnel == other.SplitTunnel)
            {
                results.Add("SplitTunnel", SplitTunnel + " does not match " + other.SplitTunnel);
            }

            if (!L2tpPsk.EqualsString(other.L2tpPsk))
            {
                results.Add("L2tpPsk", L2tpPsk + " does not match " + other.L2tpPsk);
            }

            if (ProxyType != other.ProxyType)
            {
                results.Add("ProxyType", ProxyType + " does not match " + other.ProxyType);
            }

            if (!ProxyServer.EqualsString(other.ProxyServer))
            {
                results.Add("ProxyServer", ProxyServer + " does not match " + other.ProxyServer);
            }

            if (DisableClassBasedDefaultRoute != other.DisableClassBasedDefaultRoute)
            {
                results.Add("DisableClassBasedDefaultRoute", DisableClassBasedDefaultRoute + " does not match " + other.DisableClassBasedDefaultRoute);
            }

            if (CryptographySuite != other.CryptographySuite)
            {
                results.Add("CryptographySuite", "Cryptography Suite does not match");
            }

            if (!AuthenticationMethod.EqualsString(other.AuthenticationMethod))
            {
                results.Add("AuthenticationMethod", AuthenticationMethod + " does not match " + other.AuthenticationMethod);
            }

            if (!EapConfig.EqualsString(other.EapConfig))
            {
                results.Add("EapConfig", EapConfig + " does not match " + other.EapConfig);
            }

            if (!DomainNameInformation.EqualsArray(other.DomainNameInformation))
            {
                results.Add("DomainNameInformation", "Domain Name Information List does not match");
            }

            if (!RouteList.EqualsArray(other.RouteList))
            {
                results.Add("RouteList", "Route List does not match");
            }

            if (FastReconnect != other.FastReconnect)
            {
                results.Add("FastReconnect", FastReconnect + " does not match " + other.FastReconnect);
            }

            if (DeviceComplianceEnabled != other.DeviceComplianceEnabled)
            {
                results.Add("DeviceComplianceEnabled", DeviceComplianceEnabled + " does not match " + other.DeviceComplianceEnabled);
            }

            if (DeviceComplianceSSOEnabled != other.DeviceComplianceSSOEnabled)
            {
                results.Add("DeviceComplianceSSOEnabled", DeviceComplianceSSOEnabled + " does not match " + other.DeviceComplianceSSOEnabled);
            }

            if (DeviceComplianceSSOEKU != other.DeviceComplianceSSOEKU)
            {
                results.Add("DeviceComplianceSSOEKU", DeviceComplianceSSOEKU + " does not match " + other.DeviceComplianceSSOEKU);
            }

            if (DeviceComplianceSSOIssuerHash != other.DeviceComplianceSSOIssuerHash)
            {
                results.Add("DeviceComplianceSSOIssuerHash", DeviceComplianceSSOIssuerHash + " does not match " + other.DeviceComplianceSSOIssuerHash);
            }

            if (!TrafficFilterList.EqualsArray(other.TrafficFilterList))
            {
                results.Add("TrafficFilterList", "Traffic Filter List does not match");
            }

            if (ProfileName != other.ProfileName)
            {
                results.Add("ProfileName", ProfileName + " does not match " + other.ProfileName);
            }

            if (!string.IsNullOrWhiteSpace(LoadError))
            {
                results.Add("Profile Load Error", ProfileName + " has loading errors " + LoadError);
            }

            if (!string.IsNullOrWhiteSpace(other.LoadError))
            {
                results.Add("Profile Load Error", other.ProfileName + " has loading errors " + other.LoadError);
            }

            if (DisableUIDisconnectButton != other.DisableUIDisconnectButton)
            {
                results.Add("DisableUIDisconnectButton", DisableUIDisconnectButton + " does not match " + other.DisableUIDisconnectButton);
            }

            if (DisableUIEditButton != other.DisableUIEditButton)
            {
                results.Add("DisableUIEditButton", DisableUIEditButton + " does not match " + other.DisableUIEditButton);
            }

            return results;
        }

        public override int GetHashCode()
        {
            int hashCode = 1749855708;
            //hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(EdpModeId);
            hashCode = hashCode * -1521134295 + RememberCredentials.GetHashCode();
            hashCode = hashCode * -1521134295 + AlwaysOn.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<IList<string>>.Default.GetHashCode(DNSSuffix);
            hashCode = hashCode * -1521134295 + EqualityComparer<IList<string>>.Default.GetHashCode(TrustedNetworkDetection);
            hashCode = hashCode * -1521134295 + LockDown.GetHashCode();
            hashCode = hashCode * -1521134295 + DeviceTunnel.GetHashCode();
            hashCode = hashCode * -1521134295 + RegisterDNS.GetHashCode();
            hashCode = hashCode * -1521134295 + ByPassForLocal.GetHashCode();
            hashCode = hashCode * -1521134295 + ProxyType.GetHashCode();
            hashCode = hashCode * -1521134295 + ProxyServer.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<IList<string>>.Default.GetHashCode(ServerList);
            hashCode = hashCode * -1521134295 + SplitTunnel.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(NativeProtocolType);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(L2tpPsk);
            hashCode = hashCode * -1521134295 + DisableClassBasedDefaultRoute.GetHashCode();
            hashCode = hashCode * -1521134295 + CryptographySuite.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(AuthenticationMethod);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(EapConfig);
            hashCode = hashCode * -1521134295 + EqualityComparer<IList<DomainNameInformation>>.Default.GetHashCode(DomainNameInformation);
            hashCode = hashCode * -1521134295 + RouteList.GetHashCode();
            hashCode = hashCode * -1521134295 + FastReconnect.GetHashCode();
            //hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(APNBinding);
            hashCode = hashCode * -1521134295 + DeviceComplianceEnabled.GetHashCode();
            hashCode = hashCode * -1521134295 + DeviceComplianceSSOEnabled.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(DeviceComplianceSSOEKU);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(DeviceComplianceSSOIssuerHash);
            //hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(PluginProfile);
            //hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(AppTrigger);
            hashCode = hashCode * -1521134295 + EqualityComparer<IList<TrafficFilter>>.Default.GetHashCode(TrafficFilterList);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ProfileName);
            //hashCode = hashCode * -1521134295 + RequireVpnClientAppUI.GetHashCode();
            //hashCode = hashCode * -1521134295 + PlumbIKEv2TSAsRoutes.GetHashCode();
            hashCode = hashCode * -1521134295 + DisableUIDisconnectButton.GetHashCode();
            hashCode = hashCode * -1521134295 + DisableUIEditButton.GetHashCode();
            return hashCode;
        }

        public override string ToString()
        {
            StringBuilder returnString = new StringBuilder();
            returnString.AppendLine("ProfileName: " + ProfileName);
            returnString.AppendLine("RememberCredentials: " + RememberCredentials);
            returnString.AppendLine("AlwaysOn: " + AlwaysOn);
            foreach (string dNSSuffix in DNSSuffix)
            {
                returnString.AppendLine("DNSSuffix: " + dNSSuffix);
            }

            foreach (string trustedNetworkDetection in TrustedNetworkDetection)
            {
                returnString.AppendLine("TrustedNetworkDetection: " + trustedNetworkDetection);
            }

            returnString.AppendLine("LockDown: " + LockDown);
            returnString.AppendLine("DisableUIEditButton: " + DisableUIEditButton);
            returnString.AppendLine("DisableUIDisconnectButton: " + DisableUIDisconnectButton);
            returnString.AppendLine("DeviceTunnel: " + DeviceTunnel);
            returnString.AppendLine("RegisterDNS: " + RegisterDNS);
            returnString.AppendLine("ByPassForLocal: " + ByPassForLocal);
            returnString.AppendLine("ProxyType: " + ProxyType);
            returnString.AppendLine("ProxyServer: " + ProxyServer);
            returnString.AppendLine("DeviceComplianceEnabled: " + DeviceComplianceEnabled);
            returnString.AppendLine("DeviceComplianceSSOEnabled: " + DeviceComplianceSSOEnabled);
            returnString.AppendLine("DeviceComplianceSSOEKU: " + DeviceComplianceSSOEKU);
            returnString.AppendLine("DeviceComplianceSSOIssuerHash: " + DeviceComplianceSSOIssuerHash);


            foreach (DomainNameInformation domainNameInformation in DomainNameInformation)
            {
                returnString.AppendLine("DomainNameInformation: " + domainNameInformation);
            }

            foreach (TrafficFilter trafficFilterList in TrafficFilterList)
            {
                returnString.AppendLine("TrafficFilterList: " + trafficFilterList);
            }

            returnString.AppendLine();
            returnString.AppendLine("Native Profile: ");

            foreach (string serverList in ServerList)
            {
                returnString.AppendLine("ServerList: " + serverList);
            }

            returnString.AppendLine("SplitTunnel: " + SplitTunnel);
            returnString.AppendLine("NativeProtocolType: " + NativeProtocolType);
            returnString.AppendLine("L2tpPsk: " + L2tpPsk);
            returnString.AppendLine("DisableClassBasedDefaultRoute: " + DisableClassBasedDefaultRoute);
            returnString.AppendLine("CryptographySuite: ");
            returnString.AppendLine(CryptographySuite.ToString());
            returnString.AppendLine("AuthenticationMethod: " + AuthenticationMethod);
            returnString.AppendLine("EapConfig: " + EapConfig);
            returnString.AppendLine("FastReconnect: " + FastReconnect);

            foreach (Route routeList in RouteList)
            {
                returnString.AppendLine("RouteList: " + routeList);
            }

            returnString.AppendLine();
            returnString.AppendLine("Errors:");
            returnString.AppendLine(LoadError);

            return returnString.ToString();
        }

        public static bool operator ==(VPNProfile left, VPNProfile right)
        {
            return EqualityComparer<VPNProfile>.Default.Equals(left, right);
        }

        public static bool operator !=(VPNProfile left, VPNProfile right)
        {
            return !(left == right);
        }

        internal void FormatAndSaveEapConfig(XElement EapConfigXML)
        {
            if (EapConfigXML != null)
            {
                foreach (XElement CANode in EapConfigXML.XPathSelectElements("//*[local-name()='TrustedRootCA']"))
                {
                    CANode.Value = CANode.Value.Replace(" ", "");
                }

                foreach (XElement CANode in EapConfigXML.XPathSelectElements("//*[local-name()='IssuerHash']"))
                {
                    CANode.Value = CANode.Value.Replace(" ", "");
                }

                IList<XElement> serverNameElementList = EapConfigXML.XPathSelectElements("//*[local-name()='ServerNames']").ToList();

                //Have to use a for loop as we are modifying the collection which is being iterated over which means that some elements are missed
                for (int i = serverNameElementList.Count-1;i >= 0;i--)
                {
                    //When NPS Validation is disabled the EAP profile doesn't have the element ServerNames however the WMI Profile inserts a blank element which causes conflicts
                    if (string.IsNullOrWhiteSpace(serverNameElementList[i].Value))
                    {
                        serverNameElementList[i].Remove();
                    }
                }

                FastReconnect = Convert.ToBoolean(EapConfigXML.XPathSelectElement("//*[local-name()='FastReconnect']")?.Value, CultureInfo.InvariantCulture);
                EapConfigXML.XPathSelectElement("//*[local-name()='FastReconnect']")?.Remove();

                XAttribute CAHashListEnabled = EapConfigXML.XPathSelectElement("//*[local-name()='CAHashList']")?.Attribute("Enabled");
                if (CAHashListEnabled != null && CAHashListEnabled.Value == "false")
                {
                    //Remove the attribute when enabled is false because windows only returns this attribute when it is true
                    CAHashListEnabled.Remove();
                }

                //Save the updated EapConfig for later comparison
                EapConfig = EapConfigXML.FirstNode?.ToString(SaveOptions.DisableFormatting);
            }
        }

        public static bool CompareToInstalledProfile(string profileName, string desiredProfile, CancellationToken cancelToken)
        {
            IList<ProfileInfo> installedProfiles = ManageRasphonePBK.ListSystemProfiles(profileName);

            if (string.IsNullOrWhiteSpace(desiredProfile) && installedProfiles.Count == 0)
            {
                //Desired no profile and no profile found on system
                return true;
            }

            if (string.IsNullOrWhiteSpace(desiredProfile) ^ installedProfiles.Count == 0)
            {
                //One but not both profiles are empty so they can't be the same
                return false;
            }

            //If no profiles installed installedProfiles will be empty so skipping the foreach loop
            foreach (ProfileInfo installedProfile in installedProfiles)
            {
                if (CompareProfiles(new WMIProfile(installedProfile, cancelToken), new CSPProfile(desiredProfile, profileName)))
                {
                    //Found a matching profile
                    return true;
                }
            }

            //No matching profile found
            return false;
        }

        public static Dictionary<string, string> CompareToInstalledProfileWithResults(string profileName, string desiredProfile, CancellationToken cancelToken)
        {
            IList<ProfileInfo> installedProfiles = ManageRasphonePBK.ListProfiles(profileName);

            //Both profiles are blank, so true nothing to do
            if (string.IsNullOrWhiteSpace(desiredProfile) && installedProfiles.Count == 0)
            {
                return new Dictionary<string, string>(); //Profiles are equal so send back clear results
            }
            else if (string.IsNullOrWhiteSpace(desiredProfile) ^ installedProfiles.Count == 0)
            {
                //If one but not both profiles is null then they can't be equal
                Dictionary<string, string> results = new Dictionary<string, string>();
                if (string.IsNullOrWhiteSpace(desiredProfile))
                {
                    results.Add("Missing Profile", "Desired Profile is Null");
                }

                if (installedProfiles.Count == 0)
                {
                    results.Add("Missing Profile", "Installed Profile could not be found");
                }
                return results;
            }
            else if (installedProfiles.Count > 1)
            {
                Dictionary<string, string> results = new Dictionary<string, string>
                {
                    { "Unexpected Profiles", installedProfiles.Count.ToString(CultureInfo.InvariantCulture) + " Installed Profiles found when only 1 was expected" }
                };
                return results;
            }
            else
            {
                //Already done the bounds check above to ensure that there is 1 and only 1 profile
                VPNProfile profile1 = new WMIProfile(installedProfiles[0], cancelToken);
                VPNProfile profile2 = new CSPProfile(desiredProfile, profileName);

                return profile1.EqualsResults(profile2);
            }
        }

        //Run the same set of normalisation for both CSP and WMI Profiles to make sure they are in line
        internal void NormaliseProxyServer()
        {
            if (string.IsNullOrWhiteSpace(ProxyServer))
            {
                //ProxyServer has not been set, so keep it that way
                return;
            }
            //remove http and https from start of stings and this doesn't appear to be included in the profile
            if (ProxyServer.Contains("://"))
            {
                ProxyServer = Regex.Match(ProxyServer, @"(?<=://)+(.*)?").Value;
            }

            //If no port is defined assume it is port 80 as thats what windows does
            if (!ProxyServer.Contains(":"))
            {
                ProxyServer += ":80";
            }
        }
    }
}
