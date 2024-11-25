using System;

namespace DPCLibrary.Enums
{
    //For up to date version install Windows SDK and look in C:\Program Files (x86)\Windows Kits\10\Include\<Version>\um\ras.h
    [Flags]
    public enum RasOptions : uint
    {
        None = 0x00000000,
        RASEO_UseCountryAndAreaCodes = 0x00000001,
        RASEO_SpecificIpAddr = 0x00000002,
        RASEO_SpecificNameServers = 0x00000004,
        RASEO_IpHeaderCompression = 0x00000008,
        RASEO_RemoteDefaultGateway = 0x00000010,
        RASEO_DisableLcpExtensions = 0x00000020,
        RASEO_TerminalBeforeDial = 0x00000040,
        RASEO_TerminalAfterDial = 0x00000080,
        RASEO_ModemLights = 0x00000100,
        RASEO_SwCompression = 0x00000200,
        RASEO_RequireEncryptedPw = 0x00000400,
        RASEO_RequireMsEncryptedPw = 0x00000800,
        RASEO_RequireDataEncryption = 0x00001000,
        RASEO_NetworkLogon = 0x00002000,
        RASEO_UseLogonCredentials = 0x00004000,
        RASEO_PromoteAlternates = 0x00008000,
        RASEO_SecureLocalFiles = 0x00010000,
        RASEO_RequireEAP = 0x00020000,
        RASEO_RequirePAP = 0x00040000,
        RASEO_RequireSPAP = 0x00080000,
        RASEO_Custom = 0x00100000,
        RASEO_PreviewPhoneNumber = 0x00200000,
        RASEO_SharedPhoneNumbers = 0x00800000,
        RASEO_PreviewUserPw = 0x01000000,
        RASEO_PreviewDomain = 0x02000000,
        RASEO_ShowDialingProgress = 0x04000000,
        RASEO_RequireCHAP = 0x08000000,
        RASEO_RequireMsCHAP = 0x10000000,
        RASEO_RequireMsCHAP2 = 0x20000000,
        RASEO_RequireW95MSCHAP = 0x40000000,
        RASEO_CustomScript = 0x80000000
    }
}
