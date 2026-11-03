using DPCLibrary.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Xml.Linq;

namespace DPCLibraryTests
{
    [TestClass]
    [TestCategory("Basic")]
    public class RouteTests
    {
        [TestMethod]
        public void ParsesIPv4CIDRRouteFromXml()
        {
            Route route = new Route(System.Xml.Linq.XElement.Parse(
                "<Route><Address>10.20.30.0/24</Address><Metric>4</Metric><ExclusionRoute>true</ExclusionRoute></Route>"));

            Assert.AreEqual("10.20.30.0", route.Address.ToString());
            Assert.AreEqual(24, route.Prefix);
            Assert.AreEqual((uint)4, route.Metric);
            Assert.IsTrue(route.ExclusionRoute);
        }

        [TestMethod]
        public void ParsesIPv6RouteAndUsesDefaultMetric()
        {
            Route route = new Route(System.Xml.Linq.XElement.Parse(
                "<Route><Address>2001:db8::1</Address><PrefixSize>64</PrefixSize></Route>"));

            Assert.AreEqual("2001:db8::1", route.Address.ToString());
            Assert.AreEqual(64, route.Prefix);
            Assert.AreEqual((uint)1, route.Metric);
            Assert.IsFalse(route.ExclusionRoute);
        }

        [TestMethod]
        public void RejectsPrefixOutsideAddressFamilyRange()
        {
            Assert.Throws<InvalidDataException>(() =>
                new Route(System.Xml.Linq.XElement.Parse("<Route><Address>192.0.2.1</Address><PrefixSize>33</PrefixSize></Route>")));

            Assert.Throws<InvalidDataException>(() =>
                new Route(System.Xml.Linq.XElement.Parse("<Route><Address>2001:db8::1</Address><PrefixSize>129</PrefixSize></Route>")));
        }
    }
}
