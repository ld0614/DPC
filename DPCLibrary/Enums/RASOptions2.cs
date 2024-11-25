using System;

namespace DPCLibrary.Enums
{
    //For up to date version install Windows SDK and look in C:\Program Files (x86)\Windows Kits\10\Include\<Version>\um\ras.h
    [Flags]
    public enum RasOptions2 : uint
    {
        None = 0x00000000,
        RASEO2_SecureFileAndPrint = 0x00000001,
        RASEO2_SecureClientForMSNet = 0x00000002,
        RASEO2_DontNegotiateMultilink = 0x00000004,
        RASEO2_DontUseRasCredentials = 0x00000008,
        RASEO2_UsePreSharedKey = 0x00000010,
        RASEO2_Internet = 0x00000020,
        RASEO2_DisableNbtOverIP = 0x00000040,
        RASEO2_UseGlobalDeviceSettings = 0x00000080,
        RASEO2_ReconnectIfDropped = 0x00000100,
        RASEO2_SharePhoneNumbers = 0x00000200,
        RASEO2_SecureRoutingCompartment = 0x00000400,
        RASEO2_UseTypicalSettings = 0x00000800,
        RASEO2_IPv6SpecificNameServers = 0x00001000,
        RASEO2_IPv6RemoteDefaultGateway = 0x00002000,
        RASEO2_RegisterIpWithDNS = 0x00004000,
        RASEO2_UseDNSSuffixForRegistration = 0x00008000,
        RASEO2_IPv4ExplicitMetric = 0x00010000,
        RASEO2_IPv6ExplicitMetric = 0x00020000,
        RASEO2_DisableIKENameEkuCheck = 0x00040000,
        RASEO2_DisableClassBasedStaticRoute = 0x00080000,
        RASEO2_SpecificIPv6Addr = 0x00100000,
        RASEO2_DisableMobility = 0x00200000,
        RASEO2_RequireMachineCertificates = 0x00400000,
        RASEO2_UsePreSharedKeyForIkev2Initiator = 0x00800000,
        RASEO2_UsePreSharedKeyForIkev2Responder = 0x01000000,
        RASEO2_CacheCredentials = 0x02000000,
        RASEO2_AutoTriggerCapable = 0x04000000,
        RASEO2_IsThirdPartyProfile = 0x08000000,
        RASEO2_AuthTypeIsOtp = 0x10000000,
        RASEO2_IsAlwaysOn = 0x20000000,
        RASEO2_IsPrivateNetwork = 0x40000000,
        RASEO2_PlumbIKEv2TSAsRoutes = 0x80000000
    }
}
