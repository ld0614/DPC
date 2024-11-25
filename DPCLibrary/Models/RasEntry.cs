using DPCLibrary.Enums;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace DPCLibrary.Models
{
    [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Auto)]
    public struct RasEntry : IEquatable<RasEntry>
    {
        public uint dwSize;
        public RasOptions dwfOptions;
        public uint dwCountryID;
        public uint dwCountryCode;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = RasConstants.MaxAreaCode + 1)]
        public string szAreaCode;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = RasConstants.MaxPhoneNumber + 1)]
        public string szLocalPhoneNumber;
        public uint dwAlternateOffset;
        IPv4Address ipaddr;
        IPv4Address ipaddrDns;
        IPv4Address ipaddrDnsAlt;
        IPv4Address ipaddrWins;
        IPv4Address ipaddrWinsAlt;
        public uint dwFrameSize;
        public uint dwfNetProtocols;
        public uint dwFramingProtocol;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = RasConstants.MAX_PATH)]
        public string szScript;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = RasConstants.MAX_PATH)]
        public string szAutodialDll;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = RasConstants.MAX_PATH)]
        public string szAutodialFunc;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = RasConstants.MaxDeviceType + 1)]
        public string szDeviceType;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = RasConstants.MaxDeviceName + 1)]
        public string szDeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = RasConstants.MaxPadType + 1)]
        public string szX25PadType;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = RasConstants.MaxX25Address + 1)]
        public string szX25Address;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = RasConstants.MaxFacilities + 1)]
        public string szX25Facilities;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = RasConstants.MaxUserData + 1)]
        public string szX25UserData;
        public uint dwChannels;
        public uint dwReserved1;
        public uint dwReserved2;
        public uint dwSubEntries;
        public uint dwDialMode;
        public uint dwDialExtraPercent;
        public uint dwDialExtraSampleSeconds;
        public uint dwHangUpExtraPercent;
        public uint dwHangUpExtraSampleSeconds;
        public uint dwIdleDisconnectSeconds;
        public uint dwType;
        public uint dwEncryptionType;
        public uint dwCustomAuthKey;
        public Guid guidId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = RasConstants.MAX_PATH)]
        public string szCustomDialDll;
        public VPNStrategy dwVpnStrategy;
        public RasOptions2 dwfOptions2;
        public uint dwfOptions3;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = RasConstants.MaxDnsSuffix)]
        public string szDnsSuffix;
        public uint dwTcpWindowSize;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = RasConstants.MAX_PATH)]
        public string szPrerequisitePbk;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = RasConstants.MaxEntryName + 1)]
        public string szPrerequisiteEntry;
        public uint dwRedialCount;
        public uint dwRedialPause;
        IPv6Address ipv6addrDns;
        IPv6Address ipv6addrDnsAlt;
        public uint dwIPv4InterfaceMetric;
        public uint dwIPv6InterfaceMetric;
        IPv6Address ipv6addr;
        public uint dwIPv6PrefixLength;
        public uint dwNetworkOutageTime;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = RasConstants.MaxIDSize + 1)]
        public string szIDi;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = RasConstants.MaxIDSize + 1)]
        public string szIDr;
        public bool fIsImsConfig;
        IKEV2_ID_PAYLOAD_TYPE IdiType;
        IKEV2_ID_PAYLOAD_TYPE IdrType;
        public bool fDisableIKEv2Fragmentation;

        public override bool Equals(object obj)
        {
            return obj is RasEntry entry && Equals(entry);
        }

        public bool Equals(RasEntry other)
        {
            return dwSize == other.dwSize &&
                   dwfOptions == other.dwfOptions &&
                   dwCountryID == other.dwCountryID &&
                   dwCountryCode == other.dwCountryCode &&
                   szAreaCode == other.szAreaCode &&
                   szLocalPhoneNumber == other.szLocalPhoneNumber &&
                   dwAlternateOffset == other.dwAlternateOffset &&
                   ipaddr.Equals(other.ipaddr) &&
                   ipaddrDns.Equals(other.ipaddrDns) &&
                   ipaddrDnsAlt.Equals(other.ipaddrDnsAlt) &&
                   ipaddrWins.Equals(other.ipaddrWins) &&
                   ipaddrWinsAlt.Equals(other.ipaddrWinsAlt) &&
                   dwFrameSize == other.dwFrameSize &&
                   dwfNetProtocols == other.dwfNetProtocols &&
                   dwFramingProtocol == other.dwFramingProtocol &&
                   szScript == other.szScript &&
                   szAutodialDll == other.szAutodialDll &&
                   szAutodialFunc == other.szAutodialFunc &&
                   szDeviceType == other.szDeviceType &&
                   szDeviceName == other.szDeviceName &&
                   szX25PadType == other.szX25PadType &&
                   szX25Address == other.szX25Address &&
                   szX25Facilities == other.szX25Facilities &&
                   szX25UserData == other.szX25UserData &&
                   dwChannels == other.dwChannels &&
                   dwReserved1 == other.dwReserved1 &&
                   dwReserved2 == other.dwReserved2 &&
                   dwSubEntries == other.dwSubEntries &&
                   dwDialMode == other.dwDialMode &&
                   dwDialExtraPercent == other.dwDialExtraPercent &&
                   dwDialExtraSampleSeconds == other.dwDialExtraSampleSeconds &&
                   dwHangUpExtraPercent == other.dwHangUpExtraPercent &&
                   dwHangUpExtraSampleSeconds == other.dwHangUpExtraSampleSeconds &&
                   dwIdleDisconnectSeconds == other.dwIdleDisconnectSeconds &&
                   dwType == other.dwType &&
                   dwEncryptionType == other.dwEncryptionType &&
                   dwCustomAuthKey == other.dwCustomAuthKey &&
                   guidId.Equals(other.guidId) &&
                   szCustomDialDll == other.szCustomDialDll &&
                   dwVpnStrategy == other.dwVpnStrategy &&
                   dwfOptions2 == other.dwfOptions2 &&
                   dwfOptions3 == other.dwfOptions3 &&
                   szDnsSuffix == other.szDnsSuffix &&
                   dwTcpWindowSize == other.dwTcpWindowSize &&
                   szPrerequisitePbk == other.szPrerequisitePbk &&
                   szPrerequisiteEntry == other.szPrerequisiteEntry &&
                   dwRedialCount == other.dwRedialCount &&
                   dwRedialPause == other.dwRedialPause &&
                   ipv6addrDns.Equals(other.ipv6addrDns) &&
                   ipv6addrDnsAlt.Equals(other.ipv6addrDnsAlt) &&
                   dwIPv4InterfaceMetric == other.dwIPv4InterfaceMetric &&
                   dwIPv6InterfaceMetric == other.dwIPv6InterfaceMetric &&
                   ipv6addr.Equals(other.ipv6addr) &&
                   dwIPv6PrefixLength == other.dwIPv6PrefixLength &&
                   dwNetworkOutageTime == other.dwNetworkOutageTime &&
                   szIDi == other.szIDi &&
                   szIDr == other.szIDr &&
                   fIsImsConfig == other.fIsImsConfig &&
                   IdiType == other.IdiType &&
                   IdrType == other.IdrType &&
                   fDisableIKEv2Fragmentation == other.fDisableIKEv2Fragmentation;
        }

        public override int GetHashCode()
        {
            int hashCode = 1361127332;
            hashCode = hashCode * -1521134295 + dwSize.GetHashCode();
            hashCode = hashCode * -1521134295 + dwfOptions.GetHashCode();
            hashCode = hashCode * -1521134295 + dwCountryID.GetHashCode();
            hashCode = hashCode * -1521134295 + dwCountryCode.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(szAreaCode);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(szLocalPhoneNumber);
            hashCode = hashCode * -1521134295 + dwAlternateOffset.GetHashCode();
            hashCode = hashCode * -1521134295 + ipaddr.GetHashCode();
            hashCode = hashCode * -1521134295 + ipaddrDns.GetHashCode();
            hashCode = hashCode * -1521134295 + ipaddrDnsAlt.GetHashCode();
            hashCode = hashCode * -1521134295 + ipaddrWins.GetHashCode();
            hashCode = hashCode * -1521134295 + ipaddrWinsAlt.GetHashCode();
            hashCode = hashCode * -1521134295 + dwFrameSize.GetHashCode();
            hashCode = hashCode * -1521134295 + dwfNetProtocols.GetHashCode();
            hashCode = hashCode * -1521134295 + dwFramingProtocol.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(szScript);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(szAutodialDll);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(szAutodialFunc);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(szDeviceType);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(szDeviceName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(szX25PadType);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(szX25Address);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(szX25Facilities);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(szX25UserData);
            hashCode = hashCode * -1521134295 + dwChannels.GetHashCode();
            hashCode = hashCode * -1521134295 + dwReserved1.GetHashCode();
            hashCode = hashCode * -1521134295 + dwReserved2.GetHashCode();
            hashCode = hashCode * -1521134295 + dwSubEntries.GetHashCode();
            hashCode = hashCode * -1521134295 + dwDialMode.GetHashCode();
            hashCode = hashCode * -1521134295 + dwDialExtraPercent.GetHashCode();
            hashCode = hashCode * -1521134295 + dwDialExtraSampleSeconds.GetHashCode();
            hashCode = hashCode * -1521134295 + dwHangUpExtraPercent.GetHashCode();
            hashCode = hashCode * -1521134295 + dwHangUpExtraSampleSeconds.GetHashCode();
            hashCode = hashCode * -1521134295 + dwIdleDisconnectSeconds.GetHashCode();
            hashCode = hashCode * -1521134295 + dwType.GetHashCode();
            hashCode = hashCode * -1521134295 + dwEncryptionType.GetHashCode();
            hashCode = hashCode * -1521134295 + dwCustomAuthKey.GetHashCode();
            hashCode = hashCode * -1521134295 + guidId.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(szCustomDialDll);
            hashCode = hashCode * -1521134295 + dwVpnStrategy.GetHashCode();
            hashCode = hashCode * -1521134295 + dwfOptions2.GetHashCode();
            hashCode = hashCode * -1521134295 + dwfOptions3.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(szDnsSuffix);
            hashCode = hashCode * -1521134295 + dwTcpWindowSize.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(szPrerequisitePbk);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(szPrerequisiteEntry);
            hashCode = hashCode * -1521134295 + dwRedialCount.GetHashCode();
            hashCode = hashCode * -1521134295 + dwRedialPause.GetHashCode();
            hashCode = hashCode * -1521134295 + ipv6addrDns.GetHashCode();
            hashCode = hashCode * -1521134295 + ipv6addrDnsAlt.GetHashCode();
            hashCode = hashCode * -1521134295 + dwIPv4InterfaceMetric.GetHashCode();
            hashCode = hashCode * -1521134295 + dwIPv6InterfaceMetric.GetHashCode();
            hashCode = hashCode * -1521134295 + ipv6addr.GetHashCode();
            hashCode = hashCode * -1521134295 + dwIPv6PrefixLength.GetHashCode();
            hashCode = hashCode * -1521134295 + dwNetworkOutageTime.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(szIDi);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(szIDr);
            hashCode = hashCode * -1521134295 + fIsImsConfig.GetHashCode();
            hashCode = hashCode * -1521134295 + IdiType.GetHashCode();
            hashCode = hashCode * -1521134295 + IdrType.GetHashCode();
            hashCode = hashCode * -1521134295 + fDisableIKEv2Fragmentation.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(RasEntry left, RasEntry right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RasEntry left, RasEntry right)
        {
            return !(left == right);
        }
    }
}
