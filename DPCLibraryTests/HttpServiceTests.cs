using DPCLibrary.Models;
using DPCLibrary.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DPCLibraryTests
{
    [TestClass]
    [TestCategory("Basic")]
    public class HttpServiceTests
    {
        /// <summary>
        ///  Gets or sets the test context which provides
        ///  information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void GetOffice365Endpoints()
        {
            Office365Exclusion[] result = HttpService.GetOffice365EndPoints(Guid.NewGuid());
            Assert.IsGreaterThan(1, result.Length);
        }

        [TestMethod]
        public void CheckUDPLists()
        {
            Office365Exclusion[] result = HttpService.GetOffice365EndPoints(Guid.NewGuid());
            List<Office365Exclusion> udpList = result.Where(r => string.IsNullOrWhiteSpace(r.UdpPorts)).ToList();
            Assert.AreNotEqual(0, udpList.Count);
            Assert.AreNotEqual(udpList.Count, result.Length);
        }

        [TestMethod]
        public void CheckTCPLists()
        {
            Office365Exclusion[] result = HttpService.GetOffice365EndPoints(Guid.NewGuid());
            List<Office365Exclusion> tcpList = result.Where(r => string.IsNullOrWhiteSpace(r.TcpPorts)).ToList();
            Assert.AreNotEqual(0, tcpList.Count);
            Assert.AreNotEqual(tcpList.Count, result.Length);
        }

        [TestMethod]
        public void CheckIPAddressLists()
        {
            Office365Exclusion[] result = HttpService.GetOffice365EndPoints(Guid.NewGuid());
            List<Office365Exclusion> IpsList = result.Where(r => r.Ips != null && r.Ips.Length > 0).ToList();
            Assert.AreNotEqual(0, IpsList.Count);
            Assert.AreNotEqual(IpsList.Count, result.Length);
        }

        [TestMethod]
        [DataRow("www.google.co.uk")]
        [DataRow("www.microsoft.com")]
        [DataRow("www.bbc.co.uk")] //Should return more than 1 IP address
        public void DNSLookup(string url)
        {
            IList<string> IPList = HttpService.GetIPfromDNS(url);
            Assert.IsTrue(IPList.Count > 0);
            foreach (string ip in IPList)
            {
                TestContext.WriteLine(ip);
            }
        }
    }
}