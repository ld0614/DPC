using DPCLibrary.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace DPCLibrary.Models
{
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public struct IPv6Address : IEquatable<IPv6Address>, IPAddress
    {
        [FieldOffset(0)] internal byte Byte_0;
        [FieldOffset(1)] internal byte Byte_1;
        [FieldOffset(2)] internal byte Byte_2;
        [FieldOffset(3)] internal byte Byte_3;

        [FieldOffset(4)] internal byte Byte_4;
        [FieldOffset(5)] internal byte Byte_5;
        [FieldOffset(6)] internal byte Byte_6;
        [FieldOffset(7)] internal byte Byte_7;

        [FieldOffset(8)] internal byte Byte_8;
        [FieldOffset(9)] internal byte Byte_9;
        [FieldOffset(10)] internal byte Byte_10;
        [FieldOffset(11)] internal byte Byte_11;

        [FieldOffset(12)] internal byte Byte_12;
        [FieldOffset(13)] internal byte Byte_13;
        [FieldOffset(14)] internal byte Byte_14;
        [FieldOffset(15)] internal byte Byte_15;

        [FieldOffset(0)] internal short Word_0;
        [FieldOffset(2)] internal short Word_1;
        [FieldOffset(4)] internal short Word_2;
        [FieldOffset(6)] internal short Word_3;

        [FieldOffset(8)] internal short Word_4;
        [FieldOffset(10)] internal short Word_5;
        [FieldOffset(12)] internal short Word_6;
        [FieldOffset(14)] internal short Word_7;

        public void LoadFromPBKHex(string pbkHex)
        {
            if (string.IsNullOrWhiteSpace(pbkHex))
            {
                throw new ArgumentException("IPAddress must not be null");
            }

            if (pbkHex.Length != 32)
            {
                throw new ArgumentException("IPv6Address must be 32 Hex Characters");
            }

            IEnumerable<string> ipv6PartsIE = pbkHex.SplitByLength(2, true);
            string[] ipv6Parts = ipv6PartsIE.ToArray();
            Byte_0 = Convert.ToByte(INIParser.FormatPBKByteAsUInt(ipv6Parts[0]));
            Byte_1 = Convert.ToByte(INIParser.FormatPBKByteAsUInt(ipv6Parts[1]));
            Byte_2 = Convert.ToByte(INIParser.FormatPBKByteAsUInt(ipv6Parts[2]));
            Byte_3 = Convert.ToByte(INIParser.FormatPBKByteAsUInt(ipv6Parts[3]));

            Byte_4 = Convert.ToByte(INIParser.FormatPBKByteAsUInt(ipv6Parts[4]));
            Byte_5 = Convert.ToByte(INIParser.FormatPBKByteAsUInt(ipv6Parts[5]));
            Byte_6 = Convert.ToByte(INIParser.FormatPBKByteAsUInt(ipv6Parts[6]));
            Byte_7 = Convert.ToByte(INIParser.FormatPBKByteAsUInt(ipv6Parts[7]));

            Byte_8 = Convert.ToByte(INIParser.FormatPBKByteAsUInt(ipv6Parts[8]));
            Byte_9 = Convert.ToByte(INIParser.FormatPBKByteAsUInt(ipv6Parts[9]));
            Byte_10 = Convert.ToByte(INIParser.FormatPBKByteAsUInt(ipv6Parts[10]));
            Byte_11 = Convert.ToByte(INIParser.FormatPBKByteAsUInt(ipv6Parts[11]));

            Byte_12 = Convert.ToByte(INIParser.FormatPBKByteAsUInt(ipv6Parts[12]));
            Byte_13 = Convert.ToByte(INIParser.FormatPBKByteAsUInt(ipv6Parts[13]));
            Byte_14 = Convert.ToByte(INIParser.FormatPBKByteAsUInt(ipv6Parts[14]));
            Byte_15 = Convert.ToByte(INIParser.FormatPBKByteAsUInt(ipv6Parts[15]));
        }

        public void LoadFromString(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                throw new ArgumentException("IPAddress must not be null");
            }

            if (!Validate.IPv6(address))
            {
                throw new ArgumentException("IPAddress must be a valid IPv6 Address");
            }

            System.Net.IPAddress ipv6 = System.Net.IPAddress.Parse(address);
            byte[] addressBytes = ipv6.GetAddressBytes();

            Byte_0 = addressBytes[0];
            Byte_1 = addressBytes[1];
            Byte_2 = addressBytes[2];
            Byte_3 = addressBytes[3];

            Byte_4 = addressBytes[4];
            Byte_5 = addressBytes[5];
            Byte_6 = addressBytes[6];
            Byte_7 = addressBytes[7];

            Byte_8 = addressBytes[8];
            Byte_9 = addressBytes[9];
            Byte_10 = addressBytes[10];
            Byte_11 = addressBytes[11];

            Byte_12 = addressBytes[12];
            Byte_13 = addressBytes[13];
            Byte_14 = addressBytes[14];
            Byte_15 = addressBytes[15];
        }

        bool IEquatable<IPAddress>.Equals(IPAddress other)
        {
            if (other is IPv6Address address)
            {
                return Equals(address);
            }
            else
            {
                return false;
            }
        }

        public override bool Equals(object obj)
        {
            return obj is IPv6Address address && Equals(address);
        }

        public bool Equals(IPv6Address other)
        {
            return Byte_0 == other.Byte_0 &&
                   Byte_1 == other.Byte_1 &&
                   Byte_2 == other.Byte_2 &&
                   Byte_3 == other.Byte_3 &&
                   Byte_4 == other.Byte_4 &&
                   Byte_5 == other.Byte_5 &&
                   Byte_6 == other.Byte_6 &&
                   Byte_7 == other.Byte_7 &&
                   Byte_8 == other.Byte_8 &&
                   Byte_9 == other.Byte_9 &&
                   Byte_10 == other.Byte_10 &&
                   Byte_11 == other.Byte_11 &&
                   Byte_12 == other.Byte_12 &&
                   Byte_13 == other.Byte_13 &&
                   Byte_14 == other.Byte_14 &&
                   Byte_15 == other.Byte_15 &&
                   Word_0 == other.Word_0 &&
                   Word_1 == other.Word_1 &&
                   Word_2 == other.Word_2 &&
                   Word_3 == other.Word_3 &&
                   Word_4 == other.Word_4 &&
                   Word_5 == other.Word_5 &&
                   Word_6 == other.Word_6 &&
                   Word_7 == other.Word_7;
        }

        public override int GetHashCode()
        {
            int hashCode = 1189785673;
            hashCode = hashCode * -1521134295 + Byte_0.GetHashCode();
            hashCode = hashCode * -1521134295 + Byte_1.GetHashCode();
            hashCode = hashCode * -1521134295 + Byte_2.GetHashCode();
            hashCode = hashCode * -1521134295 + Byte_3.GetHashCode();
            hashCode = hashCode * -1521134295 + Byte_4.GetHashCode();
            hashCode = hashCode * -1521134295 + Byte_5.GetHashCode();
            hashCode = hashCode * -1521134295 + Byte_6.GetHashCode();
            hashCode = hashCode * -1521134295 + Byte_7.GetHashCode();
            hashCode = hashCode * -1521134295 + Byte_8.GetHashCode();
            hashCode = hashCode * -1521134295 + Byte_9.GetHashCode();
            hashCode = hashCode * -1521134295 + Byte_10.GetHashCode();
            hashCode = hashCode * -1521134295 + Byte_11.GetHashCode();
            hashCode = hashCode * -1521134295 + Byte_12.GetHashCode();
            hashCode = hashCode * -1521134295 + Byte_13.GetHashCode();
            hashCode = hashCode * -1521134295 + Byte_14.GetHashCode();
            hashCode = hashCode * -1521134295 + Byte_15.GetHashCode();
            hashCode = hashCode * -1521134295 + Word_0.GetHashCode();
            hashCode = hashCode * -1521134295 + Word_1.GetHashCode();
            hashCode = hashCode * -1521134295 + Word_2.GetHashCode();
            hashCode = hashCode * -1521134295 + Word_3.GetHashCode();
            hashCode = hashCode * -1521134295 + Word_4.GetHashCode();
            hashCode = hashCode * -1521134295 + Word_5.GetHashCode();
            hashCode = hashCode * -1521134295 + Word_6.GetHashCode();
            hashCode = hashCode * -1521134295 + Word_7.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(IPv6Address left, IPv6Address right)
        {
            return EqualityComparer<IPv6Address>.Default.Equals(left, right);
        }

        public static bool operator !=(IPv6Address left, IPv6Address right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            byte[] bytes = new byte[16];
            bytes[0] = Byte_0;
            bytes[1] = Byte_1;
            bytes[2] = Byte_2;
            bytes[3] = Byte_3;
            bytes[4] = Byte_4;
            bytes[5] = Byte_5;
            bytes[6] = Byte_6;
            bytes[7] = Byte_7;
            bytes[8] = Byte_8;
            bytes[9] = Byte_9;
            bytes[10] = Byte_10;
            bytes[11] = Byte_11;
            bytes[12] = Byte_12;
            bytes[13] = Byte_13;
            bytes[14] = Byte_14;
            bytes[15] = Byte_15;

            System.Net.IPAddress ipv6 = new System.Net.IPAddress(bytes);

            return ipv6.ToString();
        }
    }
}
