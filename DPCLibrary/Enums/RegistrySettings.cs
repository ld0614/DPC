using System.Data;

namespace DPCLibrary.Enums
{
    public static class RegistrySettings
    {
        //Service Settings Root Paths
        public const string PolicyPath = @"SOFTWARE\Policies\DPC\DPCClient";
        public const string ManualPath = @"SOFTWARE\DPC\DPCClient";

        //System Registry Paths
        public const string EnterpriseResourceManagerPath = @"SOFTWARE\Microsoft\EnterpriseResourceManager\Tracked";
        public const string NetworkListPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\NetworkList\Profiles";
        public const string RasMan = @"SYSTEM\CurrentControlSet\Services\RasMan";
        public const string RasManConfig = @"SYSTEM\CurrentControlSet\Services\RasMan\Config";
        public const string RasManParameters = @"SYSTEM\CurrentControlSet\Services\RasMan\Parameters";
        public const string OSVersion = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion";
        public const string VPNUI = @"SOFTWARE\Microsoft\Flyout\VPN";
        public const string ProfileList = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList";
        public const string NDISWANParameters = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Ndiswan\Parameters\Protocols\0";

        //ProfileList Key Values
        public const string ProfileImagePath = "ProfileImagePath";
        public const string FullProfile = "FullProfile";

        //RasMan Key Values
        public const string DeviceTunnel = "DeviceTunnel";
        public const string Config = "Config";

        //RasManConfig Key Values
        public const string AutoTriggerDisabled = "AutoTriggerDisabledProfilesList";
        public const string UserSID = "UserSID";
        public const string AutoTriggerProfileName = "AutoTriggerProfileEntryName";
        public const string AutoTriggerPBKPath = "AutoTriggerProfilePhonebookPath";
        public const string AutoTriggerProfileGUID = "AutoTriggerProfileGUID";

        //RasManParameters Key Values
        public const string VPNStrategyUsageDisabled = "VPNStrategyUsageDisabled";

        //VPN UI Key Values
        public const string ShowDeviceTunnelUI = "ShowDeviceTunnelInUI";

        //NDISWAN Parameters Key Values
        public const string TunnelMTU = "TunnelMTU";
        public const string ProtocolType = "ProtocolType";
        public const string PPPProtocolType = "PPPProtocolType";

        //OSVersion Key Values
        public const string ReleaseId = "ReleaseId"; //Deprecated https://borncity.com/win/2021/05/26/windows-10-21h1-reports-releaseid-2009/
        public const string DisplayVersion = "DisplayVersion"; //New way to identify display name of the build version
        public const string PatchVersion = "UBR";

        //Registry offset locations
        public const string UserTunnelOffset = "UserTunnel";
        public const string UserBackupTunnelOffset = "UserBackupTunnel";
        public const string MachineTunnelOffset = "MachineTunnel";
        public const string InternalStateOffset = "Internal";
        public const string InternalStateFullPath = ManualPath + "\\" + InternalStateOffset;

        //Internal Setting Values
        public const string O365LastUpdate = "O365LastUpdate";
        public const string DNSLastUpdate = "DNSLastUpdate";
        public const string ClientId = "ID";
        public const string MachineProfileName = "MachineProfileName";
        public const string UserProfileName = "UserProfileName";
        public const string BackupUserProfileName = "BackupUserProfileName";

        //Internal Setting Keys
        public const string O365ExclusionKey = "O365Exclusion";
        public const string DNSRouteListKey = "DNSRouteList";

        //Device Level Settings Values
        public const string ClearDisableProfileList = "ClearDisableProfileList";
        public const string RemoveAllProfiles = "RemoveAllProfiles";
        public const string MonitorVPN = "MonitorVPN";
        public const string RefreshPeriod = "RefreshPeriod";
        public const string ProductKey = "ProductKey";
        public const string UserTunnel = "UserTunnel";
        public const string UserBackupTunnel = "UserBackupTunnel";
        public const string MachineTunnel = "MachineTunnel";
        public const string MigrationBlock = "MigrationBlock";
        public const string MTU = "MTU";
        public const string RestartOnPortAlreadyOpen = "RestartOnPortAlreadyOpen";

        //Shared Tunnel Public Setting Values
        public const string ProfileName = "ProfileName";
        public const string ExternalAddress = "URL";
        public const string OverrideXML = "OverrideXML";
        public const string CustomCryptography = "CustomCryptography";
        public const string AuthenticationTransformConstants = "AuthenticationTransformConstants";
        public const string CipherTransformConstants = "CipherTransformConstants";
        public const string PfsGroup = "PfsGroup";
        public const string DHGroup = "DHGroup";
        public const string IntegrityCheckMethod = "IntegrityCheckMethod";
        public const string EncryptionMethod = "EncryptionMethod";
        public const string DebugPath = "DebugPath";
        public const string SavePath = "SavePath";
        public const string EnableProxy = "EnableProxy";
        public const string ProxyType = "ProxyType";
        public const string ProxyValue = "ProxyValue";
        public const string InterfaceMetric = "InterfaceMetric";
        public const string NetworkOutageTime = "NetworkOutageTime";
        public const string DisableRasCredentials = "DisableRasCredentials";
        public const string RegisterDNS = "RegisterDNS";
        public const string RouteMetric = "RouteMetric";
        public const string ProxyBypassForLocal = "ProxyBypassForLocal";
        public const string DisableUIDisconnectButton = "DisableUIDisconnectButton";
        public const string DisableUIEditButton = "DisableUIEditButton";

        //User Tunnel Unique Public Setting Values
        public const string ForceTunnel = "ForceTunnel";
        public const string ExcludeOffice365 = "ExcludeOffice365";
        public const string LimitEKU = "LimitEKU";
        public const string EKUName = "EKUName";
        public const string EKUOID = "EKUOID";
        public const string VPNStrategy = "VPNStrategy";
        public const string DisableAlwaysOn = "DisableAlwaysOn";
        public const string DisableCryptoBinding = "DisableCryptoBinding";
        public const string EnableEAPSmartCard = "EnableEAPSmartCard";
        public const string IncludeDeviceTunnelRoutes = "IncludeDeviceTunnelRoutes";
        public const string DeviceComplianceEnabled = "DeviceComplianceEnabled";
        public const string DeviceComplianceEKUOID = "DCEKUOID";
        public const string DeviceComplianceIssuerHash = "DCIssuerHash";
        public const string DisableNPSValidation = "DisableNPSValidation";

        //Shared Tunnel Public Setting Keys
        public const string DNSSuffixKey = "DNSSuffix";
        public const string TrustedNetworksKey = "TrustedNetworks";
        public const string DomainNameInfoKey = "DomainNameInfo";
        public const string RouteListKey = "RouteList";
        public const string TrafficFilters = "TrafficFilters";
        public const string DNSRouteList = "DNSRouteList";

        //User Tunnel Public Setting Keys
        public const string IssuingCertificatesKey = "IssuingCertificates";
        public const string RootCertificatesKey = "RootCertificates";
        public const string NPSListKey = "NPSList";
        public const string RouteListExcludeKey = "RouteListExclude";
        public const string ProxyExcludeList = "ProxyExcludeList";

        //Device Tunnel Unique Public Setting Values

        //Device Tunnel Public Setting Keys
        public const string MachineEKU = "MachineEKU";

        //Traffic Filter Setting Values
        public const string TrafficFilterAppId = "AppId";
        public const string TrafficFilterDirection = "Direction";
        public const string TrafficFilterEnabled = "Enabled";
        public const string TrafficFilterLocalAddresses = "LocalAddresses";
        public const string TrafficFilterRemoteAddresses = "RemoteAddresses";
        public const string TrafficFilterLocalPorts = "LocalPorts";
        public const string TrafficFilterRemotePorts = "RemotePorts";
        public const string TrafficFilterProtocol = "Protocol";
        public const string TrafficFilterRoutingPolicyType = "RoutingPolicyType";

        public static string GetProfileOffset(ProfileType profileType)
        {
            switch (profileType)
            {
                case ProfileType.Machine:
                    return MachineTunnelOffset;
                case ProfileType.User:
                    return UserTunnelOffset;
                case ProfileType.UserBackup:
                    return UserBackupTunnelOffset;
                default:
                    throw new InvalidConstraintException("Unknown Profile Type: " + profileType);
            }
        }
    }
}