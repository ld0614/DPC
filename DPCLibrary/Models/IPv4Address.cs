using DPCLibrary.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DPCLibrary.Models
{
    public struct IPv4Address : IEquatable<IPv4Address>, IPAddress
    {
        byte Byte_0;
        byte Byte_1;
        byte Byte_2;
        byte Byte_3;

        public void LoadFromPBKHex(string pbkHex)
        {
            if (string.IsNullOrWhiteSpace(pbkHex))
            {
                throw new ArgumentException("IPAddress must not be null");
            }

            if (pbkHex.Length != 8)
            {
                throw new ArgumentException("IPv4Address must be 8 Hex Characters");
            }

            IEnumerable<string> ipv4PartsIE = pbkHex.SplitByLength(2, true);
            string[] ipv4Parts = ipv4PartsIE.ToArray();
            Byte_0 = Convert.ToByte(INIParser.FormatPBKByteAsUInt(ipv4Parts[0]));
            Byte_1 = Convert.ToByte(INIParser.FormatPBKByteAsUInt(ipv4Parts[1]));
            Byte_2 = Convert.ToByte(INIParser.FormatPBKByteAsUInt(ipv4Parts[2]));
            Byte_3 = Convert.ToByte(INIParser.FormatPBKByteAsUInt(ipv4Parts[3]));
        }

        public void LoadFromString(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                throw new ArgumentException("IPAddress must not be null");
            }

            if (!Validate.IPv4Address(address))
            {
                throw new ArgumentException("IPAddress must be a valid IPv4 Address");
            }

            string[] addressParts = address.Split('.');

            if (addressParts.Length != 4)
            {
                throw new ArgumentException("IPv4Address must be 4 numbers split by .");
            }

            Byte_0 = Convert.ToByte(addressParts[0], CultureInfo.InvariantCulture);
            Byte_1 = Convert.ToByte(addressParts[1], CultureInfo.InvariantCulture);
            Byte_2 = Convert.ToByte(addressParts[2], CultureInfo.InvariantCulture);
            Byte_3 = Convert.ToByte(addressParts[3], CultureInfo.InvariantCulture);
        }

        public override bool Equals(object obj)
        {
            return obj is IPv4Address address && Equals(address);
        }

        public bool Equals(IPv4Address other)
        {
            return Byte_0 == other.Byte_0 &&
                   Byte_1 == other.Byte_1 &&
                   Byte_2 == other.Byte_2 &&
                   Byte_3 == other.Byte_3;
        }

        public override int GetHashCode()
        {
            int hashCode = 390099410;
            hashCode = hashCode * -1521134295 + Byte_0.GetHashCode();
            hashCode = hashCode * -1521134295 + Byte_1.GetHashCode();
            hashCode = hashCode * -1521134295 + Byte_2.GetHashCode();
            hashCode = hashCode * -1521134295 + Byte_3.GetHashCode();
            return hashCode;
        }

        public override string ToString()
        {
            return Byte_0.ToString(CultureInfo.InvariantCulture) + "." + Byte_1.ToString(CultureInfo.InvariantCulture) + "." + Byte_2.ToString(CultureInfo.InvariantCulture) + "." + Byte_3.ToString(CultureInfo.InvariantCulture);
        }

        bool IEquatable<IPAddress>.Equals(IPAddress other)
        {
            if (other is IPv4Address address)
            {
                return Equals(address);
            }
            else
            {
                return false;
            }
        }

        public static bool operator ==(IPv4Address left, IPv4Address right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(IPv4Address left, IPv4Address right)
        {
            return !(left == right);
        }
    }
}
