using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace DPCLibrary.Models
{
    [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Auto)]
    public struct RasConnection : IEquatable<RasConnection>
    {
        public int dwSize;
        public readonly IntPtr hrasconn;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = RasConstants.MaxEntryName)]
        public string szEntryName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = RasConstants.MaxDeviceType)]
        public string szDeviceType;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = RasConstants.MaxDeviceName)]
        public string szDeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = RasConstants.MAX_PATH)]
        public string szPhonebook;
        public int dwSubEntry;
        public Guid guidEntry;
        public int dwFlags;
        public Guid luid;

        public override bool Equals(object obj)
        {
            return obj is RasConnection connection && Equals(connection);
        }

        public bool Equals(RasConnection other)
        {
            return dwSize == other.dwSize &&
                   EqualityComparer<IntPtr>.Default.Equals(hrasconn, other.hrasconn) &&
                   szEntryName == other.szEntryName &&
                   szDeviceType == other.szDeviceType &&
                   szDeviceName == other.szDeviceName &&
                   szPhonebook == other.szPhonebook &&
                   dwSubEntry == other.dwSubEntry &&
                   guidEntry.Equals(other.guidEntry) &&
                   dwFlags == other.dwFlags &&
                   luid.Equals(other.luid);
        }

        public override int GetHashCode()
        {
            int hashCode = -812793559;
            hashCode = hashCode * -1521134295 + dwSize.GetHashCode();
            hashCode = hashCode * -1521134295 + hrasconn.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(szEntryName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(szDeviceType);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(szDeviceName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(szPhonebook);
            hashCode = hashCode * -1521134295 + dwSubEntry.GetHashCode();
            hashCode = hashCode * -1521134295 + guidEntry.GetHashCode();
            hashCode = hashCode * -1521134295 + dwFlags.GetHashCode();
            hashCode = hashCode * -1521134295 + luid.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(RasConnection left, RasConnection right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RasConnection left, RasConnection right)
        {
            return !(left == right);
        }
    }
}
