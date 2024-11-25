using DPCLibrary.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DPCLibraryTests
{
    [TestClass]
    [TestCategory("Basic")]
    public class IPAddressTests
    {
        /// <summary>
        ///  Gets or sets the test context which provides
        ///  information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext { get; set; }


        [TestMethod]
        [DataTestMethod]
        [DataRow("10.0.0.0")]
        [DataRow("217.56.25.4")]
        [DataRow("255.255.255.255")]
        public void ValidIPV4FromString(string address)
        {
            IPv4Address ip = new IPv4Address();
            ip.LoadFromString(address);
            Assert.AreEqual(address, ip.ToString());
        }

        [TestMethod]
        [DataTestMethod]
        [DataRow("2620:1ec:908::")]
        [DataRow("2a01:111:f402::")]
        [DataRow("2a01:111::f402")]
        [DataRow("2a01:2:111:0:f402::")]
        public void ValidIPV6FromString(string address)
        {
            IPv6Address ip = new IPv6Address();
            ip.LoadFromString(address);
            Assert.AreEqual(address, ip.ToString());
        }

        [TestMethod]
        [DataTestMethod]
        [DataRow("2620:1ec:908::")]
        [DataRow("2a01:111:f402::")]
        [DataRow("2a01:111::f402")]
        [DataRow("2a01:2:111:0:f402::")]
        public void IPV6EqualsItself(string address)
        {
            IPv6Address ip1 = new IPv6Address();
            ip1.LoadFromString(address);

            IPv6Address ip2 = new IPv6Address();
            ip2.LoadFromString(address);
            Assert.AreEqual(ip1, ip2);
        }
    }
}
