using DPCLibrary.Enums;
using DPCLibrary.Exceptions;
using DPCLibrary.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Schema;

namespace DPCLibrary.Utils
{
    /// <summary>
    /// Main XML Generation Class, when a new profile is needed this class reads the profile requirements from registry (or passed in for testing purposes),
    /// attempts to validate the settings where possible to avoid invalid profile configurations then generates the XML.  This is finally validated against
    /// the XML schema before being returned. To enable errors and warnings to bubble up these are written to string buffers which can then be checked as
    /// required by calling classes.
    /// </summary>
    public class VPNProfileCreator
    {
        private static readonly object O365ExclusionLock = new object();

        private readonly StringBuilder ProfileString = new StringBuilder();
        private readonly StringBuilder ValidationFailures = new StringBuilder();
        private readonly StringBuilder ValidationWarnings = new StringBuilder();
        private readonly StringBuilder ValidationDebugMessages = new StringBuilder();
        private readonly string TunnelRegOffset;

        //All Tunnels
        //Essential Properties
        private readonly ProfileType ProfileType;
        private string ProfileName;
        private TunnelType TunnelType;
        private string ExternalAddress;

        //Optional Properties
        private string OverrideXML;
        private IList<string> DNSSuffixList;
        private IList<string> TrustedNetworkList;
        private Dictionary<string, string> RouteList;
        private Dictionary<string, string> RouteExcludeList;
        private bool CustomCryptography;
        private string AuthenticationTransformConstants;
        private string CipherTransformConstants;
        private string PfsGroup;
        private string DHGroup;
        private string IntegrityCheckMethod;
        private string EncryptionMethod;
        private Dictionary<string, string> DomainInformationList;
        private bool UseProxy;
        private ProxyType ProxyType;
        private string ProxyValue;
        private uint InterfaceMetric;
        private uint RouteMetric;
        private uint MTU;
        private bool DisableRasCredentials;
        private uint NetworkOutageTime = 1800;
        private bool RegisterDNS;
        private bool DNSAlreadyRegistered;
        private IList<TrafficFilter> TrafficFilters;
        private IList<string> ProxyExcludeList;
        private bool ProxyBypassForLocal;
        private bool DisableUIDisconnectButton;
        private bool DisableUIEditButton;

        //User Tunnel
        //Essential Properties
        private IList<string> RootThumbprintList;
        private IList<string> IssuingThumbprintList;
        private IList<string> NPSServerList;

        //Optional properties
        private bool EKUMapping;
        private bool EAPSmartCard;
        private string EKUName;
        private string EKUOID;
        private bool DeviceComplianceEnabled;
        private string DeviceComplianceEKUOID;
        private string DeviceComplianceIssuerHash;
        private VPNStrategy VPNStrategy = VPNStrategy.Ikev2Only; //Default to IKEv2
        private bool DisableAlwaysOn;
        private bool DisableCryptoBinding;
        private Dictionary<string, string> DNSRouteList;
        private bool DisableNPSValidation;

        //Device Tunnel
        //Optional Properties
        private IList<string> MachineEKU;

        /// <summary>
        /// Initializes the profile generation. Each generation typically creates its own instance of the class which is also specific to the type of
        /// tunnel being created
        /// </summary>
        /// <param name="profileType">The Type of tunnel such as Device or User</param>
        /// <param name="loadFromReg">Specify if registry settings should be loaded automatically, primarily set to false for unit testing</param>
        public VPNProfileCreator(ProfileType profileType, bool loadFromReg)
        {
            ProfileType = profileType;

            TunnelRegOffset = RegistrySettings.GetProfileOffset(ProfileType);

            if (loadFromReg)
            {
                LoadFromRegistry();
            }
        }

        /// <summary>
        /// Testing Helper function to initalise the class and all profile values without needing to inject them or mock them into the registry first
        /// </summary>
        /// <param name="profileName"></param>
        /// <param name="tunnelType"></param>
        /// <param name="externalAddress"></param>
        /// <param name="rootThumbprintList"></param>
        /// <param name="issuingThumbprintList"></param>
        /// <param name="nPSServerList"></param>
        /// <param name="overrideXML"></param>
        /// <param name="dNSSuffixList"></param>
        /// <param name="trustedNetworkList"></param>
        /// <param name="routeList"></param>
        /// <param name="routeExcludeList"></param>
        /// <param name="customCryptography"></param>
        /// <param name="authenticationTransformConstants"></param>
        /// <param name="cipherTransformConstants"></param>
        /// <param name="pfsGroup"></param>
        /// <param name="dHGroup"></param>
        /// <param name="integrityCheckMethod"></param>
        /// <param name="encryptionMethod"></param>
        /// <param name="domainInformationList"></param>
        /// <param name="eKUMapping"></param>
        /// <param name="eKUName"></param>
        /// <param name="eKUOID"></param>
        /// <param name="useProxy"></param>
        /// <param name="proxyType"></param>
        /// <param name="proxyValue"></param>
        /// <param name="interfaceMetric"></param>
        /// <param name="vPNStrategy"></param>
        /// <param name="disableRasCredentials"></param>
        /// <param name="networkOutageTime"></param>
        /// <param name="registerDNS"></param>
        /// <param name="dnsAlreadyRegistered"></param>
        /// <param name="routeMetric"></param>
        /// <param name="trafficFilters"></param>
        /// <param name="disableAlwaysOn"></param>
        /// <param name="disableCryptoBinding"></param>
        /// <param name="dnsRouteList"></param>
        /// <param name="enableEKUSmartCard"></param>
        /// <exception cref="InvalidProfileException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public void LoadUserProfile(
                string profileName,
                TunnelType tunnelType,
                string externalAddress,
                IList<string> rootThumbprintList,
                IList<string> issuingThumbprintList,
                IList<string> nPSServerList = null,
                string overrideXML = null,
                IList<string> dNSSuffixList = null,
                IList<string> trustedNetworkList = null,
                Dictionary<string, string> routeList = null,
                Dictionary<string, string> routeExcludeList = null,
                bool customCryptography = false,
                string authenticationTransformConstants = null,
                string cipherTransformConstants = null,
                string pfsGroup = null,
                string dHGroup = null,
                string integrityCheckMethod = null,
                string encryptionMethod = null,
                Dictionary<string, string> domainInformationList = null,
                bool eKUMapping = false,
                string eKUName = null,
                string eKUOID = null,
                bool useProxy = false,
                ProxyType proxyType = ProxyType.None,
                string proxyValue = null,
                uint interfaceMetric = 0,
                VPNStrategy vPNStrategy = VPNStrategy.Ikev2Only,
                bool disableRasCredentials = false,
                uint networkOutageTime = 1800,
                bool registerDNS = false,
                bool dnsAlreadyRegistered = false,
                uint routeMetric = 0,
                IList<TrafficFilter> trafficFilters = null,
                bool disableAlwaysOn = false,
                bool disableCryptoBinding = false,
                Dictionary<string, string> dnsRouteList = null,
                bool enableEKUSmartCard = false,
                bool excludeO365 = false,
                IList<string> proxyExcludeList = null,
                bool proxyBypassForLocal = false,
                bool disableUIDisconnectButton = false,
                bool disableUIEditButton = false,
                uint mTU = 1400,
                bool deviceComplianceEnabled = false,
                string deviceComplianceEKUOID = null,
                string deviceComplianceIssuerHash = null,
                bool disableNPSValidation = false
            )
        {
            if (ProfileType != ProfileType.User && ProfileType != ProfileType.UserBackup)
            {
                throw new InvalidProfileException("Instance not initialized as a User Profile");
            }

            ProfileName = profileName ?? throw new ArgumentNullException(nameof(profileName));
            TunnelType = tunnelType;
            ExternalAddress = externalAddress ?? throw new ArgumentNullException(nameof(externalAddress));
            OverrideXML = overrideXML;
            DNSSuffixList = dNSSuffixList ?? new List<string>();
            TrustedNetworkList = trustedNetworkList ?? new List<string>();
            RouteList = routeList ?? new Dictionary<string, string>();
            RouteExcludeList = routeExcludeList ?? new Dictionary<string, string>();
            CustomCryptography = customCryptography;
            AuthenticationTransformConstants = authenticationTransformConstants;
            CipherTransformConstants = cipherTransformConstants;
            PfsGroup = pfsGroup;
            DHGroup = dHGroup;
            IntegrityCheckMethod = integrityCheckMethod;
            EncryptionMethod = encryptionMethod;
            DomainInformationList = domainInformationList ?? new Dictionary<string, string>();
            RootThumbprintList = rootThumbprintList ?? throw new ArgumentNullException(nameof(rootThumbprintList));
            IssuingThumbprintList = issuingThumbprintList ?? throw new ArgumentNullException(nameof(issuingThumbprintList));
            NPSServerList = nPSServerList ?? new List<string>();
            EKUMapping = eKUMapping;
            EKUName = eKUName;
            EKUOID = eKUOID;
            UseProxy = useProxy;
            ProxyType = proxyType;
            ProxyValue = proxyValue;
            InterfaceMetric = interfaceMetric;
            VPNStrategy = vPNStrategy;
            NetworkOutageTime = networkOutageTime;
            DisableRasCredentials = disableRasCredentials;
            RegisterDNS = registerDNS;
            DNSAlreadyRegistered = dnsAlreadyRegistered;
            RouteMetric = routeMetric;
            TrafficFilters = trafficFilters ?? new List<TrafficFilter>();

            if (ProfileType == ProfileType.User) //Simulate registry load behavior
            {
                DisableAlwaysOn = disableAlwaysOn;
            }
            else
            {
                DisableAlwaysOn = true;
            }
            DisableCryptoBinding = disableCryptoBinding;
            DNSRouteList = dnsRouteList ?? new Dictionary<string, string>();
            EAPSmartCard = enableEKUSmartCard;
            ProxyExcludeList = proxyExcludeList ?? new List<string>();
            ProxyBypassForLocal = proxyBypassForLocal;
            if (excludeO365)
            {
                ConfigureO365ExcludeRoutes();
            }
            DisableUIDisconnectButton = disableUIDisconnectButton;
            DisableUIEditButton = disableUIEditButton;
            MTU = mTU;
            DeviceComplianceEnabled = deviceComplianceEnabled;
            DeviceComplianceEKUOID = deviceComplianceEKUOID;
            DeviceComplianceIssuerHash = deviceComplianceIssuerHash;
            DisableNPSValidation = disableNPSValidation;
        }

        /// <summary>
        /// Testing Helper function to initalise the class and all profile values without needing to inject them or mock them into the registry first
        /// </summary>
        /// <param name="profileName"></param>
        /// <param name="tunnelType"></param>
        /// <param name="externalAddress"></param>
        /// <param name="overrideXML"></param>
        /// <param name="dNSSuffixList"></param>
        /// <param name="trustedNetworkList"></param>
        /// <param name="routeList"></param>
        /// <param name="routeExcludeList"></param>
        /// <param name="customCryptography"></param>
        /// <param name="authenticationTransformConstants"></param>
        /// <param name="cipherTransformConstants"></param>
        /// <param name="pfsGroup"></param>
        /// <param name="dHGroup"></param>
        /// <param name="integrityCheckMethod"></param>
        /// <param name="encryptionMethod"></param>
        /// <param name="domainInformationList"></param>
        /// <param name="registerDNS"></param>
        /// <param name="dnsAlreadyRegistered"></param>
        /// <param name="useProxy"></param>
        /// <param name="proxyType"></param>
        /// <param name="proxyValue"></param>
        /// <param name="interfaceMetric"></param>
        /// <param name="disableRasCredentials"></param>
        /// <param name="networkOutageTime"></param>
        /// <param name="routeMetric"></param>
        /// <param name="trafficFilters"></param>
        /// <exception cref="InvalidProfileException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public void LoadMachineProfile(
                string profileName,
                TunnelType tunnelType,
                string externalAddress,
                string overrideXML = null,
                IList<string> dNSSuffixList = null,
                IList<string> trustedNetworkList = null,
                Dictionary<string, string> routeList = null,
                Dictionary<string, string> routeExcludeList = null,
                bool customCryptography = false,
                string authenticationTransformConstants = null,
                string cipherTransformConstants = null,
                string pfsGroup = null,
                string dHGroup = null,
                string integrityCheckMethod = null,
                string encryptionMethod = null,
                Dictionary<string, string> domainInformationList = null,
                bool registerDNS = false,
                bool dnsAlreadyRegistered = false,
                bool useProxy = false,
                ProxyType proxyType = ProxyType.None,
                string proxyValue = null,
                uint interfaceMetric = 0,
                bool disableRasCredentials = false,
                uint networkOutageTime = 1800,
                uint routeMetric = 0,
                IList<TrafficFilter> trafficFilters = null,
                IList<string> machineEKU = null,
                IList<string> proxyExcludeList = null,
                bool proxyBypassForLocal = false,
                bool disableUIDisconnectButton = false,
                bool disableUIEditButton = false,
                uint mTU = 1400
            )
        {
            if (ProfileType != ProfileType.Machine)
            {
                throw new InvalidProfileException("Instance not initialized as a Machine Profile");
            }

            ProfileName = profileName ?? throw new ArgumentNullException(nameof(profileName));
            TunnelType = tunnelType;
            ExternalAddress = externalAddress ?? throw new ArgumentNullException(nameof(externalAddress));
            OverrideXML = overrideXML;
            DNSSuffixList = dNSSuffixList ?? new List<string>();
            TrustedNetworkList = trustedNetworkList ?? new List<string>();
            RouteList = routeList ?? new Dictionary<string, string>();
            RouteExcludeList = routeExcludeList ?? new Dictionary<string, string>();
            CustomCryptography = customCryptography;
            AuthenticationTransformConstants = authenticationTransformConstants;
            CipherTransformConstants = cipherTransformConstants;
            PfsGroup = pfsGroup;
            DHGroup = dHGroup;
            IntegrityCheckMethod = integrityCheckMethod;
            EncryptionMethod = encryptionMethod;
            DomainInformationList = domainInformationList ?? new Dictionary<string, string>();
            RegisterDNS = registerDNS;
            DNSAlreadyRegistered = dnsAlreadyRegistered;
            UseProxy = useProxy;
            ProxyType = proxyType;
            ProxyValue = proxyValue;
            InterfaceMetric = interfaceMetric;
            VPNStrategy = VPNStrategy.Ikev2Only; //Device Tunnel only supports IKEv2 Only
            DisableRasCredentials = disableRasCredentials;
            NetworkOutageTime = networkOutageTime;
            RouteMetric = routeMetric;
            TrafficFilters = trafficFilters ?? new List<TrafficFilter>();
            MachineEKU = machineEKU;
            ProxyExcludeList = proxyExcludeList ?? new List<string>();
            ProxyBypassForLocal = proxyBypassForLocal;
            DisableUIDisconnectButton = disableUIDisconnectButton;
            DisableUIEditButton = disableUIEditButton;
            MTU = mTU;
        }

        /// <summary>
        /// Load all class variables from registry into the class for profile generation. Generally this method shouldn't validate settings just
        /// complete the load. It does avoid loading settings which aren't relevant to the particular profile though
        /// </summary>
        public void LoadFromRegistry()
        {
            ValidationFailures.Clear(); //Clear any existing errors as its assumed that the class is being reused
            ValidationWarnings.Clear();
            ValidationDebugMessages.Clear();

            LoadRegistryVariable(ref TunnelType, RegistrySettings.ForceTunnel);

            LoadRegistryVariable(ref ProfileName, RegistrySettings.ProfileName);
            LoadRegistryVariable(ref OverrideXML, RegistrySettings.OverrideXML);
            if (!string.IsNullOrWhiteSpace(OverrideXML))
            {
                ValidationDebugMessages.AppendLine("Override specified, ignoring load of all other registry values");
                return;
            }
            LoadRegistryVariable(ref ExternalAddress, RegistrySettings.ExternalAddress);
            LoadRegistryVariable(ref DNSSuffixList, RegistrySettings.DNSSuffixKey);
            LoadRegistryVariable(ref TrustedNetworkList, RegistrySettings.TrustedNetworksKey);
            LoadRegistryVariable(ref DomainInformationList, RegistrySettings.DomainNameInfoKey);

            if (TunnelType == TunnelType.SplitTunnel)
            {
                LoadRegistryVariable(ref RouteList, RegistrySettings.RouteListKey);
            }
            else
            {
                //initialize a default Route List in case the Setting Include Device Tunnel Routes is set
                RouteList = new Dictionary<string, string>();
            }

            LoadRegistryVariable(ref RouteExcludeList, RegistrySettings.RouteListExcludeKey);

            LoadRegistryVariable(ref RouteMetric, RegistrySettings.RouteMetric, 0); //Default to Disable Custom Route Metric

            LoadRegistryVariable(ref MTU, RegistrySettings.MTU, 1400, null); //Default to standard MTU but get it from the device settings and not from the profile offset

            if (ProfileType == ProfileType.User || ProfileType == ProfileType.UserBackup)
            {
                bool excludeO365 = false;
                LoadRegistryVariable(ref excludeO365, RegistrySettings.ExcludeOffice365, false);
                if (excludeO365)
                {
                    ConfigureO365ExcludeRoutes();
                }

                bool includeDeviceTunnelRoutes = false;
                LoadRegistryVariable(ref includeDeviceTunnelRoutes, RegistrySettings.IncludeDeviceTunnelRoutes, false);
                if (includeDeviceTunnelRoutes)
                {
                    try
                    {
                        Dictionary<string, string> deviceRouteList = AccessRegistry.ReadMachineHashtable(RegistrySettings.RouteListKey, RegistrySettings.GetProfileOffset(ProfileType.Machine));
                        if (deviceRouteList != null)
                        {
                            //Loop through the Machine Route list and include any entries which haven't already been manually added
                            foreach (KeyValuePair<string, string> element in deviceRouteList)
                            {
                                if (!RouteList.ContainsKey(element.Key))
                                {
                                    RouteList.Add(element.Key, element.Value + " (Machine Tunnel Route)");
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        ValidationWarnings.AppendLine("Unable to Load machine Tunnel Setting " + RegistrySettings.RouteListKey + " Registry: " + e.Message);
                    }

                    try
                    {
                        Dictionary<string, string> deviceRouteExcludeList = AccessRegistry.ReadMachineHashtable(RegistrySettings.RouteListExcludeKey, RegistrySettings.GetProfileOffset(ProfileType.Machine));
                        if (deviceRouteExcludeList != null)
                        {
                            //Loop through the Machine Route Exclude list and include any entries which haven't already been manually added
                            foreach (KeyValuePair<string, string> element in deviceRouteExcludeList)
                            {
                                if (!RouteExcludeList.ContainsKey(element.Key))
                                {
                                    RouteExcludeList.Add(element.Key, element.Value + " (Machine Tunnel Route)");
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        ValidationWarnings.AppendLine("Unable to Load machine Tunnel Setting " + RegistrySettings.RouteListExcludeKey + " Registry: " + e.Message);
                    }
                }
            }

            LoadRegistryVariable(ref UseProxy, RegistrySettings.EnableProxy, false);

            if (UseProxy)
            {
                LoadRegistryVariable(ref ProxyType, RegistrySettings.ProxyType);
                LoadRegistryVariable(ref ProxyValue, RegistrySettings.ProxyValue);
                LoadRegistryVariable(ref ProxyExcludeList, RegistrySettings.ProxyExcludeList);
                LoadRegistryVariable(ref ProxyBypassForLocal, RegistrySettings.ProxyBypassForLocal,false);
            }
            else
            {
                //Proxy Disabled, reset proxy settings to defaults
                ProxyType = ProxyType.None;
                ProxyValue = "";
                if (ProxyExcludeList != null && ProxyExcludeList.Count > 0)
                {
                    ProxyExcludeList.Clear();
                }
                ProxyBypassForLocal = false;
            }

            LoadRegistryVariable(ref CustomCryptography, RegistrySettings.CustomCryptography, false);

            if (CustomCryptography)
            {
                LoadRegistryVariable(ref AuthenticationTransformConstants, RegistrySettings.AuthenticationTransformConstants);
                LoadRegistryVariable(ref CipherTransformConstants, RegistrySettings.CipherTransformConstants);
                LoadRegistryVariable(ref PfsGroup, RegistrySettings.PfsGroup);
                LoadRegistryVariable(ref DHGroup, RegistrySettings.DHGroup);
                LoadRegistryVariable(ref IntegrityCheckMethod, RegistrySettings.IntegrityCheckMethod);
                LoadRegistryVariable(ref EncryptionMethod, RegistrySettings.EncryptionMethod);
            }

            if (ProfileType == ProfileType.User || ProfileType == ProfileType.UserBackup)
            {
                LoadRegistryVariable(ref RootThumbprintList, RegistrySettings.RootCertificatesKey);
                LoadRegistryVariable(ref IssuingThumbprintList, RegistrySettings.IssuingCertificatesKey);
                LoadRegistryVariable(ref NPSServerList, RegistrySettings.NPSListKey);
                LoadRegistryVariable(ref DisableNPSValidation, RegistrySettings.DisableNPSValidation, false);
                LoadRegistryVariable(ref EKUMapping, RegistrySettings.LimitEKU, false);

                if (EKUMapping)
                {
                    LoadRegistryVariable(ref EKUName, RegistrySettings.EKUName);
                    LoadRegistryVariable(ref EKUOID, RegistrySettings.EKUOID);
                }

                LoadRegistryVariable(ref DeviceComplianceEnabled, RegistrySettings.DeviceComplianceEnabled, false);

                if (DeviceComplianceEnabled)
                {
                    LoadRegistryVariable(ref DeviceComplianceEKUOID, RegistrySettings.DeviceComplianceEKUOID);
                    LoadRegistryVariable(ref DeviceComplianceIssuerHash, RegistrySettings.DeviceComplianceIssuerHash);
                }

                LoadRegistryVariable(ref EAPSmartCard, RegistrySettings.EnableEAPSmartCard, false);

                if (ProfileType == ProfileType.User)
                {
                    LoadRegistryVariable(ref DisableAlwaysOn, RegistrySettings.DisableAlwaysOn, false);
                }
                else
                {
                    DisableAlwaysOn = true; //backup Profiles must never be Always On
                }
                LoadRegistryVariable(ref DisableCryptoBinding, RegistrySettings.DisableCryptoBinding, false);

                if (TunnelType == TunnelType.SplitTunnel)
                {
                    LoadRegistryVariable(ref DNSRouteList, RegistrySettings.DNSRouteList);
                    if (DNSRouteList.Count > 0)
                    {
                        ConfigureDNSIncludeRoutes();
                    }
                }
            }

            //Load in Register DNS info for both tunnels, then check the other Tunnel to check that it isn't enabled on both tunnels
            LoadRegistryVariable(ref RegisterDNS, RegistrySettings.RegisterDNS, false);
            if (ProfileType == ProfileType.Machine)
            {
                LoadRegistryVariable(ref DNSAlreadyRegistered, RegistrySettings.RegisterDNS, false, RegistrySettings.UserTunnelOffset);
                if (!DNSAlreadyRegistered)
                {
                    //If the user tunnel isn't registered, the backup tunnel might have been so we want to detect and warn
                    //If the user tunnel is already registered we already know that we need to warn
                    LoadRegistryVariable(ref DNSAlreadyRegistered, RegistrySettings.RegisterDNS, false, RegistrySettings.UserBackupTunnelOffset);
                }
                LoadRegistryVariable(ref MachineEKU, RegistrySettings.MachineEKU);
            }
            else
            {
                LoadRegistryVariable(ref DNSAlreadyRegistered, RegistrySettings.RegisterDNS, false, RegistrySettings.MachineTunnelOffset);
            }

            LoadRegistryVariable(ref TrafficFilters);

            LoadRegistryVariable(ref DisableUIDisconnectButton, RegistrySettings.DisableUIDisconnectButton, false);
            LoadRegistryVariable(ref DisableUIEditButton, RegistrySettings.DisableUIEditButton, false);

            LoadWin32SettingsFromRegistry();
        }

        /// <summary>
        /// Starter function for the process to connect to the Microsoft 365 route list URL (https://endpoints.office.com) and get the latest Office 365 routes.
        /// This method trys to get the latest version, if it does it saves that to registry in case in the future it fails.
        /// If it fails it attempts to load the last known set from the registry.
        /// Finally it adds the list of valid routes to the existing exclude route list.
        /// </summary>
        private void ConfigureO365ExcludeRoutes()
        {
            IList<string> excludeList = new List<string>();

            //Multiple Profiles in different threads can potentially try to update the O365 list at the same time, locking avoids issues with registry settings being written at the same time
            lock (O365ExclusionLock)
            {
                try
                {
                    excludeList = GetOffice365ExcludeRoutes();

                    AccessRegistry.SaveMachineData(RegistrySettings.O365LastUpdate, DateTime.UtcNow.ToString(CultureInfo.InvariantCulture));
                    AccessRegistry.SaveMachineData(RegistrySettings.O365ExclusionKey, excludeList);
                }
                catch (Exception e)
                {
                    ValidationWarnings.AppendLine("Unable to retrieve latest Office 365 exclusion list\nError Message: " + e.Message);
                    try
                    {
                        excludeList = AccessRegistry.ReadMachineList(RegistrySettings.O365ExclusionKey, RegistrySettings.InternalStateOffset);
                        if (excludeList.Count > 0)
                        {
                            string o365CacheDate = AccessRegistry.ReadMachineString(RegistrySettings.O365LastUpdate, RegistrySettings.InternalStateOffset);
                            ValidationWarnings.AppendLine("Using cached Office 365 Exclusion list.  Cache was last updated: " + o365CacheDate + " UTC");
                        }
                        else
                        {
                            ValidationWarnings.AppendLine("Unable to retrieve cached Office 365 exclusion list.  No Office 365 exclusions will be included in profile");
                        }
                    }
                    catch (Exception e2)
                    {
                        ValidationWarnings.AppendLine("Unable to retrieve cached Office 365 exclusion list\n Error Message: " + e2.Message);
                        ValidationWarnings.AppendLine("No Office 365 exclusions will be included in profile");
                    }
                }
            }

            foreach (string route in excludeList)
            {
                if (!RouteExcludeList.ContainsKey(route))
                {
                    RouteExcludeList.Add(route, "O365 Route");
                }
            }
        }

        private void ConfigureDNSIncludeRoutes()
        {
            Dictionary<string, string> includeList;
            string cacheOffset = RegistrySettings.InternalStateOffset + "\\" + ProfileType;

            try
            {
                includeList = GetDNSIncludeRoutes(DNSRouteList);

                AccessRegistry.SaveMachineData(RegistrySettings.DNSLastUpdate, DateTime.UtcNow.ToString(CultureInfo.InvariantCulture), cacheOffset);
                AccessRegistry.SaveMachineData(RegistrySettings.DNSRouteListKey, includeList, cacheOffset);
            }
            catch (Exception e)
            {
                //TODO: Handle individual failures
                ValidationWarnings.AppendLine("Unable to retrieve latest DNS Route Lookups\nError Message: " + e.Message);
                includeList = AccessRegistry.ReadMachineHashtable(RegistrySettings.DNSRouteListKey, cacheOffset);
                if (includeList.Count > 0)
                {
                    string DNSCacheDate = AccessRegistry.ReadMachineString(RegistrySettings.DNSLastUpdate, cacheOffset);
                    ValidationWarnings.AppendLine("Using cached DNS Route list.  Cache was last updated: " + DNSCacheDate + " UTC");
                }
                else
                {
                    ValidationWarnings.AppendLine("Unable to retrieve cached DNS Route list.  No DNS routes will be included in profile");
                }
            }

            foreach (KeyValuePair<string, string> route in includeList)
            {
                if (!RouteList.ContainsKey(route.Key))
                {
                    RouteList.Add(route.Key, route.Value);
                }
            }
        }

        public void LoadWin32SettingsFromRegistry()
        {
            LoadRegistryVariable(ref VPNStrategy, RegistrySettings.VPNStrategy, (int)VPNStrategy.Ikev2Only); //Default to IKEv2 Only Tunnel

            LoadRegistryVariable(ref InterfaceMetric, RegistrySettings.InterfaceMetric, 0); //Default to Disable Custom Interface Metric

            LoadRegistryVariable(ref DisableRasCredentials, RegistrySettings.DisableRasCredentials, false); //Default to enabling Ras Credentials

            LoadRegistryVariable(ref NetworkOutageTime, RegistrySettings.NetworkOutageTime, 1800); //Default to 30 Minutes
        }

        //Handle all Enum converstions as c# will automatically convert from int to the required Enum
        private void LoadRegistryVariable<T>(ref T var, string registryValue, uint defaultValue = 0) where T: Enum
        {
            try
            {
                var = (T)Enum.ToObject(typeof(T), AccessRegistry.ReadMachineUInt32(registryValue, defaultValue, TunnelRegOffset));
            }
            catch (Exception e)
            {
                ValidationFailures.AppendLine("Unable to Load " + registryValue + " Registry: " + e.Message);
            }
        }

        private void LoadRegistryVariable(ref string var, string registryValue)
        {
            var = null;
            try
            {
                var = AccessRegistry.ReadMachineString(registryValue, TunnelRegOffset);
            }
            catch (Exception e)
            {
                ValidationFailures.AppendLine("Unable to Load " + registryValue + " Registry: " + e.Message);
            }
        }

        private void LoadRegistryVariable(ref uint var, string registryValue, uint defaultValue)
        {
            try
            {
                LoadRegistryVariable(ref var, registryValue, defaultValue, TunnelRegOffset);
            }
            catch (Exception e)
            {
                ValidationFailures.AppendLine("Unable to Load " + registryValue + " Registry: " + e.Message);
            }
        }

        private void LoadRegistryVariable(ref uint var, string registryValue, uint defaultValue, string offset)
        {
            try
            {
                var = AccessRegistry.ReadMachineUInt32(registryValue, defaultValue, offset);
            }
            catch (Exception e)
            {
                ValidationFailures.AppendLine("Unable to Load " + registryValue + " Registry: " + e.Message);
            }
        }

        private void LoadRegistryVariable(ref IList<string> var, string registryValue)
        {
            var = null;
            try
            {
                var = AccessRegistry.ReadMachineList(registryValue, TunnelRegOffset);
            }
            catch (Exception e)
            {
                ValidationFailures.AppendLine("Unable to Load " + registryValue + " Registry: " + e.Message);
            }
        }

        private void LoadRegistryVariable(ref Dictionary<string, string> var, string registryValue)
        {
            var = null;
            try
            {
                var = AccessRegistry.ReadMachineHashtable(registryValue, TunnelRegOffset);
            }
            catch (Exception e)
            {
                ValidationFailures.AppendLine("Unable to Load " + registryValue + " Registry: " + e.Message);
            }
        }

        private void LoadRegistryVariable(ref bool var, string registryValue, bool defaultValue, string offset)
        {
            var = defaultValue;
            try
            {
                var = AccessRegistry.ReadMachineBoolean(registryValue, defaultValue, offset);
            }
            catch (Exception e)
            {
                ValidationFailures.AppendLine("Unable to Load " + registryValue + " Registry: " + e.Message);
            }
        }

        private void LoadRegistryVariable(ref bool var, string registryValue, bool defaultValue)
        {
            var = defaultValue;
            try
            {
                var = AccessRegistry.ReadMachineBoolean(registryValue, defaultValue, TunnelRegOffset);
            }
            catch (Exception e)
            {
                ValidationFailures.AppendLine("Unable to Load " + registryValue + " Registry: " + e.Message);
            }
        }

        private void LoadRegistryVariable(ref IList<TrafficFilter> var)
        {
            var = new List<TrafficFilter>();
            string filterRootOffset = TunnelRegOffset + "\\" + RegistrySettings.TrafficFilters;
            try
            {
                IList<string> TrafficFilterList = AccessRegistry.ReadMachineSubkeys(null, filterRootOffset);
                foreach(string filterName in TrafficFilterList)
                {
                    try
                    {
                        string filterOffset = filterRootOffset + "/" + filterName;
                        //Only add the entry if Enabled = 1/true
                        if (AccessRegistry.ReadMachineBoolean(RegistrySettings.TrafficFilterEnabled,false, filterOffset))
                        {
                            var.Add(new TrafficFilter(filterName)
                            {
                                AppId = AccessRegistry.ReadMachineString(RegistrySettings.TrafficFilterAppId, filterOffset),
                                Direction = (ProtocolDirection)Enum.ToObject(typeof(ProtocolDirection), AccessRegistry.ReadMachineUInt32(RegistrySettings.TrafficFilterDirection, 0, filterOffset)),
                                RoutingPolicyType = (TunnelType)Enum.ToObject(typeof(TunnelType), AccessRegistry.ReadMachineUInt32(RegistrySettings.TrafficFilterRoutingPolicyType, 0, filterOffset)),
                                Protocol = (Protocol)Enum.ToObject(typeof(Protocol), AccessRegistry.ReadMachineInt32(RegistrySettings.TrafficFilterProtocol, -1, filterOffset)),
                                LocalAddresses = AccessRegistry.ReadMachineString(RegistrySettings.TrafficFilterLocalAddresses, filterOffset),
                                RemoteAddresses = AccessRegistry.ReadMachineString(RegistrySettings.TrafficFilterRemoteAddresses, filterOffset),
                                LocalPorts = AccessRegistry.ReadMachineString(RegistrySettings.TrafficFilterLocalPorts, filterOffset),
                                RemotePorts = AccessRegistry.ReadMachineString(RegistrySettings.TrafficFilterRemotePorts, filterOffset)
                            });
                        }
                        else
                        {
                            ValidationWarnings.AppendLine("Traffic Filter: " + filterName + " was ignored as Enabled was not True");
                        }
                    }
                    catch (Exception e)
                    {
                        ValidationFailures.AppendLine("Unable to Load " + filterName + " Registry Settings: " + e.Message);
                    }
                }
            }
            catch (Exception e)
            {
                ValidationFailures.AppendLine("Unable to Load TrafficFilters List: " + e.Message);
            }
        }

        public void Generate()
        {
            ValidateParameters();

            ProfileString.Clear();

            if (!string.IsNullOrWhiteSpace(OverrideXML))
            {
                ProfileString.Append(OverrideXML);
                //Call Validation but then convert all failures to warning to avoid the profile being discarded
                ValidateProfile();
                if (ValidationFailures.Length > 0)
                {
                    ValidationWarnings.AppendLine("Suppressing Validation Errors: ");
                    ValidationWarnings.Append(ValidationFailures.ToString());
                    ValidationFailures.Clear();
                }
                return;
            }

            XmlWriterSettings settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = " ",
                WriteEndDocumentOnClose = true,
                OmitXmlDeclaration = true,
                Encoding = Encoding.ASCII,
                Async = true
            };

            using (XmlWriter writer = XmlWriter.Create(ProfileString, settings))
            {
                writer.WriteStartElement("VPNProfile");
                //EdpModeId not configurable as WIP is not widely used

                //Remember credentials should never be set unless using CHAP https://directaccess.richardhicks.com/2019/04/17/always-on-vpn-updates-to-improve-connection-reliability/#comment-30640

                writer.WriteElementString("AlwaysOn", (!DisableAlwaysOn).ToString().ToLowerInvariant()); //Enable unless explicitly disabled

                if (DNSSuffixList != null && DNSSuffixList.Count > 0)
                {
                    //Turn the DNS List in to a comma separated string
                    writer.WriteElementString("DnsSuffix", string.Join(",", DNSSuffixList));
                }
                if (TrustedNetworkList != null && TrustedNetworkList.Count > 0)
                {
                    //Turn the DNS List in to a comma separated string
                    writer.WriteElementString("TrustedNetworkDetection", string.Join(",", TrustedNetworkList));
                }
                //Lockdown mode is not configurable as it is not widely used

                if (DisableUIEditButton)
                {
                    writer.WriteElementString("DisableAdvancedOptionsEditButton", DisableUIEditButton.ToString().ToLowerInvariant());
                }

                if (DisableUIDisconnectButton)
                {
                    writer.WriteElementString("DisableDisconnectButton", DisableUIDisconnectButton.ToString().ToLowerInvariant());
                }

                if (ProfileType == ProfileType.Machine)
                {
                    writer.WriteElementString("DeviceTunnel", "true");  //Specify that the profile is for a device tunnel
                }
                writer.WriteElementString("RegisterDNS", RegisterDNS.ToString().ToLowerInvariant()); //Choose to register the device IP in DNS
                //BypassForLocal is currently considered reserved for future use

                //Proxy configuration
                if (UseProxy)
                {
                    switch (ProxyType)
                    {
                        case ProxyType.None:
                            break;
                        case ProxyType.PAC:
                            writer.WriteStartElement("Proxy");
                            writer.WriteElementString("AutoConfigUrl", ProxyValue);
                            writer.WriteEndElement();
                            break;
                        case ProxyType.Manual:
                            writer.WriteStartElement("Proxy");
                            writer.WriteStartElement("Manual");
                            writer.WriteElementString("Server", ProxyValue);
                            writer.WriteEndElement();
                            writer.WriteEndElement();
                            break;
                        default:
                            ValidationWarnings.AppendLine("ProxyType not known about, please update DPC");
                            break;
                    }
                }

                //APNBinding is not typically deployed

                if (DeviceComplianceEnabled)
                {
                    writer.WriteStartElement("DeviceCompliance");
                    writer.WriteElementString("Enabled", "true");
                    writer.WriteStartElement("Sso");
                    writer.WriteElementString("Enabled", "true");
                    if (!string.IsNullOrWhiteSpace(DeviceComplianceEKUOID))
                    {
                        writer.WriteElementString("Eku", DeviceComplianceEKUOID);
                    }
                    writer.WriteElementString("IssuerHash", DeviceComplianceIssuerHash);
                    writer.WriteEndElement(); //</Sso>
                    writer.WriteEndElement(); //</DeviceCompliance>
                }

                //PluginProfile is not typically deployed

                //AppTrigger is not typically deployed

                //Domain Name Information
                if (DomainInformationList != null && DomainInformationList.Count > 0)
                {
                    foreach (KeyValuePair<string, string> domainInfo in DomainInformationList)
                    {
                        writer.WriteStartElement("DomainNameInformation");
                        writer.WriteElementString("DomainName", domainInfo.Key);
                        if (!string.IsNullOrWhiteSpace(domainInfo.Value))
                        {
                            //Excluded Domain Name information should not have a DNS Servers Block https://directaccess.richardhicks.com/2018/04/23/always-on-vpn-and-the-name-resolution-policy-table-nrpt/
                            writer.WriteElementString("DnsServers", domainInfo.Value.Replace(" ","")); //Remove all spaces from the IP address list as this causes DomainNameInfo to not be accepted correctly
                        }
                        writer.WriteEndElement();
                    }
                }

                //Traffic Filter
                if (TrafficFilters != null && TrafficFilters.Count > 0)
                {
                    foreach (TrafficFilter filter in TrafficFilters)
                    {
                        if (!string.IsNullOrWhiteSpace(filter.RuleName))
                        {
                            writer.WriteComment(filter.RuleName);
                        }
                        writer.WriteStartElement("TrafficFilter");

                        if (!string.IsNullOrWhiteSpace(filter.AppId))
                        {
                            writer.WriteStartElement("App");
                            writer.WriteElementString("Id", filter.AppId);
                            writer.WriteEndElement();
                        }

                        //If no protocol is specified ommit from profile to avoid filtering
                        if (filter.Protocol != Protocol.ANY) writer.WriteElementString("Protocol", ((int)filter.Protocol).ToString(CultureInfo.InvariantCulture));
                        if (!string.IsNullOrWhiteSpace(filter.LocalPorts)) writer.WriteElementString("LocalPortRanges", filter.LocalPorts);
                        if (!string.IsNullOrWhiteSpace(filter.RemotePorts)) writer.WriteElementString("RemotePortRanges", filter.RemotePorts);
                        if (!string.IsNullOrWhiteSpace(filter.LocalAddresses)) writer.WriteElementString("LocalAddressRanges", filter.LocalAddresses);
                        if (!string.IsNullOrWhiteSpace(filter.RemoteAddresses)) writer.WriteElementString("RemoteAddressRanges", filter.RemoteAddresses);

                        if (filter.RoutingPolicyType == TunnelType.ForceTunnel && !string.IsNullOrWhiteSpace(filter.AppId))
                        {
                            writer.WriteElementString("RoutingPolicyType", "ForceTunnel");
                        }
                        else if (filter.RoutingPolicyType == TunnelType.SplitTunnel && !string.IsNullOrWhiteSpace(filter.AppId))
                        {
                            writer.WriteElementString("RoutingPolicyType", "SplitTunnel");
                        }

                        if (DeviceInfo.GetOSVersion().IsGreaterThanWin10_2004)
                        {
                            //This feature was only introduced in Windows 10 2004
                            if (filter.Direction == ProtocolDirection.Inbound)
                            {
                                writer.WriteElementString("Direction", "Inbound");
                            }
                            else
                            {
                                writer.WriteElementString("Direction", "Outbound");
                            }
                        }
                        writer.WriteEndElement();
                    }
                }

                //Native Profile
                writer.WriteStartElement("NativeProfile");
                writer.WriteElementString("Servers", ExternalAddress);
                writer.WriteElementString("RoutingPolicyType", TunnelType.ToString());
                //Always deploy the initial tunnel as IKEv2 so that the custom cryptography settings are always read https://directaccess.richardhicks.com/2019/01/07/always-on-vpn-ikev2-connection-failure-error-code-800/
                writer.WriteElementString("NativeProtocolType", "IKEv2");

                //L2tpPsk is not used
                if (TunnelType == TunnelType.SplitTunnel)
                {
                    writer.WriteElementString("DisableClassBasedDefaultRoute", "true");
                }
                //CryptographySuite
                if (CustomCryptography)
                {
                    writer.WriteStartElement("CryptographySuite");
                    if (!string.IsNullOrWhiteSpace(AuthenticationTransformConstants)) writer.WriteElementString("AuthenticationTransformConstants", AuthenticationTransformConstants);
                    if (!string.IsNullOrWhiteSpace(CipherTransformConstants)) writer.WriteElementString("CipherTransformConstants", CipherTransformConstants);
                    if (!string.IsNullOrWhiteSpace(PfsGroup)) writer.WriteElementString("PfsGroup", PfsGroup);
                    if (!string.IsNullOrWhiteSpace(DHGroup)) writer.WriteElementString("DHGroup", DHGroup);
                    if (!string.IsNullOrWhiteSpace(IntegrityCheckMethod)) writer.WriteElementString("IntegrityCheckMethod", IntegrityCheckMethod);
                    if (!string.IsNullOrWhiteSpace(EncryptionMethod)) writer.WriteElementString("EncryptionMethod", EncryptionMethod);
                    writer.WriteEndElement();
                }
                writer.WriteStartElement("Authentication");
                if (ProfileType == ProfileType.User || ProfileType == ProfileType.UserBackup)
                {
                    writer.WriteElementString("UserMethod", "Eap");
                    writer.WriteStartElement("Eap");
                    writer.WriteStartElement("Configuration");
                    writer.WriteStartElement("EapHostConfig", "http://www.microsoft.com/provisioning/EapHostConfig");
                    writer.WriteStartElement("EapMethod");
                    writer.WriteElementString("Type", "http://www.microsoft.com/provisioning/EapCommon", "25");
                    writer.WriteElementString("VendorId", "http://www.microsoft.com/provisioning/EapCommon", "0");
                    writer.WriteElementString("VendorType", "http://www.microsoft.com/provisioning/EapCommon", "0");
                    writer.WriteElementString("AuthorId", "http://www.microsoft.com/provisioning/EapCommon", "0");
                    writer.WriteEndElement();
                    writer.WriteStartElement("Config");
                    writer.WriteAttributeString("xmlns", "http://www.microsoft.com/provisioning/EapHostConfig");
                    writer.WriteStartElement("Eap", "http://www.microsoft.com/provisioning/BaseEapConnectionPropertiesV1");
                    writer.WriteElementString("Type", "25");
                    writer.WriteStartElement("EapType", "http://www.microsoft.com/provisioning/MsPeapConnectionPropertiesV1");
                    writer.WriteStartElement("ServerValidation");
                    writer.WriteElementString("DisableUserPromptForServerValidation", "true");
                    if (!DisableNPSValidation)
                    {
                        writer.WriteElementString("ServerNames", string.Join(";", NPSServerList));
                    }

                    foreach (string thumbprint in RootThumbprintList)
                    {
                        writer.WriteElementString("TrustedRootCA", FormatThumbprint(thumbprint));
                    }
                    writer.WriteEndElement();
                    writer.WriteElementString("InnerEapOptional", "false");
                    writer.WriteStartElement("Eap", "http://www.microsoft.com/provisioning/BaseEapConnectionPropertiesV1");
                    writer.WriteElementString("Type", "13");
                    writer.WriteStartElement("EapType", "http://www.microsoft.com/provisioning/EapTlsConnectionPropertiesV1");
                    writer.WriteStartElement("CredentialsSource");
                    if (EAPSmartCard)
                    {
                        writer.WriteStartElement("SmartCard");
                        writer.WriteEndElement();
                    }
                    else
                    {
                        writer.WriteStartElement("CertificateStore");
                        writer.WriteElementString("SimpleCertSelection", "true");
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                    writer.WriteStartElement("ServerValidation");
                    writer.WriteElementString("DisableUserPromptForServerValidation", "true");
                    if (!DisableNPSValidation)
                    {
                        writer.WriteElementString("ServerNames", string.Join(";", NPSServerList));
                    }
                    foreach (string thumbprint in RootThumbprintList)
                    {
                        writer.WriteElementString("TrustedRootCA", FormatThumbprint(thumbprint));
                    }
                    writer.WriteEndElement();
                    writer.WriteElementString("DifferentUsername", "false");
                    writer.WriteElementString("PerformServerValidation", "http://www.microsoft.com/provisioning/EapTlsConnectionPropertiesV2", "true");
                    writer.WriteElementString("AcceptServerName", "http://www.microsoft.com/provisioning/EapTlsConnectionPropertiesV2", (!DisableNPSValidation).ToString().ToLowerInvariant());
                    writer.WriteStartElement("TLSExtensions", "http://www.microsoft.com/provisioning/EapTlsConnectionPropertiesV2");
                    writer.WriteStartElement("FilteringInfo", "http://www.microsoft.com/provisioning/EapTlsConnectionPropertiesV3");
                    writer.WriteStartElement("CAHashList");
                    writer.WriteAttributeString("Enabled", (!DeviceComplianceEnabled).ToString().ToLowerInvariant()); //Disable if Device Compliance is used as the certificate issuer will be different
                    foreach (string thumbprint in IssuingThumbprintList)
                    {
                        writer.WriteElementString("IssuerHash", FormatThumbprint(thumbprint));
                    }
                    writer.WriteEndElement(); //</CAHashList>

                    if (EKUMapping || DeviceComplianceEnabled)
                    {
                        writer.WriteStartElement("EKUMapping");
                        writer.WriteStartElement("EKUMap");
                        writer.WriteElementString("EKUName", EKUName);
                        writer.WriteElementString("EKUOID", EKUOID);
                        writer.WriteEndElement(); //</EKUMap>
                        writer.WriteEndElement(); //</EKUMapping>
                    }

                    if (EKUMapping || EAPSmartCard || DeviceComplianceEnabled)
                    {
                        writer.WriteStartElement("ClientAuthEKUList");
                        writer.WriteAttributeString("Enabled", "true");

                        if (EKUMapping || DeviceComplianceEnabled)
                        {
                            writer.WriteStartElement("EKUMapInList");
                            writer.WriteElementString("EKUName", EKUName);
                            writer.WriteEndElement(); //</EKUMapInList>
                        }
                        writer.WriteEndElement(); //</ClientAuthEKUList>
                    }

                    writer.WriteEndElement(); //</FilteringInfo>

                    if (EAPSmartCard)
                    {
                        writer.WriteElementString("GroupSmartCardCerts", "true");
                    }

                    writer.WriteEndElement(); //</TLSExtensions>

                    writer.WriteEndElement(); //</EapType>
                    writer.WriteEndElement(); //</Eap>
                    writer.WriteElementString("EnableQuarantineChecks", "false");
                    writer.WriteElementString("RequireCryptoBinding", (!DisableCryptoBinding).ToString().ToLowerInvariant()); //Enable unless explicitly disabled
                    writer.WriteStartElement("PeapExtensions");
                    writer.WriteElementString("PerformServerValidation", "http://www.microsoft.com/provisioning/MsPeapConnectionPropertiesV2", "true");
                    writer.WriteElementString("AcceptServerName", "http://www.microsoft.com/provisioning/MsPeapConnectionPropertiesV2", (!DisableNPSValidation).ToString().ToLowerInvariant());
                    writer.WriteEndElement(); //</PeapExtensions>
                    writer.WriteEndElement(); //</EapType>
                    writer.WriteEndElement(); //</Eap>
                    writer.WriteEndElement(); //</Config>
                    writer.WriteEndElement(); //</EapHostConfig>
                    writer.WriteEndElement(); //</Configuration>
                    writer.WriteEndElement(); //</Eap>
                }
                else
                {
                    writer.WriteElementString("MachineMethod", "Certificate");
                }
                writer.WriteEndElement(); //</ Authentication >
                writer.WriteEndElement(); //</NativeProfile>

                //Route
                foreach (KeyValuePair<string,string> Route in RouteList)
                {
                    if (!string.IsNullOrWhiteSpace(Route.Value))
                    {
                        writer.WriteComment(Route.Value);
                    }
                    writer.WriteStartElement("Route");
                    writer.WriteElementString("Address", IPUtils.GetIPAddress(Route.Key));
                    writer.WriteElementString("PrefixSize", IPUtils.GetIPCIDRSuffix(Route.Key).ToString(CultureInfo.InvariantCulture));
                    if (RouteMetric > 0)
                    {
                        writer.WriteElementString("Metric", RouteMetric.ToString(CultureInfo.InvariantCulture));
                    }
                    writer.WriteEndElement();
                }

                foreach (KeyValuePair<string, string> Route in RouteExcludeList)
                {
                    if (!string.IsNullOrWhiteSpace(Route.Value))
                    {
                        writer.WriteComment(Route.Value);
                    }
                    writer.WriteStartElement("Route");
                    writer.WriteElementString("Address", IPUtils.GetIPAddress(Route.Key));
                    writer.WriteElementString("PrefixSize", IPUtils.GetIPCIDRSuffix(Route.Key).ToString(CultureInfo.InvariantCulture));
                    writer.WriteElementString("ExclusionRoute", "true");
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
            }

            ValidateProfile();
        }

        public bool ValidateFailed()
        {
            return ValidationFailures.Length != 0;
        }

        public bool ValidateWarnings()
        {
            return ValidationWarnings.Length != 0;
        }

        public string GetProfileName()
        {
            return ProfileName;
        }

        public string GetValidationFailures()
        {
            if (string.IsNullOrWhiteSpace(ValidationFailures.ToString()))
            {
                return "";
            }
            else
            {
                return ValidationFailures.Insert(0, "    - ").ToString().TrimEnd('\n').Replace("\n", "\n    - ");
            }
        }

        public string GetValidationWarnings()
        {
            if (string.IsNullOrWhiteSpace(ValidationWarnings.ToString()))
            {
                return "";
            }
            else
            {
                return ValidationWarnings.Insert(0, "    - ").ToString().TrimEnd('\n').Replace("\n", "\n    - ");
            }
        }

        public string GetProfile() => ProfileString.ToString();

        public ManagedProfile GetProfileUpdate()
        {
            return new ManagedProfile
            {
                ProfileName = ProfileName,
                ProfileType = ProfileType,
                ProfileXML = GetProfile(),
                VPNStrategy = VPNStrategy,
                Metric = InterfaceMetric,
                DisableRasCredentials = DisableRasCredentials,
                NetworkOutageTime = NetworkOutageTime,
                MachineEKU = MachineEKU,
                ProxyExcludeList = ProxyExcludeList,
                ProxyBypassForLocal = ProxyBypassForLocal,
                MTU = MTU
            };
        }

        public string SaveProfile(string savePath)
        {
            return SaveProfile(ProfileName, GetProfile(), savePath);
        }

        private void ValidateParameters()
        {
            //Core Params
            //Profile Name
            if (string.IsNullOrWhiteSpace(ProfileName))
            {
                ValidationFailures.AppendLine("Profile must have a valid name");
            }

            if (!Validate.ProfileName(ProfileName))
            {
                ValidationFailures.AppendLine(ProfileName + " is not considered valid profile name");
            }

            if (!string.IsNullOrWhiteSpace(OverrideXML))
            {
                //Skip parameter validation if override is enabled
                ValidationDebugMessages.AppendLine("Override specified, ignoring checks on all other registry values");
                return;
            }

            //External URL
            if (string.IsNullOrWhiteSpace(ExternalAddress))
            {
                ValidationFailures.AppendLine("Profile must have a valid external URL");
            }

            if (!Validate.ValidateConnectionURL(ExternalAddress))
            {
                ValidationFailures.AppendLine(ExternalAddress + " is not considered valid external URL");
            }

            //Optional but shared Params
            if (TunnelType == TunnelType.SplitTunnel && RouteList.Count <= 0)
            {
                ValidationWarnings.AppendLine("Split Tunnel Configurations require RouteList Entries, Defaulting to standard RFC1918 Address Ranges");
                RouteList.Add("10.0.0.0/8", "DPC Default Route");
                RouteList.Add("172.16.0.0/12", "DPC Default Route");
                RouteList.Add("192.168.0.0/16", "DPC Default Route");
            }

            DNSSuffixList = ValidateList(DNSSuffixList, Validate.ValidateFQDN);
            TrustedNetworkList = ValidateList(TrustedNetworkList, Validate.ValidateFQDN);
            RouteList = ValidateDictionary(RouteList, Validate.IPv4OrIPv6OrCIDR, Validate.Comment);
            RouteExcludeList = ValidateDictionary(RouteExcludeList, Validate.IPv4OrIPv6OrCIDR, Validate.Comment);
            DomainInformationList = ValidateDictionary(DomainInformationList, Validate.ValidateFQDN, Validate.IPAddressCommaList);

            if (InterfaceMetric > 9999)
            {
                ValidationWarnings.AppendLine("Metric must be below 10000, setting to 9999");
                InterfaceMetric = 9999;
            }

            if (MTU < 576)
            {
                ValidationWarnings.AppendLine("MTU must be at least 576, defaulting to 1400");
                MTU = 1400;
            }

            if (MTU > 1400)
            {
                ValidationWarnings.AppendLine("MTU must be at 1400 or less, defaulting to 1400");
                MTU = 1400;
            }

            if (TrustedNetworkList != null && TrustedNetworkList.Count() < 1 && DomainInformationList != null && DomainInformationList.Count > 0)
            {
                //When there is no Trusted Network List and there is a Domain Information list Windows will automatically generate one, when using . to force all DNS traffic
                //Internally this may cause issues so warn so that admins can modify behavior without needing to debug or blame the tool
                ValidationWarnings.AppendLine("DomainNameInformation included but no Trusted Network Detection has been defined.  This may cause unexpected behavior");
            }

            if (CustomCryptography && (VPNStrategy == VPNStrategy.GREOnly || VPNStrategy == VPNStrategy.PptpOnly || VPNStrategy == VPNStrategy.ProtocolList || VPNStrategy == VPNStrategy.SstpOnly))
            {
                ValidationWarnings.AppendLine("VPN Strategy selected which can't handle custom cryptography, disabling custom cryptography");
                CustomCryptography = false;
            }

            if (!DeviceInfo.GetOSVersion().IsGreaterThanWin10_2004)
            {
                IList<TrafficFilter> invalidFilters = TrafficFilters.Where(f => f.Direction != ProtocolDirection.Outbound).ToList();
                if (invalidFilters.Count > 0)
                {
                    //Remove invalid filters from profile deployment
                    TrafficFilters = TrafficFilters.Where(f => f.Direction == ProtocolDirection.Outbound).ToList();
                    foreach (TrafficFilter filter in invalidFilters)
                    {
                        ValidationWarnings.AppendLine("Device detected as below Windows 10 2004 which does not support Inbound Traffic Filters, Ignoring Rule " + filter.RuleName);
                    }
                }
            }

            foreach (TrafficFilter filter in TrafficFilters)
            {
                if (!Validate.PackageId(filter.AppId))
                {
                    ValidationWarnings.AppendLine(filter.RuleName + " has a invalid PackageId value of " + filter.AppId);
                    filter.Invalid = true;
                }
                if (!Validate.PortList(filter.LocalPorts))
                {
                    ValidationWarnings.AppendLine(filter.RuleName + " has a invalid LocalPort value of " + filter.LocalPorts);
                    filter.Invalid = true;
                }
                if (!Validate.PortList(filter.RemotePorts))
                {
                    ValidationWarnings.AppendLine(filter.RuleName + " has a invalid RemotePorts value of " + filter.RemotePorts);
                    filter.Invalid = true;
                }
                if (!Validate.IPv4List(filter.LocalAddresses))
                {
                    ValidationWarnings.AppendLine(filter.RuleName + " has a invalid LocalAddresses value of " + filter.LocalAddresses);
                    filter.Invalid = true;
                }
                if (!Validate.IPv4List(filter.RemoteAddresses))
                {
                    ValidationWarnings.AppendLine(filter.RuleName + " has a invalid RemoteAddresses value of " + filter.RemoteAddresses);
                    filter.Invalid = true;
                }

                if (filter.RoutingPolicyType != TunnelType.SplitTunnel && filter.RoutingPolicyType != TunnelType.ForceTunnel)
                {
                    ValidationWarnings.AppendLine(filter.RuleName + " has a invalid RoutingPolicyType value of " + filter.RoutingPolicyType);
                }

                if (filter.Direction == ProtocolDirection.Inbound && !DeviceInfo.GetOSVersion().IsGreaterThanWin10_2004)
                {
                    ValidationWarnings.AppendLine(filter.RuleName + " is an inbound rule which is not supported on systems older than Windows 10 2004");
                    filter.Invalid = true;
                }

                if (!string.IsNullOrWhiteSpace(filter.LocalPorts) && filter.Protocol != Protocol.TCP && filter.Protocol != Protocol.UDP)
                {
                    ValidationWarnings.AppendLine(filter.RuleName + " is invalid as Local Ports are only valid when the protocol is TCP or UDP");
                    filter.Invalid = true;
                }

                if (!string.IsNullOrWhiteSpace(filter.RemotePorts) && filter.Protocol != Protocol.TCP && filter.Protocol != Protocol.UDP)
                {
                    ValidationWarnings.AppendLine(filter.RuleName + " is invalid as Remote Ports are only valid when the protocol is TCP or UDP");
                    filter.Invalid = true;
                }

                if (!string.IsNullOrWhiteSpace(filter.RemoteAddresses) && !string.IsNullOrWhiteSpace(filter.LocalAddresses))
                {
                    ValidationWarnings.AppendLine(filter.RuleName + " is invalid as Local Addresses are ignored when Remote Addresses are enabled");
                    filter.Invalid = true;
                }

                if (filter.IsDefault())
                {
                    ValidationWarnings.AppendLine(filter.RuleName + " does not have any configuration");
                    filter.Invalid = true;
                }
            }

            //Remove any invalid filters by selecting only valid filters
            TrafficFilters = TrafficFilters.Where(f => f.Invalid == false).ToList();

            if (ProfileType == ProfileType.User || ProfileType == ProfileType.UserBackup)
            {
                //Essential User Params
                NPSServerList = ValidateList(NPSServerList, Validate.ValidateConnectionURL);
                RootThumbprintList = ValidateThumbprint(RootThumbprintList);
                IssuingThumbprintList = ValidateThumbprint(IssuingThumbprintList);

                if (RootThumbprintList.Count < 1)
                {
                    ValidationFailures.AppendLine("User templates must have at least 1 Root CA Thumbprint");
                }

                if (IssuingThumbprintList.Count < 1)
                {
                    ValidationFailures.AppendLine("User templates must have at least 1 Issuing CA Thumbprint");
                }

                if (NPSServerList.Count < 1 && !DisableNPSValidation)
                {
                    ValidationFailures.AppendLine("User templates must have at least 1 NPS Server");
                }

                if (DisableNPSValidation)
                {
                    ValidationWarnings.AppendLine("NPS Validation Disabled. This setting is not recommended and should be disabled as soon as NPS Validation issues have been resolved");
                }

                if (TunnelType != TunnelType.ForceTunnel && UseProxy)
                {
                    ValidationWarnings.AppendLine("Proxy Settings are only used when the tunnel is configured for Force Tunnel, Ignoring Proxy Settings");
                    UseProxy = false;
                    //Reset proxy settings in case they try to be used later
                    ProxyExcludeList.Clear();
                    ProxyBypassForLocal = false;
                }

                if (UseProxy && ProxyType != ProxyType.Manual)
                {
                    if (ProxyExcludeList.Count > 0)
                    {
                        //ADMX has a limitation where something has to be in the ProxyExcludeList to stop it erroring so if this magic value is entered just ignore the contents of the exclude list
                        if (ProxyExcludeList.First().ToLower(CultureInfo.InvariantCulture) != "pacfile")
                        {
                            ValidationWarnings.AppendLine("Proxy Exclusions can only be enabled with a manually configured Proxy, Ignoring exclusions");
                        }
                        ProxyExcludeList.Clear();
                    }
                    if (ProxyBypassForLocal)
                    {
                        ValidationWarnings.AppendLine("Bypass for Local can only be enabled with a manually configured Proxy, Ignoring setting");
                        ProxyBypassForLocal = false;
                    }
                }

                //Optional User Params
                if (EKUMapping)
                {
                    if (string.IsNullOrWhiteSpace(EKUName))
                    {
                        ValidationFailures.AppendLine(RegistrySettings.EKUName + " must not be null when EKUMapping is enabled");
                        EKUMapping = false;
                    }

                    if (!Validate.OID(EKUOID))
                    {
                        ValidationFailures.AppendLine(EKUOID + " must be a valid OID");
                        EKUMapping = false;
                    }
                }

                if (DeviceComplianceEnabled)
                {
                    //Check if Device Compliance should still be enabled
                    if (string.IsNullOrWhiteSpace(DeviceComplianceIssuerHash))
                    {
                        ValidationFailures.AppendLine(RegistrySettings.DeviceComplianceIssuerHash + " must not be null when Device Compliance is enabled");
                        DeviceComplianceEnabled = false;
                    }

                    if (!string.IsNullOrWhiteSpace(DeviceComplianceEKUOID) && !Validate.OID(DeviceComplianceEKUOID))
                    {
                        ValidationFailures.AppendLine(DeviceComplianceEKUOID + " must be a valid OID");
                        DeviceComplianceEnabled = false; //While not mandatory to set this value, if the value is set it should be a valid one
                    }

                    DeviceComplianceIssuerHash = Validate.Thumbprint(DeviceComplianceIssuerHash.ToLowerInvariant());
                    if (DeviceComplianceIssuerHash == null)
                    {
                        ValidationFailures.AppendLine("Failed to validate Device Compliance Issuer Hash: " + DeviceComplianceIssuerHash);
                        DeviceComplianceEnabled = false;
                    }
                }

                if (DeviceComplianceEnabled)
                {
                    //Device Compliance always uses a certificate which uses these values
                    EKUName = "AAD Conditional Access";
                    EKUOID = "1.3.6.1.4.1.311.87";
                }

                if (DeviceComplianceEnabled && EKUMapping)
                {
                    ValidationWarnings.AppendLine("EKU Settings conflict with Device Compliance. EKU Settings will be ignored");
                    EKUMapping = false;
                }

                if (VPNStrategy == VPNStrategy.SstpOnly && CustomCryptography)
                {
                    ValidationWarnings.AppendLine("SSTP does not use Custom Cryptography settings, Custom Cryptography settings will be ignored");
                }
            }

            if (ProfileType == ProfileType.UserBackup)
            {
                if (DisableAlwaysOn != true)
                {
                    ValidationWarnings.AppendLine("DisableAlwaysOn must be set for Backup VPN Tunnels");
                    DisableAlwaysOn = true;
                }
            }

            //Essential Machine Params
            if (ProfileType == ProfileType.Machine)
            {
                if (TunnelType == TunnelType.ForceTunnel)
                {
                    ValidationWarnings.AppendLine("Unable to Configure Machine Profile with Forced Tunnel, reseting to SplitTunnel");
                    TunnelType = TunnelType.SplitTunnel;
                }
            }

            if (RegisterDNS && DNSAlreadyRegistered && (ProfileType == ProfileType.User || ProfileType == ProfileType.UserBackup))
            {
                ValidationWarnings.AppendLine(RegistrySettings.RegisterDNS +" is already configured on the Machine Tunnel, Ignoring DNS Registration on User Tunnel");
            }

            //Ignored Machine Params
            if (ProfileType == ProfileType.Machine)
            {
                if (EKUMapping)
                {
                    ValidationWarnings.AppendLine("EKU is not supported for machine profiles as it relies on EAP Configuration, ignoring setting");
                    EKUMapping = false;
                }

                if (VPNStrategy != VPNStrategy.Ikev2Only)
                {
                    ValidationWarnings.AppendLine("SSTP is not supported for machine profiles, using IKEv2 Only instead");
                    VPNStrategy = VPNStrategy.Ikev2Only;
                }

                if (DisableAlwaysOn)
                {
                    ValidationWarnings.AppendLine("Disabling Always On is not supported for machine profiles, ignoring setting");
                    DisableAlwaysOn = false;
                }

                if (DisableCryptoBinding)
                {
                    ValidationWarnings.AppendLine("Disabling Crypto Binding is only relevant to user profiles, ignoring setting");
                    DisableCryptoBinding = false;
                }
            }
            else
            {
                if (MachineEKU != null && MachineEKU.Count > 0)
                {
                    ValidationWarnings.AppendLine("Machine EKU Filtering is only relevant to machine profiles, ignoring setting");
                    MachineEKU = null;
                }
            }

            if (!DeviceInfo.GetOSVersion().IsGreaterThanWin11_22H2 && DisableUIDisconnectButton)
            {
                ValidationWarnings.AppendLine("Disable Disconnect Button is only valid for Windows 11 22H2 and later, ignoring setting");
                DisableUIDisconnectButton = false;
            }

            if (!DeviceInfo.GetOSVersion().IsGreaterThanWin11_22H2 && DisableUIEditButton)
            {
                ValidationWarnings.AppendLine("Disable VPN Edit Button is only valid for Windows 11 22H2 and later, ignoring setting");
                DisableUIEditButton = false;
            }
        }

        private IList<string> ValidateList(IList<string> list, Func<string, bool> validateFunction)
        {
            IList<string> returnList = new List<string>();
            if (list != null)
            {
                foreach (string item in list)
                {
                    if (!validateFunction(item))
                    {
                        ValidationWarnings.AppendLine(validateFunction.Method + " Failed to validate: " + item);
                    }
                    else
                    {
                        returnList.Add(item);
                    }
                }
            }

            return returnList;
        }

        private Dictionary<string, string> ValidateDictionary(Dictionary<string, string> list, Func<string, bool> validateKeyFunction, Func<string, bool> validateValueFunction)
        {
            Dictionary<string, string> returnList = new Dictionary<string, string>();
            if (list != null)
            {
                foreach (KeyValuePair<string, string> item in list)
                {
                    if (!validateKeyFunction(item.Key))
                    {
                        ValidationWarnings.AppendLine(validateKeyFunction.Method + " Failed to validate Key: " + item.Key);
                    }
                    else if (!validateValueFunction(item.Value))
                    {
                        ValidationWarnings.AppendLine(validateValueFunction.Method + " Failed to validate Value: " + item.Value);
                    }
                    else
                    {
                        returnList.Add(item.Key, item.Value);
                    }
                }
            }

            return returnList;
        }

        private List<string> ValidateThumbprint(IList<string> thumbList)
        {
            List<string> returnList = new List<string>();
            string sanitisedThumbprint;
            if (thumbList != null)
            {
                foreach (string thumbprint in thumbList)
                {
                    sanitisedThumbprint = Validate.Thumbprint(thumbprint);
                    if (sanitisedThumbprint != null)
                    {
                        returnList.Add(sanitisedThumbprint);
                    }
                    else
                    {
                        ValidationWarnings.AppendLine("Failed to validate thumbprint: " + thumbprint);
                    }
                }
            }

            return returnList;
        }

        private static string FormatThumbprint(string thumbprint)
        {
            StringBuilder newThumbprint = new StringBuilder();
            for (int i = 0; i < thumbprint.Length; i++)
            {
                if ((i % 2) == 0 && i != 0)
                {
                    newThumbprint.Append(" ");
                }
                newThumbprint.Append(thumbprint[i]);
            }
            return newThumbprint.ToString();
        }

        private void ValidateProfile()
        {
            XmlDocument xml = new XmlDocument();
            try
            {
                using (StringReader profileReader = new StringReader(ProfileString.ToString()))
                {
                    xml.Load(profileReader);
                }
                xml.Schemas.Add(GenerateXMLSchemaSet());
                xml.Validate(ValidationCallBack);
            }
            catch (Exception e)
            {
                ValidationFailures.AppendLine("Unable to start Validation: " + e.Message);
            }
        }

        private static XmlSchemaSet GenerateXMLSchemaSet()
        {
            XmlSchemaSet schemas = new XmlSchemaSet();

            //Load the main AOVPN XSD from an embedded resource
            using (StringReader profileReader = new StringReader(Properties.Resources.VPNProfileSchema))
            {
                schemas.Add(XmlSchema.Read(profileReader, null));
            }

            List<string> eapSchemaFileList = new List<string>();

            //Load the EAP schemas from the local disk as they are the most likely to be accurate for the device importing the profile
            eapSchemaFileList.AddRange(Directory.GetFiles(Path.Combine(Environment.GetEnvironmentVariable("windir"), "schemas\\EAPHost"), "*.xsd"));
            eapSchemaFileList.AddRange(Directory.GetFiles(Path.Combine(Environment.GetEnvironmentVariable("windir"), "schemas\\EAPMethods"), "*.xsd"));

            eapSchemaFileList = eapSchemaFileList.Where(f => !f.Contains("EapGenericUserCredentials.xsd")).ToList(); //Remove EapGenericUserCredentials.xsd as it causes a validation failure

            foreach(string file in eapSchemaFileList)
            {
                using (XmlReader fileReader = XmlReader.Create(file))
                {
                    schemas.Add(XmlSchema.Read(fileReader, null));
                }
            }

            return schemas;
        }

        private void ValidationCallBack(object sender, ValidationEventArgs args)
        {
            if (args.Severity == XmlSeverityType.Warning)
            {
                ValidationWarnings.AppendLine(args.Message + " Line: " + args.Exception.LineNumber.ToString(CultureInfo.InvariantCulture) + " Position: " + args.Exception.LinePosition.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                ValidationFailures.AppendLine(args.Message + " Line: " + args.Exception.LineNumber.ToString(CultureInfo.InvariantCulture) + " Position: " + args.Exception.LinePosition.ToString(CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// This method handles the core logic of preparing to get the latest Office 365 exclusion routes. The Microsoft endpoint requires a unique identifier
        /// so we get it from registry if it already exists, if it doesn't we generate a new one and save it.
        /// After we get the results from the HTTP Service we process the results to only return the results that DPC can handle. This is because the service
        /// will return various types of result including URLs, wildcard URLs, IPv4 and IPv6 routes of which DPC can only handle IPv4 currently
        /// </summary>
        /// <returns>IPv4 route list to be excluded</returns>
        private static List<string> GetOffice365ExcludeRoutes()
        {
            Guid? nClientId = AccessRegistry.ReadMachineGuid(RegistrySettings.ClientId, RegistrySettings.InternalStateOffset);
            Guid clientId;
            if (nClientId == null)
            {
                clientId = Guid.NewGuid();
                AccessRegistry.SaveMachineData(RegistrySettings.ClientId, clientId.ToString());
            }
            else
            {
                clientId = (Guid)nClientId;
            }

            Office365Exclusion[] Office365Endpoints = HttpService.GetOffice365EndPoints(clientId);
            List<string[]> UsableIPList = Office365Endpoints.Where(e => e.Ips != null && e.Category == Office365EndpointCategory.Optimize).Select(e => e.Ips).ToList();
            List<string> ipList = new List<string>();
            foreach (string[] list in UsableIPList)
            {
                foreach (string item in list)
                {
                    if (ipList.Contains(item)) continue;
                    //Don't add IPv6 addresses as currently the WMI callback doesn't match IPv6 correctly so all profiles fail to validate
                    //if (Validate.IPv4(item) || Validate.IPv4CIDR(item) || Validate.IPv6(item) || Validate.IPv6CIDR(item))
                    if (Validate.IPv4(item) || Validate.IPv4CIDR(item))
                    {
                        ipList.Add(item);
                    }
                }
            }

            return ipList;
        }

        private static Dictionary<string, string> GetDNSIncludeRoutes(Dictionary<string, string> DNSList)
        {
            Dictionary<string, string> unvalidatedList = new Dictionary<string, string>();
            foreach (KeyValuePair<string, string> DNS in DNSList)
            {
                foreach (string IP in HttpService.GetIPfromDNS(DNS.Key))
                {
                    if (string.IsNullOrEmpty(DNS.Value))
                    {
                        //No user provided comment, use DNS instead
                        unvalidatedList.Add(IP, DNS.Key);
                    }
                    else
                    {
                        //User provided comment
                        unvalidatedList.Add(IP, DNS.Value);
                    }
                }
            }

            Dictionary<string, string> ipList = new Dictionary<string, string>();
            foreach (KeyValuePair<string, string> item in unvalidatedList)
            {
                if (ipList.ContainsKey(item.Key)) continue; //Skip Duplicate IPs
                //Don't add IPv6 addresses as currently the WMI callback doesn't match IPv6 correctly so all profiles fail to validate
                //if (Validate.IPv4(item) || Validate.IPv6(item))
                if (Validate.IPv4(item.Key))
                {
                    ipList.Add(item.Key, item.Value);
                }
            }

            return ipList;
        }

        public static string SaveProfile(string profileName, string profile, string savePath)
        {
            if (string.IsNullOrWhiteSpace(savePath))
            {
                return null;
            }

            savePath = savePath.Trim();

            if (string.IsNullOrWhiteSpace(profile))
            {
                throw new InvalidOperationException("Profile has not been generated");
            }

            if (string.IsNullOrWhiteSpace(profileName))
            {
                throw new InvalidOperationException("Profile Name has not been set");
            }

            savePath = Environment.ExpandEnvironmentVariables(savePath);

            if (!Path.HasExtension(savePath))
            {
                savePath = Path.Combine(savePath, profileName + ".xml");
            }

            string formattedProfile;

            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(profile);

                StringBuilder sb = new StringBuilder();
                XmlWriterSettings settings = new XmlWriterSettings
                {
                    Indent = true,
                    IndentChars = " ",
                    WriteEndDocumentOnClose = true,
                    OmitXmlDeclaration = true,
                    Encoding = Encoding.ASCII,
                    Async = true
                };

                using (XmlWriter writer = XmlWriter.Create(sb, settings))
                {
                    doc.Save(writer);
                }
                formattedProfile = sb.ToString();
            }
            catch
            {
                formattedProfile = profile;
            }

            savePath = Path.GetFullPath(savePath);

            Directory.CreateDirectory(Path.GetDirectoryName(savePath));

            File.WriteAllText(savePath, formattedProfile, Encoding.ASCII);

            return savePath;
        }
    }
}
