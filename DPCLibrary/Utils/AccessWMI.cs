using DPCLibrary.Enums;
using DPCLibrary.Exceptions;
using DPCLibrary.Models;
using Microsoft.Management.Infrastructure;
using Microsoft.Management.Infrastructure.Generic;
using Microsoft.Management.Infrastructure.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using System.Xml.XPath;

namespace DPCLibrary.Utils
{
    public static class AccessWMI
    {
        private static readonly object profileActionLock = new object();

        private static readonly CimSession WMISession = CimSession.Create(null); //Connect to local WMI

        private const string MDMWMINamespace = @"root\cimv2\mdm\dmmap";
        private const string MDMWMIClassName = "MDM_VPNv2_01";
        private const string NetAdapterWMINamespace = @"root\StandardCimv2";
        private const string NetAdapterWMIClassName = "MSFT_NetAdapter";
        private const string NetAdapterConfigWMINamespace = @"root\Cimv2";
        private const string NetAdapterConfigWMIClassName = "Win32_NetworkAdapterConfiguration";
        private const string NetInterfaceWMIClassName = "MSFT_NetIPInterface";
        private const string RemoteAccessWMINamespace = @"root\Microsoft\Windows\RemoteAccess\Client";

        private const string CSPURI = "./Vendor/MSFT/VPNv2";

        public static IList<CimInstance> GetNetworkAdapters()
        {
            IEnumerable<CimInstance> NetAdapters = WMISession.EnumerateInstances(NetAdapterWMINamespace, NetAdapterWMIClassName);
            return NetAdapters.ToList();
        }

        public static IList<CimInstance> GetNetIPInterfaces()
        {
            IEnumerable<CimInstance> NetAdapters = WMISession.EnumerateInstances(NetAdapterWMINamespace, NetInterfaceWMIClassName);
            return NetAdapters.ToList();
        }

        public static IList<CimInstance> GetNetworkAdapterConfig()
        {
            IEnumerable<CimInstance> NetConfig = WMISession.EnumerateInstances(NetAdapterConfigWMINamespace, NetAdapterConfigWMIClassName);
            return NetConfig.ToList();
        }

        //Can't use Getinstance as Name format is not understood (see Get-NetIPInterface | ft name, interfacealias, addressfamily), instead pull
        //List of all interfaces and loop through them searching for the correct interface name
        public static CimInstance GetNetIPInterface(string interfaceName, IPAddressFamily protocol)
        {
            IList<CimInstance> interfaces = GetNetIPInterfaces();
            foreach (CimInstance instance in interfaces)
            {
                if ((string)instance.CimInstanceProperties["InterfaceAlias"].Value == interfaceName)
                {
                    int familyInt = Convert.ToInt32(instance.CimInstanceProperties["AddressFamily"].Value, CultureInfo.InvariantCulture);
                    if ((IPAddressFamily)familyInt == protocol)
                    {
                        return instance;
                    }
                }
            }

            return null;
        }

        //Can't use Getinstance as Name format is not understood (see Get-NetIPInterface | ft name, interfacealias, addressfamily), instead pull
        //List of all interfaces and loop through them searching for the correct interface name
        public static uint? GetNetIPInterfaceIndex(string interfaceName, IPAddressFamily protocol)
        {
            IList<CimInstance> interfaces = GetNetIPInterfaces();
            foreach (CimInstance instance in interfaces)
            {
                if ((string)instance.CimInstanceProperties["InterfaceAlias"].Value == interfaceName)
                {
                    int familyInt = Convert.ToInt32(instance.CimInstanceProperties["AddressFamily"].Value, CultureInfo.InvariantCulture);
                    if ((IPAddressFamily)familyInt == protocol)
                    {
                        return Convert.ToUInt32(instance.CimInstanceProperties["InterfaceIndex"].Value, CultureInfo.InvariantCulture);
                    }
                }
            }

            return null;
        }

        public static CimInstance GetNetAdapterConfig(string interfaceName, IPAddressFamily protocol)
        {
            CimInstance interfaceInstance = GetNetIPInterface(interfaceName, protocol);
            if (interfaceInstance == null) return null;

            return GetNetAdapterConfig(Convert.ToInt32(interfaceInstance.CimInstanceProperties["InterfaceIndex"].Value, CultureInfo.InvariantCulture));
        }

        public static CimInstance GetNetAdapterConfig(int interfaceIndex)
        {
            IList<CimInstance> adapters = GetNetworkAdapterConfig();
            foreach (CimInstance instance in adapters)
            {
                if (Convert.ToInt32(instance.CimInstanceProperties["InterfaceIndex"].Value, CultureInfo.InvariantCulture) == interfaceIndex)
                {
                    return instance;
                }
            }

            return null;
        }

        public static uint? GetInterfaceMTU(string interfaceName, IPAddressFamily protocol)
        {
            CimInstance instance = GetNetIPInterface(interfaceName, protocol);
            if (instance == null) return null;

            return Convert.ToUInt32(instance.CimInstanceProperties["NlMtu"].Value, CultureInfo.InvariantCulture);
        }

        private static CimOperationOptions GetContextOptions(string sid, CancellationToken cancelToken)
        {
            CimOperationOptions options = new CimOperationOptions
            {
                CancellationToken = cancelToken
            };

            if (sid != null && sid != DeviceInfo.SYSTEMSID)
            {
                options.SetCustomOption("PolicyPlatformContext_PrincipalContext_Type", "PolicyPlatform_UserContext", false);
                options.SetCustomOption("PolicyPlatformContext_PrincipalContext_Id", sid, false);
            }
            else if (!DeviceInfo.IsCurrentUserSYSTEM())
            {
                //In Windows 10 this enables tests to access the SYSTEM context while only running admin
                //Taken from https://docs.microsoft.com/en-us/previous-versions/windows/desktop/usm/win32-offlinefilesmachineconfiguration, enables explicit access to the machine context from user context
                options.SetCustomOption("PolicyPlatformContext_PrincipalContext_Type", "PolicyPlatform_MachineContext", false);
                options.SetCustomOption("PolicyPlatformContext_PrincipalContext_Id", "SYSTEM", false);
            }

            return options;
        }

        public static void NewProfile(string profileName, string profileXML, CancellationToken cancelToken)
        {
            //Multiple profile modifications at the same time can cause errors relating to the rasphone.pbk file being locked
            lock (profileActionLock)
            {
                using (CimInstance newProfile = new CimInstance(MDMWMIClassName, MDMWMINamespace))
                {
                    using (CimOperationOptions options = GetContextOptions(DeviceInfo.SYSTEMSID, cancelToken))
                    {
                        newProfile.CimInstanceProperties.Add(CimProperty.Create("ParentID", CSPURI, CimType.String, CimFlags.Key));
                        newProfile.CimInstanceProperties.Add(CimProperty.Create("InstanceID", SanitizeName(profileName), CimType.String, CimFlags.Key));
                        newProfile.CimInstanceProperties.Add(CimProperty.Create("ProfileXML", Sanitize(profileXML), CimType.String, CimFlags.Property));

                        WMISession.CreateInstance(MDMWMINamespace, newProfile, options);
                    }
                }
            }
        }

        private static string SanitizeName(string name)
        {
            name = name.Replace(" ", "%20");
            return name;
        }

        private static string Sanitize(string profileData)
        {
            profileData = profileData.Replace("<", "&lt;");
            profileData = profileData.Replace(">", "&gt;");
            profileData = profileData.Replace("\"", "&quot;");
            return profileData;
        }

        public static string Unsanitize(string profileData)
        {
            if (string.IsNullOrWhiteSpace(profileData))
            {
                //Avoid a crash from a null valued string
                return profileData;
            }
            profileData = profileData.Replace("%20", " ");
            profileData = profileData.Replace("&lt;", "<");
            profileData = profileData.Replace("&gt;", ">");
            return profileData;
        }

        public static bool SetMachineCertificateEKUFilter(string profileName, IList<string> EKU, CancellationToken cancelToken)
        {
            //Multiple profile modifications at the same time can cause errors relating to the rasphone.pbk file being locked
            lock (profileActionLock)
            {
                IList<string> updatedEKUList;
                if (EKU == null || EKU.Count == 0)
                {
                    updatedEKUList = (new string[0]).ToList();
                }
                else
                {
                    updatedEKUList = EKU;
                }
                if (GetMachineCertificateEKUFilter(profileName, cancelToken).EqualsArray(updatedEKUList))
                {
                    //Already Equals, nothing to do
                    return false;
                }
                using (CimOperationOptions options = GetContextOptions(DeviceInfo.SYSTEMSID, cancelToken))
                {
                    CimMethodParametersCollection certParams = new CimMethodParametersCollection
                    {
                        CimMethodParameter.Create("AllUserConnection", DeviceInfo.IsCurrentUserSYSTEM(), CimFlags.In), //Point WMI at Machine List when running as system as profiles would have been written in this context
                        CimMethodParameter.Create("Name", profileName, CimFlags.In),
                        CimMethodParameter.Create("MachineCertificateEKUFilter", updatedEKUList, CimFlags.In)
                    };
                    WMISession.InvokeMethod(RemoteAccessWMINamespace, "PS_VpnConnection", "Set", certParams, options);
                }
                return true;
            }
        }

        public static bool SetProxyExcludeExceptions(string profileName, IList<string> excludeList, bool bypassForLocal, CancellationToken cancelToken)
        {
            //Multiple profile modifications at the same time can cause errors relating to the rasphone.pbk file being locked
            lock (profileActionLock)
            {
                if (GetProxyExcludeList(profileName, cancelToken).EqualsArray(excludeList) && bypassForLocal == GetBypassProxyForLocal(profileName, cancelToken))
                {
                    //Already Equals, nothing to do
                    return false;
                }

                List<string> proxyExcludeList = new List<string>(excludeList);

                using (CimOperationOptions options = GetContextOptions(DeviceInfo.SYSTEMSID, cancelToken))
                {
                    proxyExcludeList.RemoveAll(e => string.IsNullOrEmpty(e));
                    using (CimMethodParametersCollection certParams = new CimMethodParametersCollection
                    {
                        CimMethodParameter.Create("ConnectionName", profileName, CimFlags.In),
                        CimMethodParameter.Create("ExceptionPrefix", proxyExcludeList, CimFlags.In),
                        CimMethodParameter.Create("BypassProxyForLocal", bypassForLocal, CimFlags.In)
                    })
                    {
                        if (excludeList.Count > 0)
                        {
                            string proxyServer = GetProxyServer(profileName, cancelToken);
                            if (!string.IsNullOrWhiteSpace(proxyServer))
                            {
                                certParams.Add(CimMethodParameter.Create("ProxyServer", proxyServer, CimFlags.In));
                            }
                            else
                            {
                                throw new InvalidProfileException("Proxy Server must be used to take advantage of Exception Prefixs and Bypass for Local Settings");
                            }
                        }
                        WMISession.InvokeMethod(RemoteAccessWMINamespace, "PS_VpnConnectionProxy", "Set", certParams, options);
                    }
                }
                return true;
            }
        }

        private static IList<string> GetMachineCertificateEKUFilter(string profileName, CancellationToken cancelToken)
        {
            IList<CimInstance> profileDetailsList = GetRemoteAccessArrayWMI(profileName, cancelToken);

            if (profileDetailsList.Count == 0)
            {
                throw new Exception("Unable to locate " + profileName);
            }

            CimInstance profileDetails = profileDetailsList.First();
            string[] EKUData = (string[])profileDetails.CimInstanceProperties["MachineCertificateEKUFilter"].Value;
            return EKUData.ToList();
        }

        private static IList<string> GetProxyExcludeList(string profileName, CancellationToken cancelToken)
        {
            IList<string> profileDetailsList = new List<string>();
            string XML = GetWMIVPNConfig(profileName, cancelToken);
            XDocument doc = XDocument.Parse(XML);
            IList<XElement> exceptionPrefixList = doc.Root.XPathSelectElements("/VpnProfile/VpnConfiguration/VpnProxy/ExceptionPrefix").ToList();
            foreach (XElement prefix in exceptionPrefixList)
            {
                profileDetailsList.Add(prefix.Value);
            }
            return profileDetailsList;
        }

        private static string GetProxyServer(string profileName, CancellationToken cancelToken)
        {
            string XML = GetWMIVPNConfig(profileName, cancelToken);
            XDocument doc = XDocument.Parse(XML);
            XElement proxyServer = doc.Root.XPathSelectElement("/VpnProfile/VpnConfiguration/VpnProxy/ProxyServer");
            if (proxyServer == null)
            {
                //Most likely proxy is using autoconfigure URL
                return null;
            }
            string proxyServerName = proxyServer.Value;
            //Some solutions enable you to add a proxy server with a http:// prefix, this breaks the WMI call so we need to remove this
            if (proxyServerName.Contains("://"))
            {
                string[] proxyServerParts = proxyServerName.Split(new string[] { "://" }, StringSplitOptions.RemoveEmptyEntries);
                return proxyServerParts.Last();
            }
            else
            {
                return proxyServerName;
            }
        }

        private static bool GetBypassProxyForLocal(string profileName, CancellationToken cancelToken)
        {
            string XML = GetWMIVPNConfig(profileName, cancelToken);
            XDocument doc = XDocument.Parse(XML);
            XElement bypassProxyForLocal = doc.Root.XPathSelectElement("/VpnProfile/VpnConfiguration/VpnProxy/BypassProxyForLocal");
            if (bypassProxyForLocal == null)
            {
                //if in doubt set the bypass for local to false
                return false;
            }
            return Convert.ToBoolean(bypassProxyForLocal.Value, CultureInfo.InvariantCulture);
        }

        public static string GetWMIVPNConfig(string profileName, CancellationToken cancelToken)
        {
            //Multiple profile modifications at the same time can cause errors relating to the rasphone.pbk file being locked
            lock (profileActionLock)
            {
                IList<CimInstance> profileDetailsList = GetRemoteAccessArrayWMI(profileName, cancelToken);

                if (profileDetailsList.Count == 0)
                {
                    throw new InvalidProfileException("Unable to locate " + profileName);
                }

                CimInstance profileDetails = profileDetailsList.First();
                return (string)profileDetails.CimInstanceProperties["VpnConfigurationXml"].Value;
            }
        }

        public static Guid GetWMIVPNGuid(string profileName, CancellationToken cancelToken)
        {
            //Multiple profile modifications at the same time can cause errors relating to the rasphone.pbk file being locked
            lock (profileActionLock)
            {
                IList<CimInstance> profileDetailsList = GetRemoteAccessArrayWMI(profileName, cancelToken);

                if (profileDetailsList.Count == 0)
                {
                    throw new InvalidProfileException("Unable to locate " + profileName);
                }

                CimInstance profileDetails = profileDetailsList.First();
                return new Guid((string)profileDetails.CimInstanceProperties["Guid"].Value);
            }
        }

        internal static IList<CimInstance> GetRemoteAccessArrayWMI(string profileName, CancellationToken cancelToken)
        {
            using (CimOperationOptions options = GetContextOptions(DeviceInfo.SYSTEMSID, cancelToken))
            {
                CimMethodParametersCollection certParams = new CimMethodParametersCollection
                {
                    CimMethodParameter.Create("AllUserConnection", DeviceInfo.IsCurrentUserSYSTEM(), CimFlags.In), //Point WMI at Machine List when running as system as profiles would have been written in this context
                    CimMethodParameter.Create("Name", new string[]{ profileName }, CimFlags.In), //WMI requires the profile be a list
                };

                CimMethodResult cimMethodResult = WMISession.InvokeMethod(RemoteAccessWMINamespace, "PS_VpnConnection", "Get", certParams, options);
                IList<CimInstance> valueList = new List<CimInstance>();
                if (cimMethodResult != null)
                {
                    CimReadOnlyKeyedCollection<CimMethodParameter> resultOutput = cimMethodResult.OutParameters;
                    foreach (CimMethodParameter resultValue in resultOutput)
                    {
                        switch (resultValue.Name)
                        {
                            case "cmdletOutput":
                                {
                                    CimInstance[] cimInstances = (CimInstance[])resultValue.Value;
                                    foreach (CimInstance cimInstance in cimInstances)
                                    {
                                        valueList.Add(cimInstance);
                                    }
                                    break;
                                }
                            case "ReturnValue": if (resultValue.Value != null && (uint)resultValue.Value != 0) { throw new Exception("Error"); } break;
                            default:
                                throw new Exception("Unknown return from CIM");
                        }
                    }
                }
                return valueList;
            }
        }

        //Returns Null if no profile found
        public static string GetProfileData(string profileName, CancellationToken cancelToken)
        {
            //Multiple profile modifications at the same time can cause errors relating to the rasphone.pbk file being locked
            lock (profileActionLock)
            {
                WMIProfileInstance profile = GetProfile(profileName, cancelToken);

                if (profile == null)
                {
                    return null;
                }

                return profile.ProfileData;
            }
        }

        //Returns Null if no profile found
        internal static WMIProfileInstance GetProfile(string profileName, CancellationToken cancelToken)
        {
            IList<WMIProfileInstance> returnedProfiles = GetProfileList(profileName, cancelToken);

            if (returnedProfiles.Count > 0)
            {
                WMIProfileInstance returnInst = returnedProfiles.First();
                return returnInst;
            }

            return null;
        }

        //Return empty List if no profile found NOT NULL
        internal static IList<WMIProfileInstance> GetProfileList(string profileName, CancellationToken cancelToken)
        {
            List<ProfileInfo> namedProfiles = ManageRasphonePBK.ListProfiles(profileName);
            return GetProfileList(namedProfiles, cancelToken);
        }

        //Return empty List if no profile found NOT NULL
        internal static IList<WMIProfileInstance> GetProfileList(List<ProfileInfo> namedProfiles, CancellationToken cancelToken)
        {
            List<WMIProfileInstance> returnedProfiles = new List<WMIProfileInstance>();
            foreach (ProfileInfo profile in namedProfiles)
            {
                returnedProfiles.Add(GetProfileInstance(profile, cancelToken));
            }

            return returnedProfiles;
        }

        internal static WMIProfileInstance GetProfileInstance(ProfileInfo profileInfo, CancellationToken cancelToken)
        {
            try
            {
                using (CimOperationOptions options = GetContextOptions(profileInfo.Sid, cancelToken))
                {
                    CimInstance instanceSearcher = new CimInstance(MDMWMIClassName, MDMWMINamespace);
                    instanceSearcher.CimInstanceProperties.Add(CimProperty.Create("ParentID", CSPURI, CimType.String, CimFlags.Key));
                    instanceSearcher.CimInstanceProperties.Add(CimProperty.Create("InstanceID", SanitizeName(profileInfo.ProfileName), CimType.String, CimFlags.Key));
                    CimInstance returnInstance = WMISession.GetInstance(MDMWMINamespace, instanceSearcher, options);
                    return new WMIProfileInstance(profileInfo.Sid, returnInstance);
                }
            }
            catch
            {
                //Unable to get Instance
                return new WMIProfileInstance(profileInfo.Sid, profileInfo.ProfileName);
            }
        }
    }
}