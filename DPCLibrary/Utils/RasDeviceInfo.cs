using DPCLibrary.Models;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace DPCLibrary.Utils
{
    [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Auto)]
    public struct RasDeviceInfo : IEquatable<RasDeviceInfo>
    {
        public int dwSize;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = RasConstants.MaxDeviceType + 1)]
        public string szDeviceType;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = RasConstants.MaxDeviceName + 1)]
        public string szDeviceName;

        public override bool Equals(object obj)
        {
            return obj is RasDeviceInfo info && Equals(info);
        }

        public bool Equals(RasDeviceInfo other)
        {
            return dwSize == other.dwSize &&
                   szDeviceType == other.szDeviceType &&
                   szDeviceName == other.szDeviceName;
        }

        public override int GetHashCode()
        {
            int hashCode = -804040896;
            hashCode = hashCode * -1521134295 + dwSize.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(szDeviceType);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(szDeviceName);
            return hashCode;
        }

        public static bool operator ==(RasDeviceInfo left, RasDeviceInfo right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RasDeviceInfo left, RasDeviceInfo right)
        {
            return !(left == right);
        }
    }
}
