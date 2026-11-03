using DPCLibrary.Enums;
using DPCLibrary.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace DPCLibraryTests
{
    [TestClass]
    [TestCategory("Basic")]
    public class TrafficFilterTests
    {
        [TestMethod]
        public void ParsesAndNormalizesCspTrafficFilter()
        {
            TrafficFilter filter = new TrafficFilter(XElement.Parse(
                "<TrafficFilter><App><Id>sample-app</Id></App><Protocol>6</Protocol>" +
                "<LocalPortRanges>443, 80</LocalPortRanges><RemotePortRanges>8080</RemotePortRanges>" +
                "<LocalAddressRanges>192.0.2.1, 10.0.0.0/8</LocalAddressRanges>" +
                "<RemoteAddressRanges>2001:db8::1</RemoteAddressRanges>" +
                "<RoutingPolicyType>ForceTunnel</RoutingPolicyType><Direction>Inbound</Direction></TrafficFilter>"));

            Assert.AreEqual("sample-app", filter.AppId);
            Assert.AreEqual(Protocol.TCP, filter.Protocol);
            Assert.AreEqual("80,443", filter.LocalPorts);
            Assert.AreEqual("8080", filter.RemotePorts);
            Assert.AreEqual("192.0.2.1/32,10.0.0.0/8", filter.LocalAddresses);
            Assert.AreEqual("2001:db8::1/128", filter.RemoteAddresses);
            Assert.AreEqual(TunnelType.ForceTunnel, filter.RoutingPolicyType);
            Assert.AreEqual(ProtocolDirection.Inbound, filter.Direction);
        }

        [TestMethod]
        public void ParsesFirewallRulePortsAddressesAndDirection()
        {
            TrafficFilter filter = new TrafficFilter(new Dictionary<string, string>
            {
                { "Name", "Firewall rule" },
                { "Protocol", "17" },
                { "LPort", " 53 " },
                { "LPort2_10", "1000-2000" },
                { "RA4", "198.51.100.0/24" },
                { "BtoIf", "true" },
                { "Dir", "In" }
            });

            Assert.AreEqual("Firewall rule", filter.RuleName);
            Assert.AreEqual(Protocol.UDP, filter.Protocol);
            Assert.AreEqual("1000-2000,53", filter.LocalPorts);
            Assert.AreEqual("198.51.100.0/24", filter.RemoteAddresses);
            Assert.AreEqual(TunnelType.ForceTunnel, filter.RoutingPolicyType);
            Assert.AreEqual(ProtocolDirection.Inbound, filter.Direction);
        }

        [TestMethod]
        public void NormalizesCspRemotePortsWithIndividualPorts()
        {
            TrafficFilter filter = new TrafficFilter(XElement.Parse(
                "<TrafficFilter><Protocol>6</Protocol><RemotePortRanges> 443, 80, 8080 </RemotePortRanges></TrafficFilter>"));

            Assert.AreEqual("80,443,8080", filter.RemotePorts);
        }

        [TestMethod]
        public void NormalizesCspLocalPortsWithRangesAndIndividualPorts()
        {
            TrafficFilter filter = new TrafficFilter(XElement.Parse(
                "<TrafficFilter><Protocol>6</Protocol><LocalPortRanges>443,1000-2000,80</LocalPortRanges></TrafficFilter>"));

            Assert.AreEqual("1000-2000,80,443", filter.LocalPorts);
        }

        [TestMethod]
        public void NormalizesFirewallRuleRemotePortsWithRangesAndIndividualPorts()
        {
            TrafficFilter filter = new TrafficFilter(new Dictionary<string, string>
            {
                { "RPort", "443,80" },
                { "RPort2_10", "500-1000" }
            });

            Assert.AreEqual("500-1000,80,443", filter.RemotePorts);
        }

        [TestMethod]
        public void NormalizesFirewallRuleLocalPortsWithIndividualPortsOnly()
        {
            TrafficFilter filter = new TrafficFilter(new Dictionary<string, string>
            {
                { "LPort", "8080,443,80" }
            });

            Assert.AreEqual("80,443,8080", filter.LocalPorts);
        }

        [TestMethod]
        public void KeepsDescendingLocalPortRangeStringAndNormalizesAroundIt()
        {
            TrafficFilter filter = new TrafficFilter(XElement.Parse(
                "<TrafficFilter><Protocol>6</Protocol><LocalPortRanges>443,2000-1000,80</LocalPortRanges></TrafficFilter>"));

            Assert.AreEqual("2000-1000,80,443", filter.LocalPorts);
        }

        [TestMethod]
        public void KeepsDescendingRemotePortRangeStringAndNormalizesAroundIt()
        {
            TrafficFilter filter = new TrafficFilter(new Dictionary<string, string>
            {
                { "RPort", "443,80" },
                { "RPort2_10", "2000-1000" }
            });

            Assert.AreEqual("2000-1000,80,443", filter.RemotePorts);
        }

        [TestMethod]
        public void RejectsUnknownFirewallRuleDirection()
        {
            Assert.Throws<InvalidDataException>(() =>
                new TrafficFilter(new Dictionary<string, string> { { "Dir", "Sideways" } }));
        }
    }
}
