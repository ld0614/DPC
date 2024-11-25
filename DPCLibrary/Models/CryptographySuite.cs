using DPCLibrary.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;

namespace DPCLibrary.Models
{
    public class CryptographySuite : IEquatable<CryptographySuite>
    {
        public AuthenticationTransformConstant AuthenticationTransformConstants { get; set; }
        public CipherTransformConstant CipherTransformConstants { get; set; }
        public PfsGroup PfsGroup { get; set; }
        public DHGroup DHGroup { get; set; }
        public IntegrityCheckMethod IntegrityCheckMethod { get; set; }
        public EncryptionMethod EncryptionMethod { get; set; }

        public CryptographySuite() {
            AuthenticationTransformConstants = AuthenticationTransformConstant.MD596;
            CipherTransformConstants = CipherTransformConstant.None;
            PfsGroup = PfsGroup.None;
            DHGroup = DHGroup.None;
            IntegrityCheckMethod = IntegrityCheckMethod.MD5;
            EncryptionMethod = EncryptionMethod.DES;
        }

        public CryptographySuite(XElement CryptoNode)
        {
            if (CryptoNode != null)
            {
                string tempAuthenticationTransformConstants = CryptoNode.XPathSelectElement("AuthenticationTransformConstants")?.Value;
                if (!string.IsNullOrWhiteSpace(tempAuthenticationTransformConstants))
                {
                    AuthenticationTransformConstants = (AuthenticationTransformConstant)Enum.Parse(typeof(AuthenticationTransformConstant), tempAuthenticationTransformConstants);
                }

                string tempCipherTransformConstants = CryptoNode.XPathSelectElement("CipherTransformConstants")?.Value;
                if (!string.IsNullOrWhiteSpace(tempCipherTransformConstants))
                {
                    CipherTransformConstants = (CipherTransformConstant)Enum.Parse(typeof(CipherTransformConstant), tempCipherTransformConstants);
                }

                string tempPfsGroup = CryptoNode.XPathSelectElement("PfsGroup")?.Value;
                if (!string.IsNullOrWhiteSpace(tempPfsGroup))
                {
                    PfsGroup = (PfsGroup)Enum.Parse(typeof(PfsGroup), tempPfsGroup);
                }

                string tempDHGroup = CryptoNode.XPathSelectElement("DHGroup")?.Value;
                if (!string.IsNullOrWhiteSpace(tempDHGroup))
                {
                    DHGroup = (DHGroup)Enum.Parse(typeof(DHGroup), tempDHGroup);
                }

                string tempIntegrityCheckMethod = CryptoNode.XPathSelectElement("IntegrityCheckMethod")?.Value;
                if (!string.IsNullOrWhiteSpace(tempIntegrityCheckMethod))
                {
                    IntegrityCheckMethod = (IntegrityCheckMethod)Enum.Parse(typeof(IntegrityCheckMethod), tempIntegrityCheckMethod);
                }

                string tempEncryptionMethod = CryptoNode.XPathSelectElement("EncryptionMethod")?.Value;
                if (!string.IsNullOrWhiteSpace(tempEncryptionMethod))
                {
                    EncryptionMethod = (EncryptionMethod)Enum.Parse(typeof(EncryptionMethod), tempEncryptionMethod);
                }
            }
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as CryptographySuite);
        }

        public bool Equals(CryptographySuite other)
        {
            return other != null &&
                   AuthenticationTransformConstants == other.AuthenticationTransformConstants &&
                   CipherTransformConstants == other.CipherTransformConstants &&
                   PfsGroup == other.PfsGroup &&
                   DHGroup == other.DHGroup &&
                   IntegrityCheckMethod == other.IntegrityCheckMethod &&
                   EncryptionMethod == other.EncryptionMethod;
        }

        public override int GetHashCode()
        {
            int hashCode = 1352505544;
            hashCode = hashCode * -1521134295 + AuthenticationTransformConstants.GetHashCode();
            hashCode = hashCode * -1521134295 + CipherTransformConstants.GetHashCode();
            hashCode = hashCode * -1521134295 + PfsGroup.GetHashCode();
            hashCode = hashCode * -1521134295 + DHGroup.GetHashCode();
            hashCode = hashCode * -1521134295 + IntegrityCheckMethod.GetHashCode();
            hashCode = hashCode * -1521134295 + EncryptionMethod.GetHashCode();
            return hashCode;
        }

        public override string ToString()
        {
            StringBuilder returnString = new StringBuilder();
            returnString.AppendLine("AuthenticationTransformConstants: " + AuthenticationTransformConstants);
            returnString.AppendLine("CipherTransformConstants: " + CipherTransformConstants);
            returnString.AppendLine("PfsGroup: " + PfsGroup);
            returnString.AppendLine("DHGroup: " + DHGroup);
            returnString.AppendLine("IntegrityCheckMethod: " + IntegrityCheckMethod);
            returnString.AppendLine("EncryptionMethod: " + EncryptionMethod);

            return returnString.ToString();
        }

        public static bool operator ==(CryptographySuite left, CryptographySuite right)
        {
            return EqualityComparer<CryptographySuite>.Default.Equals(left, right);
        }

        public static bool operator !=(CryptographySuite left, CryptographySuite right)
        {
            return !(left == right);
        }
    }
}
