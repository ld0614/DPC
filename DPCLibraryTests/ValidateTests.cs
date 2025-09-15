using DPCLibrary.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DPCLibraryTests
{
    [TestClass]
    [TestCategory("Basic")]
    public class ValidateTests
    {
        [DataTestMethod]
        [DataRow("tttt")]
        [DataRow("")] //Empty
        [DataRow(null)] //null
        [DataRow("10.0.0.256")]
        [DataRow("10.0.256.0")]
        [DataRow("10.256.0.0")]
        [DataRow("256.0.0.0")]
        [DataRow("10.0..0")]
        [DataRow("10.0.0.")]
        [DataRow("10.0.0")]
        [DataRow("10..0.0")]
        [DataRow(".0.0.0")]
        [DataRow("...")]
        [DataRow("10.0.0.0.")]
        [DataRow("..0.0")]
        [DataRow("2001:0db9::1/64")]
        [DataRow("2001:0db9::1")]
        [DataRow("1.1.1.1/0")]
        [DataRow("1.1.1.1/-1")]
        [DataRow("1.1.1.1/33")]
        [DataRow("1.1.1.1/bob")]
        [DataRow("192.168..1/32")]
        [DataRow("192.168.1.1/0")]
        [DataRow("0.0.0.0")]
        public void InvalidIpv4ORCIDR(string IPv4Address)
        {
            bool result = Validate.IPv4OrCIDR(IPv4Address);
            Assert.IsFalse(result);
        }

        [DataTestMethod]
        [DataRow("10.0.0.0")]
        [DataRow("172.16.35.3")]
        [DataRow("192.168.5.4")]
        [DataRow("255.255.255.255")]
        [DataRow("20.1.2.3")]
        [DataRow("10.0.0.0/8")]
        [DataRow("172.16.0.0/12")]
        [DataRow("10.32.99.0/24")]
        [DataRow("192.168.0.0/16")]
        [DataRow("192.168.1.1/32")]
        [DataRow("20.1.2.3/32")]
        [DataRow("0.0.0.0/1")]
        [DataRow("0.0.0.0/0")]
        public void ValidIpv4ORCIDR(string IPv4Address)
        {
            bool result = Validate.IPv4OrCIDR(IPv4Address);
            Assert.IsTrue(result);
        }

        [DataTestMethod]
        [DataRow("tttt")]
        [DataRow("")] //Empty
        [DataRow(null)] //null
        [DataRow("10.0.0.256")]
        [DataRow("10.0.0.0/8")]
        [DataRow("10.0.0.0/16")]
        [DataRow("10.0.0.0/32")]
        [DataRow("10.32.99.0/24")]
        [DataRow("10.0.256.0")]
        [DataRow("10.256.0.0")]
        [DataRow("256.0.0.0")]
        [DataRow("10.0..0")]
        [DataRow("10.0.0.")]
        [DataRow("10.0.0")]
        [DataRow("10..0.0")]
        [DataRow(".0.0.0")]
        [DataRow("...")]
        [DataRow("10.0.0.0.")]
        [DataRow("..0.0")]
        [DataRow("2001:0db9::1/64")]
        [DataRow("2001:0db9::1")]
        [DataRow("0.0.0.0/0")]
        [DataRow("0.0.0.0")]
        public void InvalidIpv4(string IPv4Address)
        {
            bool result = Validate.IPv4(IPv4Address);
            Assert.IsFalse(result);
        }

        [DataTestMethod]
        [DataRow("10.0.0.0")]
        [DataRow("172.16.35.3")]
        [DataRow("192.168.5.4")]
        [DataRow("255.255.255.255")]
        [DataRow("20.1.2.3")]
        public void ValidIpv4(string IPv4Address)
        {
            bool result = Validate.IPv4(IPv4Address);
            Assert.IsTrue(result);
        }

        [DataTestMethod]
        [DataRow("2001:0db9::1/128")]
        [DataRow("2001::/64")]
        [DataRow("2001:0db9::ac11:c9/128")]
        [DataRow("2001:0db9:ac11::/32")]
        public void ValidIpv6CIDR(string IPv6Address)
        {
            bool result = Validate.IPv6CIDR(IPv6Address);
            Assert.IsTrue(result);
        }

        [DataTestMethod]
        [DataRow("2001:0db9::1")]
        [DataRow("2001::1")]
        [DataRow("1:2:3:4:5:6:7:8")]
        [DataRow("172.16.35.3")]
        [DataRow("192.168.5.4")]
        [DataRow("10.32.99.0/24")]
        [DataRow("192.168.0.0/16")]
        [DataRow("192.168.1.1/32")]
        public void InvalidIpv6CIDR(string IPv6Address)
        {
            bool result = Validate.IPv6CIDR(IPv6Address);
            Assert.IsFalse(result);
        }

        [DataTestMethod]
        [DataRow("tttt")]
        [DataRow("")] //Empty
        [DataRow(null)] //null
        [DataRow("10.0.0.256")]
        [DataRow("10.0.0.0/8")]
        [DataRow("10.0.0.0/16")]
        [DataRow("10.0.0.0/32")]
        [DataRow("10.32.99.0/24")]
        [DataRow("10.0.256.0")]
        [DataRow("10.256.0.0")]
        [DataRow("256.0.0.0")]
        [DataRow("10.0..0")]
        [DataRow("10.0.0.")]
        [DataRow("10.0.0")]
        [DataRow("10..0.0")]
        [DataRow(".0.0.0")]
        [DataRow("...")]
        [DataRow("10.0.0.0.")]
        [DataRow("..0.0")]
        [DataRow("10.0.0.0")]
        [DataRow("172.16.35.3")]
        [DataRow("192.168.5.4")]
        [DataRow("255.255.255.255")]
        [DataRow("20.1.2.3")]
        [DataRow("0.0.0.0")]
        [DataRow("172.16.0.0/12")]
        [DataRow("192.168.0.0/16")]
        [DataRow("192.168.1.1/32")]
        [DataRow("20.1.2.3/32")]
        [DataRow("00:00:00:00:00:00:00:00")]
        [DataRow("00:00:00:00:00:00:00:00/8")]
        [DataRow("0:0:0:0:0:0:0:0")]
        [DataRow("0:00:000:0000:0000:000:00:0")]
        [DataRow("0:00:000:0000:0000:000:00:0/0")]
        [DataRow("0::0")]
        [DataRow("::/0")]
        public void InvalidIpv6ORCIDR(string IPv4Address)
        {
            bool result = Validate.IPv6OrCIDR(IPv4Address);
            Assert.IsFalse(result);
        }

        [DataTestMethod]
        [DataRow("2001:0db9::1")]
        [DataRow("2001::1")]
        [DataRow("1:2:3:4:5:6:7:8")]
        [DataRow("2001:0db9::1/64")]
        [DataRow("2001:0db9::1/128")]
        [DataRow("2001:0db9:c9::/128")]
        public void ValidIpv6ORCIDR(string IPv4Address)
        {
            bool result = Validate.IPv6OrCIDR(IPv4Address);
            Assert.IsTrue(result);
        }

        [DataTestMethod]
        [DataRow("2001:0db9::1")]
        [DataRow("2001::1")]
        [DataRow("1:2:3:4:5:6:7:8")]
        [DataRow("2001:0db9::1/64")]
        [DataRow("2001:0db9::1/128")]
        [DataRow("10.0.0.0")]
        [DataRow("172.16.35.3")]
        [DataRow("192.168.5.4")]
        [DataRow("255.255.255.255")]
        [DataRow("20.1.2.3")]
        [DataRow("10.0.0.0/8")]
        [DataRow("172.16.0.0/12")]
        [DataRow("10.32.99.0/24")]
        [DataRow("192.168.0.0/16")]
        [DataRow("192.168.1.1/32")]
        [DataRow("20.1.2.3/32")]
        [DataRow("0.0.0.0/1")]
        [DataRow("0.0.0.0/0")]
        public void ValidIpv4ORIpv6ORCIDR(string IPv4Address)
        {
            bool result = Validate.IPv4OrIPv6OrCIDR(IPv4Address);
            Assert.IsTrue(result);
        }

        [DataTestMethod]
        [DataRow("tttt")]
        [DataRow("")] //Empty
        [DataRow(null)] //null
        [DataRow("10.0.0.256")]
        [DataRow("10.0.0.0/8")]
        [DataRow("10.0.0.0/16")]
        [DataRow("10.0.0.0/32")]
        [DataRow("10.32.99.0/24")]
        [DataRow("10.0.256.0")]
        [DataRow("10.256.0.0")]
        [DataRow("256.0.0.0")]
        [DataRow("10.0..0")]
        [DataRow("10.0.0.")]
        [DataRow("10.0.0")]
        [DataRow("10..0.0")]
        [DataRow(".0.0.0")]
        [DataRow("...")]
        [DataRow("10.0.0.0.")]
        [DataRow("..0.0")]
        [DataRow("10.0.0.0")]
        [DataRow("172.16.35.3")]
        [DataRow("192.168.5.4")]
        [DataRow("255.255.255.255")]
        [DataRow("20.1.2.3")]
        [DataRow("0.0.0.0")]
        [DataRow("0:0:0:0::0")]
        [DataRow("::")]
        [DataRow("2001:0db9::1/64")]
        [DataRow("00:00:00:00:00:00:00:00")]
        [DataRow("0:0:0:0:0:0:0:0")]
        [DataRow("0:00:000:0000:0000:000:00:0")]
        [DataRow("0:00:000:0000:0000:000:00:0/0")]
        [DataRow("0::0")]
        [DataRow("::/0")]
        public void InvalidIpv6(string IPv6Address)
        {
            bool result = Validate.IPv6(IPv6Address);
            Assert.IsFalse(result);
        }

        [DataTestMethod]
        [DataRow("2001:0db9::1")]
        [DataRow("2001::1")]
        [DataRow("1:2:3:4:5:6:7:8")]
        [DataRow("1111:2222:3333:4444:5555:6666:7777:8888")]
        [DataRow("2001:0db9::ac11:c9")]
        public void ValidIpv6(string IPv6Address)
        {
            bool result = Validate.IPv6(IPv6Address);
            Assert.IsTrue(result);
        }

        [DataTestMethod]
        [DataRow("")] //Empty
        [DataRow(null)] //null
        [DataRow("Test")]
        [DataRow(".test.local")]
        [DataRow("2001:0db9::1")]
        [DataRow("192.168.5.4")]
        [DataRow("Leo-NPS-01.local:8080")]
        [DataRow("te--st..domain.com")]
        [DataRow(".")]
        [DataRow("..")]
        public void InvalidConnectionURL(string address)
        {
            bool result = Validate.ValidateConnectionURL(address);
            Assert.IsFalse(result);
        }

        [DataTestMethod]
        [DataRow("Leo-NPS-01.local")]
        [DataRow("aovpndpcunittest.systemcenter.ninja")]
        [DataRow("aovpn.test.com")]
        [DataRow("test.ad.domain.com")]
        [DataRow("a.b.com")]
        [DataRow("te--st.ad.domain.com")]
        [DataRow("azuregateway-xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx-xxxxxxxxxxxx.vpn.azure.com")]
        [DataRow("abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcde.abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijk.abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijk.abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijk.com")]
        public void ValidConnectionURL(string IPv4Address)
        {
            bool result = Validate.ValidateConnectionURL(IPv4Address);
            Assert.IsTrue(result);
        }

        [DataTestMethod]
        [DataRow("")] //Empty
        [DataRow(null)] //null
        [DataRow("Test")]
        [DataRow("2001:0db9::1")]
        [DataRow("192.168.5.4")]
        [DataRow("Leo-NPS-01.local:8080")]
        [DataRow("te--st..domain.com")]
        [DataRow("..")]
        [DataRow("Test Network")]
        [DataRow("Test-Network")]
        public void InvalidFQDN(string address)
        {
            bool result = Validate.ValidateFQDN(address);
            Assert.IsFalse(result);
        }

        [DataTestMethod]
        [DataRow(".")]
        [DataRow(".test.local")]
        [DataRow("Leo-NPS-01.local")]
        [DataRow("aovpndpcunittest.systemcenter.ninja")]
        [DataRow("aovpn.test.com")]
        [DataRow("test.ad.domain.com")]
        [DataRow("a.b.com")]
        [DataRow("te--st.ad.domain.com")]
        [DataRow("azuregateway-xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx-xxxxxxxxxxxx.vpn.azure.com")]
        [DataRow("abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcde.abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijk.abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijk.abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijk.com")]
        public void ValidFQDN(string IPv4Address)
        {
            bool result = Validate.ValidateFQDN(IPv4Address);
            Assert.IsTrue(result);
        }

        [DataTestMethod]
        [DataRow(".")]
        [DataRow(".test.local")]
        [DataRow("Leo-NPS-01.local")]
        [DataRow("XYZ-INTRANET")]
        [DataRow("MySite XYZ Intranet")]
        [DataRow("aovpndpcunittest.systemcenter.ninja")]
        [DataRow("aovpn.test.com")]
        [DataRow("test.ad.domain.com")]
        [DataRow("test.ad.dÖmain.com")]
        [DataRow("a.b.com")]
        [DataRow("test")]
        [DataRow("te--st.ad.domain.com")]
        [DataRow("azuregateway-xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx-xxxxxxxxxxxx.vpn.azure.com")]
        [DataRow("abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcde.abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijk.abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijk.abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijk.com")]
        public void ValidTrustedNetwork(string domain)
        {
            bool result = Validate.ValidateTrustedNetwork(domain);
            Assert.IsTrue(result);
        }

        [DataTestMethod]
        [DataRow("")] //Empty
        [DataRow(null)] //null
        [DataRow("2001:0db9::1")]
        [DataRow("192.168.5.4")]
        [DataRow("Leo-NPS-01.local:8080")]
        [DataRow("te--st..domain.com")]
        [DataRow("..")]
        public void InvalidTrustedNetwork(string domain)
        {
            bool result = Validate.ValidateTrustedNetwork(domain);
            Assert.IsFalse(result);
        }

        [DataTestMethod]
        [DataRow("")] //Empty
        [DataRow(null)] //null
        [DataRow("2001:0db9::1")]
        [DataRow("Leo-NPS-01.local:8080")]
        [DataRow("AOVPN|DPC")]
        [DataRow("<AOVPN DPC")]
        [DataRow("AOVPN DPC>")]
        [DataRow("AOVPN DPC?")]
        [DataRow("AOVPN * DPC")]
        [DataRow("AOVPN \\ DPC")]
        [DataRow("AOVPN / DPC")]
        [DataRow("AOVPN : DPC")]
        [DataRow(".AOVPNDPC")]
        public void InvalidProfileName(string profileName)
        {
            bool result = Validate.ProfileName(profileName);
            Assert.IsFalse(result);
        }

        [DataTestMethod]
        [DataRow("AOVPN Profile 1")]
        [DataRow("AOVPN User Profile")]
        [DataRow("AOVPN User Profile - DPC!")]
        [DataRow("UserProfile")]
        [DataRow("User-Profile")]
        [DataRow("User_Profile")]
        [DataRow("User_Profile (DPC)")]
        [DataRow("192.168.5.4")]
        [DataRow("DPC$")]
        [DataRow("DPC£")]
        [DataRow("DPC \"Backup\"")]
        public void ValidProfileName(string IPv4Address)
        {
            bool result = Validate.ProfileName(IPv4Address);
            Assert.IsTrue(result);
        }

        [DataTestMethod]
        [DataRow("tttt")]
        [DataRow("")] //Empty
        [DataRow(null)] //null
        [DataRow("1.1.1.1/0")]
        [DataRow("1.1.1.1/-1")]
        [DataRow("1.1.1.1/33")]
        [DataRow("1.1.1.1/bob")]
        [DataRow("2001:0db9::1/64")]
        [DataRow("2001:0db9::1")]
        [DataRow("192.168..1/32")]
        public void InvalidIpv4CIDR(string IPv4Address)
        {
            bool result = Validate.IPv4CIDR(IPv4Address);
            Assert.IsFalse(result);
        }

        [DataTestMethod]
        [DataRow("10.0.0.0/8")]
        [DataRow("172.16.0.0/12")]
        [DataRow("192.168.0.0/16")]
        [DataRow("192.168.1.1/32")]
        [DataRow("20.1.2.3/32")]
        [DataRow("0.0.0.0/1")]
        [DataRow("0.0.0.0/0")]
        public void ValidIpv4CIDR(string IPv4Address)
        {
            bool result = Validate.IPv4CIDR(IPv4Address);
            Assert.IsTrue(result);
        }

        [DataTestMethod]
        [DataRow("tttt")] //Invalid length, invalid chars
        [DataRow("0549D9E2D68C0E18489FAD298C0362")] //Invalid length, valid chars
        [DataRow("0549D9E2D68C0E18489FAD298C0362621D334ZZZ")] //invalid chars, right length
        [DataRow("0549D9E2D68C0E18489FAD298C03626_1D334285")]
        [DataRow("")] //Empty
        [DataRow(null)] //null
        public void InvalidThumbprint(string thumbprint)
        {
            string result = Validate.Thumbprint(thumbprint);
            Assert.IsNull(result);
        }

        [DataTestMethod]
        [DataRow("0549D9E2D68C0E18489FAD298C0362    621D     33     3999")]
        public void ModifiedThumbprint(string thumbprint)
        {
            string result = Validate.Thumbprint(thumbprint);
            Assert.IsNotNull(result);
            Assert.AreNotEqual(thumbprint, result); //Check that the invalid data was removed
            Assert.IsNotNull(result); //Check result is a valid thumbprint
            Assert.AreEqual(result, Validate.Thumbprint(result)); //Check that running the check a second time doesn't change the result
        }

        [DataTestMethod]
        [DataRow("0549D9E2D68C0E18489FAD298C0362621D334285")]
        public void ValidThumbprint(string thumbprint)
        {
            string result = Validate.Thumbprint(thumbprint);
            Assert.IsNotNull(result); //Check thumbprint was valid
            Assert.AreEqual(result, thumbprint); //Check no changes where required
        }

        [DataTestMethod]
        [DataRow("")] //Empty
        [DataRow(null)] //null
        [DataRow("Test")]
        [DataRow("2001:0db9::1")]
        [DataRow("192.168.5.4")]
        [DataRow("Leo-NPS-01.local:8080")]
        [DataRow("Profile$")]
        [DataRow("1.2.3.4")]
        public void InvalidOID(string IPv4Address)
        {
            bool result = Validate.OID(IPv4Address);
            Assert.IsFalse(result);
        }

        [DataTestMethod]
        [DataRow("1.3.6.1.5.5.7.3.9")]
        [DataRow("1.2.3.4.5")]
        [DataRow("1.2.3.4.5.6")]
        [DataRow("1.3.6.1.4.1.311.21.8.6879051.11092925.9259695.707173.11379668.100.1.26")]
        public void ValidOID(string oid)
        {
            bool result = Validate.OID(oid);
            Assert.IsTrue(result);
        }

        [DataTestMethod]
        [DataRow("")]
        [DataRow("  ")]
        [DataRow(null)]
        [DataRow("500")]
        [DataRow("500-1000")]
        [DataRow("500-1000,443")]
        [DataRow("2000-1000")]
        [DataRow("2000 - 1000")]
        [DataRow("2000  -   1000")]
        [DataRow("500,443")]
        [DataRow("500 , 443 ")]
        [DataRow("65535")]
        [DataRow("65535     ")]
        public void ValidPortList(string ports)
        {
            bool result = Validate.PortList(ports);
            Assert.IsTrue(result);
        }

        [DataTestMethod]
        [DataRow("I'mATeaPot")]
        [DataRow("1.1.1.1")]
        [DataRow("65536")]
        [DataRow("1000-65538")]
        [DataRow("500-1000-2000")]
        [DataRow("500-")]
        public void InValidPortList(string ports)
        {
            bool result = Validate.PortList(ports);
            Assert.IsFalse(result);
        }

        [DataTestMethod]
        [DataRow("192.168.1.1")]
        [DataRow("192.168.1.0/24")]
        [DataRow("10.0.0.1,10.0.0.2")]
        [DataRow("10.0.0.0/24,10.0.1.0/24")]
        public void ValidAddressList(string ipv4List)
        {
            bool result = Validate.IPv4List(ipv4List);
            Assert.IsTrue(result);
        }

        [DataTestMethod]
        [DataRow("I'mATeaPot")]
        [DataRow("80,446")]
        [DataRow("65536")]
        [DataRow("100-200")]
        [DataRow("1000-65538")]
        [DataRow("500-1000-2000")]
        [DataRow("500-")]
        [DataRow("2001:0db9::1")]
        [DataRow("2001::1")]
        [DataRow("1:2:3:4:5:6:7:8")]
        public void InValidAddressList(string ipv4List)
        {
            bool result = Validate.IPv4List(ipv4List);
            Assert.IsFalse(result);
        }

        [DataTestMethod]
        [DataRow("")]
        [DataRow("  ")]
        [DataRow(null)]
        [DataRow("F46D4000-FD22-4DB4-AC8E-4E1DDDE828FE_cw5n1h2txyewy")]
        [DataRow("Microsoft.Windows.AssignedAccessLockApp_cw5n1h2txyewy")]
        [DataRow("NcsiUwpApp_8wekyb3d8bbwe")]
        [DataRow("SYSTEM")]
        [DataRow("C:\\Windows\\mstsc.exe")]
        [DataRow("C:\\Test\\Test.exe")]
        public void ValidPackageName(string packageId)
        {
            bool result = Validate.PackageId(packageId);
            Assert.IsTrue(result);
        }

        [DataTestMethod]
        [DataRow("F46D4000-FD22-4DB4-AC8E-4E1DDDE828FE")]
        [DataRow("_8wekyb3d8bbwe")]
        [DataRow("test.exe")] //Relative Path Won't Work
        [DataRow("dsfsdfsdfsdf")]
        [DataRow("500-1000-2000")]
        [DataRow("500-")]
        public void InValidPackageName(string packageId)
        {
            bool result = Validate.PackageId(packageId);
            Assert.IsFalse(result);
        }
    }
}