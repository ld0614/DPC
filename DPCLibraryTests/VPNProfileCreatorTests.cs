using DPCLibrary.Enums;
using DPCLibrary.Models;
using DPCLibrary.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;

namespace DPCLibraryTests
{
    [TestClass]
    [TestCategory("Administrator")]
    public class VPNProfileCreatorTests
    {
        /// <summary>
        /// Define once standard basic settings which can then be overridden in the individual tests as required
        ///</summary>
        private static readonly Dictionary<string, string> standardUserRouteList = new Dictionary<string, string> {
                                                            { "10.0.0.0/8", "Server Network" },
                                                            { "172.16.0.0/12", "Test Network" }
                                                        };
        private static readonly Dictionary<string, string> standardExcludeUserRouteList = new Dictionary<string, string> {
                                                            { "10.0.0.15", "Web Server 1" },
                                                            { "172.16.0.1", "Web Server 2" }
                                                        };
        private readonly Dictionary<string, string> standardDeviceRouteList = new Dictionary<string, string> {
                                                            { "10.0.0.1", "DC01" }
                                                        };

        private readonly string standardServerName = "aovpn.example.com";
        private readonly List<string> standardNPSServerList = new List<string>
                        {
                            "Leo-Test-01.test.local",
                            "Leo-Test-02.test.local"
                        };
        private readonly List<string> standardRootCAList = new List<string>
                        {
                            "0549D9E2D68C0E18489FAD298C0362621D334123",
                            "1239D9E2D68C0E18489FAD298C0362621D334456"
                        };
        private readonly List<string> standardIssuingCAList = new List<string>
                        {
                            "0549D9E2D68C0E18489FAD298C0362621D334456",
                            "4569D9E2D68C0E18489FAD298C0362621D334789"
                        };

        /// <summary>
        ///  Gets or sets the test context which provides
        ///  information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext { get; set; }

        private void ValidateXMLText(string profile, string element, string value)
        {
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(profile);
            XmlNodeList servers = xml.GetElementsByTagName(element);
            Assert.AreEqual(1, servers.Count);
            Assert.AreEqual(value, servers.Item(0).InnerText);
        }

        private void ValidateXMLText(string profile, string parent, string element, string value, int expectedNumber)
        {
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(profile);
            XmlNodeList servers = xml.GetElementsByTagName(element);
            int nodeCount = 0;
            foreach (XmlNode node in servers)
            {
                if (node.ParentNode.Name != parent) continue;
                nodeCount++;
                Assert.AreEqual(value, node.InnerText);
            }
            Assert.AreEqual(expectedNumber, nodeCount);
        }

        private void CreateBasicDeviceProfileInRegistry()
        {
            AccessRegistry.SaveMachineData(RegistrySettings.ExternalAddress, standardServerName, RegistrySettings.GetProfileOffset(ProfileType.Machine));
            AccessRegistry.SaveMachineData(RegistrySettings.ProfileName, "AOVPN Device Profile", RegistrySettings.GetProfileOffset(ProfileType.Machine));
            AccessRegistry.SaveMachineData(RegistrySettings.RouteListKey, standardDeviceRouteList, RegistrySettings.GetProfileOffset(ProfileType.Machine));
        }

        private void CreateBasicUserProfileInRegistry(ProfileType profileType)
        {
            AccessRegistry.SaveMachineData(RegistrySettings.ExternalAddress, standardServerName, RegistrySettings.GetProfileOffset(profileType));
            AccessRegistry.SaveMachineData(RegistrySettings.ProfileName, "AOVPN User Profile", RegistrySettings.GetProfileOffset(profileType));
            AccessRegistry.SaveMachineData(RegistrySettings.RootCertificatesKey, standardRootCAList, RegistrySettings.GetProfileOffset(profileType));
            AccessRegistry.SaveMachineData(RegistrySettings.IssuingCertificatesKey, standardIssuingCAList, RegistrySettings.GetProfileOffset(profileType));
            AccessRegistry.SaveMachineData(RegistrySettings.NPSListKey, standardNPSServerList, RegistrySettings.GetProfileOffset(profileType));
            AccessRegistry.SaveMachineData(RegistrySettings.RouteListKey, standardUserRouteList, RegistrySettings.GetProfileOffset(profileType));
        }

        private void ValidateXMLList(string profile, string element, List<string> list)
        {
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(profile);
            XmlNodeList servers = xml.GetElementsByTagName(element);
            Assert.AreEqual(1, servers.Count);
            Assert.AreEqual(string.Join(",", list), servers.Item(0).InnerText);
        }

        private void ValidateXMLTextDuplicated(string profile, string element, string serverName)
        {
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(profile);
            XmlNodeList servers = xml.GetElementsByTagName(element);
            Assert.AreEqual(2, servers.Count);
            Assert.AreEqual(serverName, servers.Item(0).InnerText);
            Assert.AreEqual(serverName, servers.Item(1).InnerText);
        }

        private void ValidateXMLTextIsMissing(string profile, string element)
        {
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(profile);
            XmlNodeList servers = xml.GetElementsByTagName(element);
            Assert.AreEqual(0, servers.Count);
        }

        private void ValidateNPSList(string profile, List<string> serverList)
        {
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(profile);
            XmlNodeList servers = xml.GetElementsByTagName("ServerNames");
            Assert.AreEqual(2, servers.Count); //NPS Servers Appear twice in the XML
            Assert.AreEqual(string.Join(";", serverList).Replace("\0",""), servers.Item(0).InnerText);
            Assert.AreEqual(string.Join(";", serverList).Replace("\0",""), servers.Item(1).InnerText);
        }

        private void ValidateRootThumbprintList(string profile, List<string> thumbprintList)
        {
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(profile);
            XmlNodeList servers = xml.GetElementsByTagName("TrustedRootCA");
            Assert.AreEqual(thumbprintList.Count * 2, servers.Count); //Thumbprints are listed individually and twice
            foreach (XmlElement ele in servers)
            {
                Assert.IsTrue(thumbprintList.Contains(ele.InnerText.Replace(" ", "")));
            }
        }

        private void ValidateIssuingThumbprintList(string profile, List<string> thumbprintList)
        {
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(profile);
            XmlNodeList servers = xml.GetElementsByTagName("IssuerHash");
            Assert.AreEqual(servers.Count, thumbprintList.Count); //Thumbprints are listed individually and twice
            foreach (XmlElement ele in servers)
            {
                Assert.IsTrue(thumbprintList.Contains(ele.InnerText.Replace(" ", "")));
            }
        }

        private RegistryKey GetHKLM()
        {
            PrivateType privateTypeObject = new PrivateType(typeof(AccessRegistry));
            object hklmObj = privateTypeObject.InvokeStatic("GetHKLM");

            Assert.IsNotNull(hklmObj);
            RegistryKey hklm = (RegistryKey)hklmObj;
            Assert.IsNotNull(hklm);
            return hklm;
        }

        private void DeleteRegLocation(string location)
        {
            RegistryKey hklm = GetHKLM();
            if (hklm.OpenSubKey(location) != null)
            {
                hklm.DeleteSubKeyTree(location);
            }
            hklm.Close();
            Assert.IsNull(GetHKLM().OpenSubKey(location));
        }

        [TestInitialize]
        public void PreTestInitialize()
        {
            ClearRegistry();
            ResetHttpService();
        }

        [TestCleanup]
        public void PostTestCleanup()
        {
            ClearRegistry();
            ResetHttpService();
        }

        private void ClearRegistry()
        {
            DeleteRegLocation(RegistrySettings.PolicyPath);
            DeleteRegLocation(RegistrySettings.ManualPath);
        }

        private void ResetHttpService()
        {
            //Reset Network Test Code
            PrivateType type = new PrivateType(typeof(HttpService));
            type.SetStaticField("breakNetwork", BindingFlags.NonPublic, false);
        }

        [DataTestMethod]
        [DataRow(ProfileType.Machine)]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void CreateProfileClassDefault(ProfileType profileType)
        {
            VPNProfileCreator pro = new VPNProfileCreator(profileType, false);
            Assert.IsFalse(pro.ValidateFailed());
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationFailures()));
            TestContext.WriteLine(pro.GetValidationFailures());
        }

        [DataTestMethod]
        [DataRow(ProfileType.Machine)]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void CreateProfileClassNoLoad(ProfileType profileType)
        {
            VPNProfileCreator pro = new VPNProfileCreator(profileType, false);
            Assert.IsFalse(pro.ValidateFailed());
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationFailures()));
            TestContext.WriteLine(pro.GetValidationFailures());
        }

        [DataTestMethod]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void UserLoadRegistryWithNothing(ProfileType profileType)
        {
            VPNProfileCreator pro = new VPNProfileCreator(profileType, true);

            pro.Generate();
            TestContext.WriteLine(pro.GetValidationFailures());
            TestContext.WriteLine(pro.GetValidationWarnings());
            Assert.IsTrue(pro.ValidateFailed());
            Assert.IsFalse(string.IsNullOrWhiteSpace(pro.GetValidationFailures()));
        }

        [TestMethod]
        public void DeviceLoadRegistryWithNothing()
        {
            VPNProfileCreator pro = new VPNProfileCreator(ProfileType.Machine, true);

            pro.Generate();
            TestContext.WriteLine(pro.GetValidationFailures());
            TestContext.WriteLine(pro.GetValidationWarnings());
            Assert.IsTrue(pro.ValidateFailed());
            Assert.IsFalse(string.IsNullOrWhiteSpace(pro.GetValidationFailures()));
        }

        [DataTestMethod]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void UserLoadRegistryWithMultipleThumbprintLists(ProfileType profileType)
        {
            CreateBasicUserProfileInRegistry(profileType);

            VPNProfileCreator pro = new VPNProfileCreator(profileType, false);
            pro.LoadFromRegistry();

            pro.Generate();

            string profile = pro.GetProfile();

            TestContext.WriteLine(pro.GetValidationFailures());
            TestContext.WriteLine(pro.GetValidationWarnings());
            TestContext.WriteLine(profile);
            Assert.IsTrue(!pro.ValidateFailed());
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationFailures()));
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationWarnings()));
            Assert.IsFalse(string.IsNullOrWhiteSpace(profile));

            ValidateXMLText(profile, "Servers", standardServerName);
            ValidateXMLText(profile, "RoutingPolicyType", "SplitTunnel");
            ValidateXMLText(profile, "UserMethod", "Eap");
            ValidateNPSList(profile, standardNPSServerList);
            ValidateRootThumbprintList(profile, standardRootCAList);
            ValidateIssuingThumbprintList(profile, standardIssuingCAList);
        }

        [DataTestMethod]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void UserLoadRegistryWith3NPS(ProfileType profileType)
        {
            List<string> NPSServerList = new List<string>
                        {
                            "Leo-Test-01.test.local",
                            "Leo-Test-02.test.local",
                            "Leo-Test-03.test.local"
                        };

            CreateBasicUserProfileInRegistry(profileType);
            AccessRegistry.SaveMachineData("NPSList", NPSServerList, RegistrySettings.GetProfileOffset(profileType));

            VPNProfileCreator pro = new VPNProfileCreator(profileType, false);
            pro.LoadFromRegistry();

            pro.Generate();

            string profile = pro.GetProfile();

            TestContext.WriteLine(pro.GetValidationFailures());
            TestContext.WriteLine(pro.GetValidationWarnings());
            TestContext.WriteLine(profile);
            Assert.IsTrue(!pro.ValidateFailed());
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationFailures()));
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationWarnings()));
            Assert.IsFalse(string.IsNullOrWhiteSpace(profile));

            ValidateXMLText(profile, "Servers", standardServerName);
            ValidateXMLText(profile, "RoutingPolicyType", "SplitTunnel");
            ValidateXMLText(profile, "UserMethod", "Eap");
            ValidateNPSList(profile, NPSServerList);
            ValidateRootThumbprintList(profile, standardRootCAList);
            ValidateIssuingThumbprintList(profile, standardIssuingCAList);
        }

        [DataTestMethod]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void UserLoadRegistryWithNullValues(ProfileType profileType)
        {
            List<string> NPSServerList = new List<string>
                        {
                            "\0Leo-Test-01.test.local",
                            "Leo-Test-02.\0test.local",
                            "Leo-Test-03.test.local\0"
                        };

            CreateBasicUserProfileInRegistry(profileType);
            AccessRegistry.SaveMachineData("NPSList", NPSServerList, RegistrySettings.GetProfileOffset(profileType));

            VPNProfileCreator pro = new VPNProfileCreator(profileType, false);
            pro.LoadFromRegistry();

            pro.Generate();

            string profile = pro.GetProfile();

            TestContext.WriteLine(pro.GetValidationFailures());
            TestContext.WriteLine(pro.GetValidationWarnings());
            TestContext.WriteLine(profile);
            Assert.IsTrue(!pro.ValidateFailed());
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationFailures()));
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationWarnings()));
            Assert.IsFalse(string.IsNullOrWhiteSpace(profile));

            ValidateXMLText(profile, "Servers", standardServerName);
            ValidateXMLText(profile, "RoutingPolicyType", "SplitTunnel");
            ValidateXMLText(profile, "UserMethod", "Eap");
            ValidateNPSList(profile, NPSServerList);
            ValidateRootThumbprintList(profile, standardRootCAList);
            ValidateIssuingThumbprintList(profile, standardIssuingCAList);
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void UserProfileAlwaysOn(bool alwaysOn)
        {
            List<string> SuffixList = new List<string>
                        {
                            "test.local",
                            "testing.net",
                            "somewherelse.partner"
                        };

            CreateBasicUserProfileInRegistry(ProfileType.User);

            AccessRegistry.SaveMachineData("DisableAlwaysOn", !alwaysOn, RegistrySettings.GetProfileOffset(ProfileType.User));
            AccessRegistry.SaveMachineData("DNSSuffix", SuffixList, RegistrySettings.GetProfileOffset(ProfileType.User));
            AccessRegistry.SaveMachineData("TrustedNetworks", SuffixList, RegistrySettings.GetProfileOffset(ProfileType.User));

            VPNProfileCreator pro = new VPNProfileCreator(ProfileType.User, false);
            pro.LoadFromRegistry();

            pro.Generate();

            string profile = pro.GetProfile();

            TestContext.WriteLine(pro.GetValidationFailures());
            TestContext.WriteLine(pro.GetValidationWarnings());
            TestContext.WriteLine(profile);
            Assert.IsTrue(!pro.ValidateFailed());
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationFailures()));
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationWarnings()));
            Assert.IsFalse(string.IsNullOrWhiteSpace(profile));

            ValidateXMLText(profile, "Servers", standardServerName);
            ValidateXMLText(profile, "RoutingPolicyType", "SplitTunnel");
            ValidateXMLText(profile, "UserMethod", "Eap");
            ValidateXMLText(profile, "AlwaysOn", alwaysOn.ToString().ToLowerInvariant());
            ValidateNPSList(profile, standardNPSServerList);
            ValidateRootThumbprintList(profile, standardRootCAList);
            ValidateIssuingThumbprintList(profile, standardIssuingCAList);
            ValidateXMLList(profile, "DnsSuffix", SuffixList);
            ValidateXMLList(profile, "TrustedNetworkDetection", SuffixList);
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void BackupProfileAlwaysNotOn(bool alwaysOn)
        {
            List<string> SuffixList = new List<string>
                        {
                            "test.local",
                            "testing.net",
                            "somewherelse.partner"
                        };

            CreateBasicUserProfileInRegistry(ProfileType.UserBackup);

            AccessRegistry.SaveMachineData("DisableAlwaysOn", !alwaysOn, RegistrySettings.GetProfileOffset(ProfileType.UserBackup));
            AccessRegistry.SaveMachineData("DNSSuffix", SuffixList, RegistrySettings.GetProfileOffset(ProfileType.UserBackup));
            AccessRegistry.SaveMachineData("TrustedNetworks", SuffixList, RegistrySettings.GetProfileOffset(ProfileType.UserBackup));

            VPNProfileCreator pro = new VPNProfileCreator(ProfileType.UserBackup, false);
            pro.LoadFromRegistry();

            pro.Generate();

            string profile = pro.GetProfile();

            TestContext.WriteLine(pro.GetValidationFailures());
            TestContext.WriteLine(pro.GetValidationWarnings());
            TestContext.WriteLine(profile);
            Assert.IsTrue(!pro.ValidateFailed());
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationFailures()));
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationWarnings()));
            Assert.IsFalse(string.IsNullOrWhiteSpace(profile));

            ValidateXMLText(profile, "Servers", standardServerName);
            ValidateXMLText(profile, "RoutingPolicyType", "SplitTunnel");
            ValidateXMLText(profile, "UserMethod", "Eap");
            ValidateXMLText(profile, "AlwaysOn", "false");
            ValidateNPSList(profile, standardNPSServerList);
            ValidateRootThumbprintList(profile, standardRootCAList);
            ValidateIssuingThumbprintList(profile, standardIssuingCAList);
            ValidateXMLList(profile, "DnsSuffix", SuffixList);
            ValidateXMLList(profile, "TrustedNetworkDetection", SuffixList);
        }

        [DataTestMethod]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void UserLoadRegistryWithMultiNetwork(ProfileType profileType)
        {
            List<string> SuffixList = new List<string>
                        {
                            "test.local",
                            "testing.net",
                            "somewherelse.partner"
                        };

            CreateBasicUserProfileInRegistry(profileType);
            AccessRegistry.SaveMachineData("DNSSuffix", SuffixList, RegistrySettings.GetProfileOffset(profileType));
            AccessRegistry.SaveMachineData("TrustedNetworks", SuffixList, RegistrySettings.GetProfileOffset(profileType));

            VPNProfileCreator pro = new VPNProfileCreator(profileType, false);
            pro.LoadFromRegistry();

            pro.Generate();

            string profile = pro.GetProfile();

            TestContext.WriteLine(pro.GetValidationFailures());
            TestContext.WriteLine(pro.GetValidationWarnings());
            TestContext.WriteLine(profile);
            Assert.IsTrue(!pro.ValidateFailed());
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationFailures()));
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationWarnings()));
            Assert.IsFalse(string.IsNullOrWhiteSpace(profile));

            ValidateXMLText(profile, "Servers", standardServerName);
            ValidateXMLText(profile, "RoutingPolicyType", "SplitTunnel");
            ValidateXMLText(profile, "UserMethod", "Eap");
            ValidateNPSList(profile, standardNPSServerList);
            ValidateRootThumbprintList(profile, standardRootCAList);
            ValidateIssuingThumbprintList(profile, standardIssuingCAList);
            ValidateXMLList(profile, "DnsSuffix", SuffixList);
            ValidateXMLList(profile, "TrustedNetworkDetection", SuffixList);
        }

        [DataTestMethod]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void UserLoadRegistryWithSingleNetwork(ProfileType profileType)
        {
            List<string> SuffixList = new List<string>
                        {
                            "test.local"
                        };

            CreateBasicUserProfileInRegistry(profileType);
            AccessRegistry.SaveMachineData("DNSSuffix", SuffixList, RegistrySettings.GetProfileOffset(profileType));
            AccessRegistry.SaveMachineData("TrustedNetworks", SuffixList, RegistrySettings.GetProfileOffset(profileType));

            VPNProfileCreator pro = new VPNProfileCreator(profileType, false);
            pro.LoadFromRegistry();

            pro.Generate();

            string profile = pro.GetProfile();

            TestContext.WriteLine(pro.GetValidationFailures());
            TestContext.WriteLine(pro.GetValidationWarnings());
            TestContext.WriteLine(profile);
            Assert.IsTrue(!pro.ValidateFailed());
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationFailures()));
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationWarnings()));
            Assert.IsFalse(string.IsNullOrWhiteSpace(profile));

            ValidateXMLText(profile, "Servers", standardServerName);
            ValidateXMLText(profile, "RoutingPolicyType", "SplitTunnel");
            ValidateXMLText(profile, "UserMethod", "Eap");
            ValidateNPSList(profile, standardNPSServerList);
            ValidateRootThumbprintList(profile, standardRootCAList);
            ValidateIssuingThumbprintList(profile, standardIssuingCAList);
            ValidateXMLList(profile, "DnsSuffix", SuffixList);
            ValidateXMLList(profile, "TrustedNetworkDetection", SuffixList);
        }

        [DataTestMethod]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void UserLoadRegistryWithDisabledCryptobinding(ProfileType profileType)
        {
            CreateBasicUserProfileInRegistry(profileType);

            AccessRegistry.SaveMachineData("DisableCryptoBinding", true, RegistrySettings.GetProfileOffset(profileType));

            VPNProfileCreator pro = new VPNProfileCreator(profileType, false);
            pro.LoadFromRegistry();

            pro.Generate();

            string profile = pro.GetProfile();

            TestContext.WriteLine(pro.GetValidationFailures());
            TestContext.WriteLine(pro.GetValidationWarnings());
            TestContext.WriteLine(profile);
            Assert.IsTrue(!pro.ValidateFailed());
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationFailures()));
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationWarnings()));
            Assert.IsFalse(string.IsNullOrWhiteSpace(profile));

            ValidateXMLText(profile, "Servers", standardServerName);
            ValidateXMLText(profile, "RoutingPolicyType", "SplitTunnel");
            ValidateXMLText(profile, "UserMethod", "Eap");
            ValidateXMLText(profile, "RequireCryptoBinding", "false");
            ValidateNPSList(profile, standardNPSServerList);
            ValidateRootThumbprintList(profile, standardRootCAList);
            ValidateIssuingThumbprintList(profile, standardIssuingCAList);
        }

        [DataTestMethod]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void UserLoadRegistryWithSmartCard(ProfileType profileType)
        {
            CreateBasicUserProfileInRegistry(profileType);

            AccessRegistry.SaveMachineData("EnableEAPSmartCard", true, RegistrySettings.GetProfileOffset(profileType));

            VPNProfileCreator pro = new VPNProfileCreator(profileType, false);
            pro.LoadFromRegistry();

            pro.Generate();

            string profile = pro.GetProfile();

            TestContext.WriteLine(pro.GetValidationFailures());
            TestContext.WriteLine(pro.GetValidationWarnings());
            TestContext.WriteLine(profile);
            Assert.IsTrue(!pro.ValidateFailed());
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationFailures()));
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationWarnings()));
            Assert.IsFalse(string.IsNullOrWhiteSpace(profile));

            ValidateXMLText(profile, "Servers", standardServerName);
            ValidateXMLText(profile, "RoutingPolicyType", "SplitTunnel");
            ValidateXMLText(profile, "UserMethod", "Eap");
            ValidateXMLText(profile, "GroupSmartCardCerts", "true");
            ValidateXMLText(profile, "SmartCard", "");
            ValidateNPSList(profile, standardNPSServerList);
            ValidateRootThumbprintList(profile, standardRootCAList);
            ValidateIssuingThumbprintList(profile, standardIssuingCAList);
        }

        [DataTestMethod]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void UserLoadRegistryWithTrafficFilterDefaultConfig(ProfileType profileType)
        {
            List<string> trafficFilters = new List<string> {
                            "TF1",
                            "TF2" };

            CreateBasicUserProfileInRegistry(profileType);
            AccessRegistry.SaveMachineData(RegistrySettings.DisableCryptoBinding, true, RegistrySettings.GetProfileOffset(profileType));
            AccessRegistry.SaveMachineData(RegistrySettings.TrafficFilters, trafficFilters, RegistrySettings.GetProfileOffset(profileType));

            foreach (string filter in trafficFilters)
            {
                string filterOffset = RegistrySettings.GetProfileOffset(profileType) + "\\" + RegistrySettings.TrafficFilters + "\\" + filter;
                AccessRegistry.SaveMachineData(RegistrySettings.TrafficFilterEnabled, true, filterOffset);
                AccessRegistry.SaveMachineData(RegistrySettings.TrafficFilterProtocol, -1, filterOffset);
            }

            VPNProfileCreator pro = new VPNProfileCreator(profileType, false);
            pro.LoadFromRegistry();

            pro.Generate();

            string profile = pro.GetProfile();

            TestContext.WriteLine(pro.GetValidationFailures());
            TestContext.WriteLine(pro.GetValidationWarnings());
            TestContext.WriteLine(profile);
            Assert.IsTrue(!pro.ValidateFailed());
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationFailures()));
            Assert.IsFalse(string.IsNullOrWhiteSpace(pro.GetValidationWarnings())); //Traffic Filters should warn
            Assert.IsFalse(string.IsNullOrWhiteSpace(profile));

            ValidateXMLText(profile, "Servers", standardServerName);
            ValidateXMLText(profile, "NativeProfile", "RoutingPolicyType", "SplitTunnel", 1);
            ValidateXMLText(profile, "TrafficFilter", "RoutingPolicyType", "SplitTunnel", 0); //No Traffic Filters should exist
            ValidateXMLText(profile, "UserMethod", "Eap");
            ValidateXMLText(profile, "RequireCryptoBinding", "false");
            ValidateNPSList(profile, standardNPSServerList);
            ValidateRootThumbprintList(profile, standardRootCAList);
            ValidateIssuingThumbprintList(profile, standardIssuingCAList);
        }

        [DataTestMethod]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void UserLoadRegistryWithTrafficFilterSYSTEMAppId(ProfileType profileType)
        {
            List<string> trafficFilters = new List<string> {
                            "TF1",
                            "TF2" };
            CreateBasicUserProfileInRegistry(profileType);

            AccessRegistry.SaveMachineData(RegistrySettings.DisableCryptoBinding, true, RegistrySettings.GetProfileOffset(profileType));
            AccessRegistry.SaveMachineData(RegistrySettings.TrafficFilters, trafficFilters, RegistrySettings.GetProfileOffset(profileType));

            foreach (string filter in trafficFilters)
            {
                string filterOffset = RegistrySettings.GetProfileOffset(profileType) + "\\" + RegistrySettings.TrafficFilters + "\\" + filter;
                AccessRegistry.SaveMachineData(RegistrySettings.TrafficFilterEnabled, true, filterOffset);
                AccessRegistry.SaveMachineData(RegistrySettings.TrafficFilterProtocol, -1, filterOffset);
                AccessRegistry.SaveMachineData(RegistrySettings.TrafficFilterAppId, "SYSTEM", filterOffset);
            }

            VPNProfileCreator pro = new VPNProfileCreator(profileType, false);
            pro.LoadFromRegistry();

            pro.Generate();

            string profile = pro.GetProfile();

            TestContext.WriteLine(pro.GetValidationFailures());
            TestContext.WriteLine(pro.GetValidationWarnings());
            TestContext.WriteLine(profile);
            Assert.IsTrue(!pro.ValidateFailed());
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationFailures()));
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationWarnings())); //Traffic Filters should not warn
            Assert.IsFalse(string.IsNullOrWhiteSpace(profile));

            ValidateXMLText(profile, "Servers", standardServerName);
            ValidateXMLText(profile, "NativeProfile", "RoutingPolicyType", "SplitTunnel", 1);
            ValidateXMLText(profile, "TrafficFilter", "RoutingPolicyType", "SplitTunnel", 2); //Traffic Filters should exist
            ValidateXMLText(profile, "UserMethod", "Eap");
            ValidateXMLText(profile, "RequireCryptoBinding", "false");
            ValidateNPSList(profile, standardNPSServerList);
            ValidateRootThumbprintList(profile, standardRootCAList);
            ValidateIssuingThumbprintList(profile, standardIssuingCAList);
        }

        [DataTestMethod]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void UserLoadRegistryWithSingleLineOverride(ProfileType profileType)
        {
            CreateBasicUserProfileInRegistry(profileType);

            AccessRegistry.SaveMachineData(RegistrySettings.OverrideXML, "<VPNProfile><AlwaysOn>true</AlwaysOn><NativeProfile><Servers>aovpn.somewhere.net</Servers><RoutingPolicyType>SplitTunnel</RoutingPolicyType><NativeProtocolType>Ikev2</NativeProtocolType><DisableClassBasedDefaultRoute>true</DisableClassBasedDefaultRoute><Authentication><UserMethod>Eap</UserMethod><Eap><Configuration><EapHostConfig xmlns=\"http://www.microsoft.com/provisioning/EapHostConfig\"><EapMethod><Type xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">25</Type><VendorId xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">0</VendorId><VendorType xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">0</VendorType><AuthorId xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">0</AuthorId></EapMethod><Config xmlns=\"http://www.microsoft.com/provisioning/EapHostConfig\"><Eap xmlns=\"http://www.microsoft.com/provisioning/BaseEapConnectionPropertiesV1\"><Type>25</Type><EapType xmlns=\"http://www.microsoft.com/provisioning/MsPeapConnectionPropertiesV1\"><ServerValidation><DisableUserPromptForServerValidation>true</DisableUserPromptForServerValidation><ServerNames>NPS-01.somewhere.local</ServerNames><TrustedRootCA>AA BB CC DD EE FF 00 11 22 33 44 55 66 77 88 99 AA BB CC DD</TrustedRootCA></ServerValidation><FastReconnect>true</FastReconnect><InnerEapOptional>false</InnerEapOptional><Eap xmlns=\"http://www.microsoft.com/provisioning/BaseEapConnectionPropertiesV1\"><Type>13</Type><EapType xmlns=\"http://www.microsoft.com/provisioning/EapTlsConnectionPropertiesV1\"><CredentialsSource><CertificateStore><SimpleCertSelection>true</SimpleCertSelection></CertificateStore></CredentialsSource><ServerValidation><DisableUserPromptForServerValidation>true</DisableUserPromptForServerValidation><ServerNames>NPS-01.somewhere.local</ServerNames><TrustedRootCA>AA BB CC DD EE FF 00 11 22 33 44 55 66 77 88 99 AA BB CC DD</TrustedRootCA></ServerValidation><DifferentUsername>false</DifferentUsername><PerformServerValidation xmlns=\"http://www.microsoft.com/provisioning/EapTlsConnectionPropertiesV2\">true</PerformServerValidation><AcceptServerName xmlns=\"http://www.microsoft.com/provisioning/EapTlsConnectionPropertiesV2\">true</AcceptServerName><TLSExtensions xmlns=\"http://www.microsoft.com/provisioning/EapTlsConnectionPropertiesV2\"><FilteringInfo xmlns=\"http://www.microsoft.com/provisioning/EapTlsConnectionPropertiesV3\"><CAHashList Enabled=\"true\"><IssuerHash>CC DD EE FF 00 11 22 33 44 55 66 77 88 99 AA BB CC DD EE FF</IssuerHash></CAHashList></FilteringInfo></TLSExtensions></EapType></Eap><EnableQuarantineChecks>false</EnableQuarantineChecks><RequireCryptoBinding>false</RequireCryptoBinding><PeapExtensions><PerformServerValidation xmlns=\"http://www.microsoft.com/provisioning/MsPeapConnectionPropertiesV2\">true</PerformServerValidation><AcceptServerName xmlns=\"http://www.microsoft.com/provisioning/MsPeapConnectionPropertiesV2\">true</AcceptServerName></PeapExtensions></EapType></Eap></Config></EapHostConfig></Configuration></Eap></Authentication></NativeProfile></VPNProfile>", RegistrySettings.GetProfileOffset(profileType));

            VPNProfileCreator pro = new VPNProfileCreator(profileType, false);
            pro.LoadFromRegistry();

            pro.Generate();

            string profile = pro.GetProfile();

            TestContext.WriteLine(pro.GetValidationFailures());
            TestContext.WriteLine(pro.GetValidationWarnings());
            TestContext.WriteLine(profile);
            Assert.IsTrue(!pro.ValidateFailed());
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationFailures()));
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationWarnings()));
            Assert.IsFalse(string.IsNullOrWhiteSpace(profile));

            ValidateXMLText(profile, "Servers", "aovpn.somewhere.net"); //Should be the override value not the one picked from registry
            ValidateXMLText(profile, "RoutingPolicyType", "SplitTunnel");
            ValidateXMLText(profile, "UserMethod", "Eap");
            ValidateNPSList(profile, new List<string>() { "NPS-01.somewhere.local" });
            ValidateRootThumbprintList(profile, new List<string>() { "AABBCCDDEEFF00112233445566778899AABBCCDD" }); //Should be the override value not the one picked from registry
            ValidateIssuingThumbprintList(profile, new List<string>() { "CCDDEEFF00112233445566778899AABBCCDDEEFF" }); //Should be the override value not the one picked from registry
        }

        [DataTestMethod]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void UserLoadRegistryWithEKUOIDMissing(ProfileType profileType)
        {
            string EKUName = "AOVPN User";

            CreateBasicUserProfileInRegistry(profileType);

            AccessRegistry.SaveMachineData(RegistrySettings.LimitEKU, true, RegistrySettings.GetProfileOffset(profileType));
            AccessRegistry.SaveMachineData(RegistrySettings.EKUName, EKUName, RegistrySettings.GetProfileOffset(profileType));

            VPNProfileCreator pro = new VPNProfileCreator(profileType, false);
            pro.LoadFromRegistry();

            pro.Generate();

            string profile = pro.GetProfile();

            TestContext.WriteLine(pro.GetValidationFailures());
            TestContext.WriteLine(pro.GetValidationWarnings());
            TestContext.WriteLine(profile);
            Assert.IsTrue(pro.ValidateFailed());
            Assert.IsFalse(string.IsNullOrWhiteSpace(pro.GetValidationFailures()));
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationWarnings()));
            Assert.IsTrue(string.IsNullOrWhiteSpace(profile)); //Profile should not generate
        }

        [DataTestMethod]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void UserLoadRegistryWithEKUSettings(ProfileType profileType)
        {
            string EKUName = "AOVPN User";
            string EKUOID = "1.2.3.4.5";

            CreateBasicUserProfileInRegistry(profileType);

            AccessRegistry.SaveMachineData(RegistrySettings.LimitEKU, true, RegistrySettings.GetProfileOffset(profileType));
            AccessRegistry.SaveMachineData(RegistrySettings.EKUName, EKUName, RegistrySettings.GetProfileOffset(profileType));
            AccessRegistry.SaveMachineData(RegistrySettings.EKUOID, EKUOID, RegistrySettings.GetProfileOffset(profileType));

            VPNProfileCreator pro = new VPNProfileCreator(profileType, false);
            pro.LoadFromRegistry();

            pro.Generate();

            string profile = pro.GetProfile();

            TestContext.WriteLine(pro.GetValidationFailures());
            TestContext.WriteLine(pro.GetValidationWarnings());
            TestContext.WriteLine(profile);
            Assert.IsTrue(!pro.ValidateFailed());
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationFailures()));
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationWarnings()));
            Assert.IsFalse(string.IsNullOrWhiteSpace(profile));

            ValidateXMLText(profile, "Servers", standardServerName);
            ValidateXMLText(profile, "RoutingPolicyType", "SplitTunnel");
            ValidateXMLText(profile, "UserMethod", "Eap");
            ValidateNPSList(profile, standardNPSServerList);
            ValidateRootThumbprintList(profile, standardRootCAList);
            ValidateIssuingThumbprintList(profile, standardIssuingCAList);
            ValidateXMLTextDuplicated(profile, "EKUName", EKUName);
            ValidateXMLText(profile, "EKUOID", EKUOID);
        }

        [DataTestMethod]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void UserLoadRegistryWithEKUMissingEKUName(ProfileType profileType)
        {
            string EKUOID = "1.2.3.4.5";

            CreateBasicUserProfileInRegistry(profileType);

            AccessRegistry.SaveMachineData(RegistrySettings.LimitEKU, true, RegistrySettings.GetProfileOffset(profileType));
            AccessRegistry.SaveMachineData(RegistrySettings.EKUOID, EKUOID, RegistrySettings.GetProfileOffset(profileType));

            VPNProfileCreator pro = new VPNProfileCreator(profileType, false);
            pro.LoadFromRegistry();

            pro.Generate();

            string profile = pro.GetProfile();

            TestContext.WriteLine(pro.GetValidationFailures());
            TestContext.WriteLine(pro.GetValidationWarnings());
            TestContext.WriteLine(profile);
            Assert.IsTrue(pro.ValidateFailed());
            Assert.IsFalse(string.IsNullOrWhiteSpace(pro.GetValidationFailures()));
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationWarnings()));
            Assert.IsTrue(string.IsNullOrWhiteSpace(profile)); //Profile should not generate
        }

        [DataTestMethod]
        [DataRow("")]
        [DataRow(null)]
        [DataRow("%TEMP%")]
        [DataRow("bob")]
        [DataRow("C:\\Windows\\Temp")]
        [DataRow("C:\\Windows\\Temp\\profile.xml")]
        [DataRow("C:\\Windows\\Temp\\Profile2")]
        public void ProfileSavePath(string savePath)
        {
            CreateBasicUserProfileInRegistry(ProfileType.User);

            if (File.Exists(savePath))
            {
                File.Delete(savePath);
            }
            Assert.IsFalse(File.Exists(savePath));

            VPNProfileCreator pro = new VPNProfileCreator(ProfileType.User, false);
            pro.LoadFromRegistry();

            pro.Generate();

            string actualSavePath = pro.SaveProfile(savePath);

            string profile = pro.GetProfile();

            TestContext.WriteLine(pro.GetValidationFailures());
            TestContext.WriteLine(pro.GetValidationWarnings());
            TestContext.WriteLine(profile);
            Assert.IsTrue(!pro.ValidateFailed());
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationFailures()));
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationWarnings()));
            Assert.IsFalse(string.IsNullOrWhiteSpace(profile));
            if (!string.IsNullOrWhiteSpace(savePath))
            {
                Assert.IsTrue(File.Exists(actualSavePath));
                string fileText = File.ReadAllText(actualSavePath);
                Assert.AreEqual(profile, fileText);
                Assert.IsTrue(fileText.Contains("\n")); //Ensure pretty print is working
                Assert.IsTrue(fileText.Contains(" ")); //Ensure pretty print is working
            }
        }

        [DataTestMethod]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void UserLoadRegistryWithMinimalSettings(ProfileType profileType)
        {
            CreateBasicUserProfileInRegistry(profileType);

            VPNProfileCreator pro = new VPNProfileCreator(profileType, false);
            pro.LoadFromRegistry();

            pro.Generate();

            string profile = pro.GetProfile();

            TestContext.WriteLine(pro.GetValidationFailures());
            TestContext.WriteLine(pro.GetValidationWarnings());
            TestContext.WriteLine(profile);
            Assert.IsTrue(!pro.ValidateFailed());
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationFailures()));
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationWarnings()));
            Assert.IsFalse(string.IsNullOrWhiteSpace(profile));

            ValidateXMLText(profile, "Servers", standardServerName);
            ValidateXMLText(profile, "RoutingPolicyType", "SplitTunnel");
            ValidateXMLText(profile, "UserMethod", "Eap");
            ValidateNPSList(profile, standardNPSServerList);
            ValidateRootThumbprintList(profile, standardRootCAList);
            ValidateIssuingThumbprintList(profile, standardIssuingCAList);
            ValidateXMLTextIsMissing(profile, "DnsSuffix");
            ValidateXMLTextIsMissing(profile, "TrustedNetworkDetection");
        }

        [DataTestMethod]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void UserLoadRegistryWithExcludeRoutes(ProfileType profileType)
        {
            CreateBasicUserProfileInRegistry(profileType);
            AccessRegistry.SaveMachineData(RegistrySettings.RouteListExcludeKey, standardExcludeUserRouteList, RegistrySettings.GetProfileOffset(profileType));

            VPNProfileCreator pro = new VPNProfileCreator(profileType, false);
            pro.LoadFromRegistry();

            pro.Generate();

            string profile = pro.GetProfile();

            TestContext.WriteLine(pro.GetValidationFailures());
            TestContext.WriteLine(pro.GetValidationWarnings());
            TestContext.WriteLine(profile);
            Assert.IsTrue(!pro.ValidateFailed());
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationFailures()));
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationWarnings()));
            Assert.IsFalse(string.IsNullOrWhiteSpace(profile));

            ValidateXMLText(profile, "Servers", standardServerName);
            ValidateXMLText(profile, "RoutingPolicyType", "SplitTunnel");
            ValidateXMLText(profile, "UserMethod", "Eap");
            ValidateNPSList(profile, standardNPSServerList);
            ValidateRootThumbprintList(profile, standardRootCAList);
            ValidateIssuingThumbprintList(profile, standardIssuingCAList);
            ValidateXMLTextIsMissing(profile, "DnsSuffix");
            ValidateXMLTextIsMissing(profile, "TrustedNetworkDetection");
        }

        [DataTestMethod]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void UserLoadRegistryWithRouteMetric(ProfileType profileType)
        {
            CreateBasicUserProfileInRegistry(profileType);

            AccessRegistry.SaveMachineData(RegistrySettings.RouteMetric, 10, RegistrySettings.GetProfileOffset(profileType));

            VPNProfileCreator pro = new VPNProfileCreator(profileType, false);
            pro.LoadFromRegistry();

            pro.Generate();

            string profile = pro.GetProfile();

            TestContext.WriteLine(pro.GetValidationFailures());
            TestContext.WriteLine(pro.GetValidationWarnings());
            TestContext.WriteLine(profile);
            Assert.IsTrue(!pro.ValidateFailed());
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationFailures()));
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationWarnings()));
            Assert.IsFalse(string.IsNullOrWhiteSpace(profile));

            ValidateXMLText(profile, "Servers", standardServerName);
            ValidateXMLText(profile, "RoutingPolicyType", "SplitTunnel");
            ValidateXMLText(profile, "UserMethod", "Eap");
            ValidateNPSList(profile, standardNPSServerList);
            ValidateRootThumbprintList(profile, standardRootCAList);
            ValidateIssuingThumbprintList(profile, standardIssuingCAList);
            ValidateXMLTextIsMissing(profile, "DnsSuffix");
            ValidateXMLTextIsMissing(profile, "TrustedNetworkDetection");
        }

        [DataTestMethod]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void UserLoadRegistryWithExcludeRoutesAndRouteMetric(ProfileType profileType)
        {
            CreateBasicUserProfileInRegistry(profileType);

            AccessRegistry.SaveMachineData(RegistrySettings.RouteListExcludeKey, standardExcludeUserRouteList, RegistrySettings.GetProfileOffset(profileType));
            AccessRegistry.SaveMachineData(RegistrySettings.RouteMetric, 10, RegistrySettings.GetProfileOffset(profileType));


            VPNProfileCreator pro = new VPNProfileCreator(profileType, false);
            pro.LoadFromRegistry();

            pro.Generate();

            string profile = pro.GetProfile();

            TestContext.WriteLine(pro.GetValidationFailures());
            TestContext.WriteLine(pro.GetValidationWarnings());
            TestContext.WriteLine(profile);
            Assert.IsTrue(!pro.ValidateFailed());
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationFailures()));
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationWarnings()));
            Assert.IsFalse(string.IsNullOrWhiteSpace(profile));

            ValidateXMLText(profile, "Servers", standardServerName);
            ValidateXMLText(profile, "RoutingPolicyType", "SplitTunnel");
            ValidateXMLText(profile, "UserMethod", "Eap");
            ValidateNPSList(profile, standardNPSServerList);
            ValidateRootThumbprintList(profile, standardRootCAList);
            ValidateIssuingThumbprintList(profile, standardIssuingCAList);
            ValidateXMLTextIsMissing(profile, "DnsSuffix");
            ValidateXMLTextIsMissing(profile, "TrustedNetworkDetection");
        }

        [DataTestMethod]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void UserLoadRegistryWithPACProxySettings(ProfileType profileType)
        {
            string proxyValue = "https://testing.test.com:8080";
            CreateBasicUserProfileInRegistry(profileType);

            AccessRegistry.SaveMachineData(RegistrySettings.EnableProxy, true, RegistrySettings.GetProfileOffset(profileType));
            AccessRegistry.SaveMachineData(RegistrySettings.ProxyValue, proxyValue, RegistrySettings.GetProfileOffset(profileType));
            AccessRegistry.SaveMachineData(RegistrySettings.ProxyType, 1, RegistrySettings.GetProfileOffset(profileType));
            AccessRegistry.SaveMachineData(RegistrySettings.ForceTunnel, 1, RegistrySettings.GetProfileOffset(profileType));

            VPNProfileCreator pro = new VPNProfileCreator(profileType, false);
            pro.LoadFromRegistry();

            pro.Generate();

            string profile = pro.GetProfile();

            TestContext.WriteLine(pro.GetValidationFailures());
            TestContext.WriteLine(pro.GetValidationWarnings());
            TestContext.WriteLine(profile);
            Assert.IsTrue(!pro.ValidateFailed());
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationFailures()));
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationWarnings()));
            Assert.IsFalse(string.IsNullOrWhiteSpace(profile));

            ValidateXMLText(profile, "Servers", standardServerName);
            ValidateXMLText(profile, "RoutingPolicyType", "ForceTunnel");
            ValidateXMLText(profile, "UserMethod", "Eap");
            ValidateXMLText(profile, "AutoConfigUrl", proxyValue);
            ValidateNPSList(profile, standardNPSServerList);
            ValidateRootThumbprintList(profile, standardRootCAList);
            ValidateIssuingThumbprintList(profile, standardIssuingCAList);
            ValidateXMLTextIsMissing(profile, "DnsSuffix");
            ValidateXMLTextIsMissing(profile, "TrustedNetworkDetection");
        }

        [DataTestMethod]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void UserLoadRegistryWithServerProxySettingsShouldWarn(ProfileType profileType)
        {
            string proxyValue = "https://testing.test.com/this.pac";

            CreateBasicUserProfileInRegistry(profileType);

            AccessRegistry.SaveMachineData(RegistrySettings.EnableProxy, true, RegistrySettings.GetProfileOffset(profileType));
            AccessRegistry.SaveMachineData(RegistrySettings.ProxyValue, proxyValue, RegistrySettings.GetProfileOffset(profileType));
            AccessRegistry.SaveMachineData(RegistrySettings.ProxyType, 2, RegistrySettings.GetProfileOffset(profileType));

            VPNProfileCreator pro = new VPNProfileCreator(profileType, false);
            pro.LoadFromRegistry();

            pro.Generate();

            string profile = pro.GetProfile();

            TestContext.WriteLine(pro.GetValidationFailures());
            TestContext.WriteLine(pro.GetValidationWarnings());
            TestContext.WriteLine(profile);
            Assert.IsTrue(!pro.ValidateFailed());
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationFailures()));
            Assert.IsFalse(string.IsNullOrWhiteSpace(pro.GetValidationWarnings())); //Should Warn
            Assert.IsFalse(string.IsNullOrWhiteSpace(profile));

            ValidateXMLText(profile, "Servers", standardServerName);
            ValidateXMLText(profile, "RoutingPolicyType", "SplitTunnel");
            ValidateXMLText(profile, "UserMethod", "Eap");
            ValidateNPSList(profile, standardNPSServerList);
            ValidateRootThumbprintList(profile, standardRootCAList);
            ValidateIssuingThumbprintList(profile, standardIssuingCAList);
            ValidateXMLTextIsMissing(profile, "DnsSuffix");
            ValidateXMLTextIsMissing(profile, "Proxy"); //Proxy Settings shouldn't be in profile
            ValidateXMLTextIsMissing(profile, "TrustedNetworkDetection");
        }

        [DataTestMethod]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void UserLoadRegistryWithServerProxySettings(ProfileType profileType)
        {
            string proxyValue = "https://testing.test.com/this.pac";

            CreateBasicUserProfileInRegistry(profileType);

            AccessRegistry.SaveMachineData(RegistrySettings.EnableProxy, true, RegistrySettings.GetProfileOffset(profileType));
            AccessRegistry.SaveMachineData(RegistrySettings.ProxyValue, proxyValue, RegistrySettings.GetProfileOffset(profileType));
            AccessRegistry.SaveMachineData(RegistrySettings.ProxyType, 2, RegistrySettings.GetProfileOffset(profileType));
            AccessRegistry.SaveMachineData(RegistrySettings.ForceTunnel, 1, RegistrySettings.GetProfileOffset(profileType));

            VPNProfileCreator pro = new VPNProfileCreator(profileType, false);
            pro.LoadFromRegistry();

            pro.Generate();

            string profile = pro.GetProfile();

            TestContext.WriteLine(pro.GetValidationFailures());
            TestContext.WriteLine(pro.GetValidationWarnings());
            TestContext.WriteLine(profile);
            Assert.IsTrue(!pro.ValidateFailed());
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationFailures()));
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationWarnings()));
            Assert.IsFalse(string.IsNullOrWhiteSpace(profile));

            ValidateXMLText(profile, "Servers", standardServerName);
            ValidateXMLText(profile, "RoutingPolicyType", "ForceTunnel");
            ValidateXMLText(profile, "UserMethod", "Eap");
            ValidateXMLText(profile, "Server", proxyValue);
            ValidateNPSList(profile, standardNPSServerList);
            ValidateRootThumbprintList(profile, standardRootCAList);
            ValidateIssuingThumbprintList(profile, standardIssuingCAList);
            ValidateXMLTextIsMissing(profile, "DnsSuffix");
            ValidateXMLTextIsMissing(profile, "TrustedNetworkDetection");
        }

        [DataTestMethod]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void UserLoadRegistryWithDomainInfoSettings(ProfileType profileType)
        {
            Dictionary<string, string> domainInfoList = new Dictionary<string, string>
                        {
                            { ".", "192.168.0.1,192.168.0.2" },
                            { ".example.com", "192.168.0.1,192.168.0.2" },
                            { "www.example.com", "" }
                        };
            CreateBasicUserProfileInRegistry(profileType);

            AccessRegistry.SaveMachineData(RegistrySettings.DomainNameInfoKey, domainInfoList, RegistrySettings.GetProfileOffset(profileType));

            VPNProfileCreator pro = new VPNProfileCreator(profileType, false);
            pro.LoadFromRegistry();

            pro.Generate();

            string profile = pro.GetProfile();

            TestContext.WriteLine(pro.GetValidationFailures());
            TestContext.WriteLine(pro.GetValidationWarnings());
            TestContext.WriteLine(profile);
            Assert.IsFalse(pro.ValidateFailed());
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationFailures()));
            Assert.AreEqual(pro.GetValidationWarnings().Trim().Split(Environment.NewLine.ToCharArray()).Count(), 1); //Check that there was only the 1 validation warning
            Assert.IsFalse(string.IsNullOrWhiteSpace(profile));

            ValidateXMLText(profile, "Servers", standardServerName);
            ValidateXMLText(profile, "RoutingPolicyType", "SplitTunnel");
            ValidateXMLText(profile, "UserMethod", "Eap");
            ValidateNPSList(profile, standardNPSServerList);
            ValidateRootThumbprintList(profile, standardRootCAList);
            ValidateIssuingThumbprintList(profile, standardIssuingCAList);
            ValidateXMLTextIsMissing(profile, "DnsSuffix");
            ValidateXMLTextIsMissing(profile, "TrustedNetworkDetection");

            VPNProfile profileObj = new CSPProfile(profile, pro.GetProfileName());
            Assert.AreEqual(profileObj.DomainNameInformation.Count, domainInfoList.Count);
            foreach (KeyValuePair<string, string> item in domainInfoList)
            {
                Assert.IsTrue(profileObj.DomainNameInformation.Contains(new DomainNameInformation(item.Key, item.Value)));
            }
        }

        [DataTestMethod]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void UserLoadRegistryWithExcludeValues(ProfileType profileType)
        {
            Dictionary<string, string> domainInfoList = new Dictionary<string, string>
                        {
                            { ".", "192.168.0.1,192.168.0.2" },
                            { ".example.com", "192.168.0.1,192.168.0.2" },
                            { "www.example.com", "" },
                            { "test.example.com", "<EMPTY>" },
                            { "www3.example.com", "<Empty>" },
                            { "remote.example.com", "<empty>" }
                        };
            CreateBasicUserProfileInRegistry(profileType);

            AccessRegistry.SaveMachineData(RegistrySettings.DomainNameInfoKey, domainInfoList, RegistrySettings.GetProfileOffset(profileType));

            VPNProfileCreator pro = new VPNProfileCreator(profileType, false);
            pro.LoadFromRegistry();

            pro.Generate();

            string profile = pro.GetProfile();

            TestContext.WriteLine(pro.GetValidationFailures());
            TestContext.WriteLine(pro.GetValidationWarnings());
            TestContext.WriteLine(profile);
            Assert.IsFalse(pro.ValidateFailed());
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationFailures()));
            Assert.AreEqual(pro.GetValidationWarnings().Trim().Split(Environment.NewLine.ToCharArray()).Count(), 1); //Check that there was only the 1 validation warning
            Assert.IsFalse(string.IsNullOrWhiteSpace(profile));

            ValidateXMLText(profile, "Servers", standardServerName);
            ValidateXMLText(profile, "RoutingPolicyType", "SplitTunnel");
            ValidateXMLText(profile, "UserMethod", "Eap");
            ValidateNPSList(profile, standardNPSServerList);
            ValidateRootThumbprintList(profile, standardRootCAList);
            ValidateIssuingThumbprintList(profile, standardIssuingCAList);
            ValidateXMLTextIsMissing(profile, "DnsSuffix");
            ValidateXMLTextIsMissing(profile, "TrustedNetworkDetection");

            VPNProfile profileObj = new CSPProfile(profile, pro.GetProfileName());
            Assert.AreEqual(profileObj.DomainNameInformation.Count, domainInfoList.Count);
            foreach (KeyValuePair<string, string> item in domainInfoList)
            {
                string value = Regex.Replace(item.Value, "<EMPTY>", "", RegexOptions.IgnoreCase);
                Assert.IsTrue(profileObj.DomainNameInformation.Contains(new DomainNameInformation(item.Key, value)));
            }
        }

        [DataTestMethod]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void UserLoadRegistryWithO365ExclusionSettings(ProfileType profileType)
        {
            CreateBasicUserProfileInRegistry(profileType);

            AccessRegistry.SaveMachineData(RegistrySettings.ExcludeOffice365, true, RegistrySettings.GetProfileOffset(profileType));

            VPNProfileCreator pro = new VPNProfileCreator(profileType, false);
            pro.LoadFromRegistry();

            pro.Generate();

            string profile = pro.GetProfile();

            TestContext.WriteLine(pro.GetValidationFailures());
            TestContext.WriteLine(pro.GetValidationWarnings());
            TestContext.WriteLine(profile);
            Assert.IsTrue(!pro.ValidateFailed());
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationFailures()));
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationWarnings()));
            Assert.IsFalse(string.IsNullOrWhiteSpace(profile));

            ValidateXMLText(profile, "Servers", standardServerName);
            ValidateXMLText(profile, "RoutingPolicyType", "SplitTunnel");
            ValidateXMLText(profile, "UserMethod", "Eap");
            ValidateNPSList(profile, standardNPSServerList);
            ValidateRootThumbprintList(profile, standardRootCAList);
            ValidateIssuingThumbprintList(profile, standardIssuingCAList);
            ValidateXMLTextIsMissing(profile, "DnsSuffix");
            ValidateXMLTextIsMissing(profile, "TrustedNetworkDetection");

            VPNProfile profileObj = new CSPProfile(profile, pro.GetProfileName());
            Assert.IsTrue(profileObj.RouteList.Where(r => !r.ExclusionRoute).ToList().Count == standardUserRouteList.Count);
            Assert.IsFalse(profileObj.RouteList.Where(r => r.ExclusionRoute).ToList().Count == 0);
        }

        [DataTestMethod]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void UserLoadRegistryWithO365InitialFailSettings(ProfileType profileType)
        {
            CreateBasicUserProfileInRegistry(profileType);

            AccessRegistry.SaveMachineData(RegistrySettings.ExcludeOffice365, true, RegistrySettings.GetProfileOffset(profileType));

            PrivateType type = new PrivateType(typeof(HttpService));

            type.SetStaticField("breakNetwork", BindingFlags.NonPublic, true);

            VPNProfileCreator pro = new VPNProfileCreator(profileType, false);
            pro.LoadFromRegistry();

            pro.Generate();

            string profile = pro.GetProfile();

            TestContext.WriteLine(pro.GetValidationFailures());
            TestContext.WriteLine(pro.GetValidationWarnings());
            TestContext.WriteLine(profile);
            Assert.IsTrue(!pro.ValidateFailed());
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationFailures()));
            Assert.IsFalse(string.IsNullOrWhiteSpace(pro.GetValidationWarnings()));
            Assert.IsFalse(string.IsNullOrWhiteSpace(profile));

            ValidateXMLText(profile, "Servers", standardServerName);
            ValidateXMLText(profile, "RoutingPolicyType", "SplitTunnel");
            ValidateXMLText(profile, "UserMethod", "Eap");
            ValidateNPSList(profile, standardNPSServerList);
            ValidateRootThumbprintList(profile, standardRootCAList);
            ValidateIssuingThumbprintList(profile, standardIssuingCAList);
            ValidateXMLTextIsMissing(profile, "DnsSuffix");
            ValidateXMLTextIsMissing(profile, "TrustedNetworkDetection");

            VPNProfile profileObj = new CSPProfile(profile, pro.GetProfileName());
            Assert.IsTrue(profileObj.RouteList.Where(r => !r.ExclusionRoute).ToList().Count == standardUserRouteList.Count);
            Assert.IsTrue(profileObj.RouteList.Where(r => r.ExclusionRoute).ToList().Count == 0);
        }

        [DataTestMethod]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void UserLoadRegistryWithO365FailOnSecondAttemptSettings(ProfileType profileType)
        {
            CreateBasicUserProfileInRegistry(profileType);

            AccessRegistry.SaveMachineData(RegistrySettings.ExcludeOffice365, true, RegistrySettings.GetProfileOffset(profileType));

            PrivateType type = new PrivateType(typeof(HttpService));

            VPNProfileCreator pro = new VPNProfileCreator(profileType, false);

            //Call 1

            pro.LoadFromRegistry();

            pro.Generate();

            //Trigger the HttpService to throw errors on every connection attempt
            type.SetStaticField("breakNetwork", BindingFlags.NonPublic, true);

            //Call 2
            pro.LoadFromRegistry();

            pro.Generate();

            string profile = pro.GetProfile();

            TestContext.WriteLine(pro.GetValidationFailures());
            TestContext.WriteLine(pro.GetValidationWarnings());
            TestContext.WriteLine(profile);
            Assert.IsTrue(!pro.ValidateFailed());
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationFailures()));
            Assert.IsFalse(string.IsNullOrWhiteSpace(pro.GetValidationWarnings()));
            Assert.IsFalse(string.IsNullOrWhiteSpace(profile));

            ValidateXMLText(profile, "Servers", standardServerName);
            ValidateXMLText(profile, "RoutingPolicyType", "SplitTunnel");
            ValidateXMLText(profile, "UserMethod", "Eap");
            ValidateNPSList(profile, standardNPSServerList);
            ValidateRootThumbprintList(profile, standardRootCAList);
            ValidateIssuingThumbprintList(profile, standardIssuingCAList);
            ValidateXMLTextIsMissing(profile, "DnsSuffix");
            ValidateXMLTextIsMissing(profile, "TrustedNetworkDetection");

            VPNProfile profileObj = new CSPProfile(profile, pro.GetProfileName());
            Assert.IsTrue(profileObj.RouteList.Where(r => !r.ExclusionRoute).ToList().Count == standardUserRouteList.Count);
            Assert.IsFalse(profileObj.RouteList.Where(r => r.ExclusionRoute).ToList().Count == 0);
        }

        [DataTestMethod]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void UserLoadRegistryWithDNSIncludeSettings(ProfileType profileType)
        {
            CreateBasicUserProfileInRegistry(profileType);
            AccessRegistry.SaveMachineData(RegistrySettings.DNSRouteList, new Dictionary<string, string>() {
                                                            { "www.google.co.uk", "Search Service" },
                                                            { "www.example.com", "Example Website" } }, RegistrySettings.GetProfileOffset(profileType));

            VPNProfileCreator pro = new VPNProfileCreator(profileType, false);
            pro.LoadFromRegistry();

            pro.Generate();

            string profile = pro.GetProfile();

            TestContext.WriteLine(pro.GetValidationFailures());
            TestContext.WriteLine(pro.GetValidationWarnings());
            TestContext.WriteLine(profile);
            Assert.IsTrue(!pro.ValidateFailed());
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationFailures()));
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationWarnings()));
            Assert.IsFalse(string.IsNullOrWhiteSpace(profile));

            ValidateXMLText(profile, "Servers", standardServerName);
            ValidateXMLText(profile, "RoutingPolicyType", "SplitTunnel");
            ValidateXMLText(profile, "UserMethod", "Eap");
            ValidateNPSList(profile, standardNPSServerList);
            ValidateRootThumbprintList(profile, standardRootCAList);
            ValidateIssuingThumbprintList(profile, standardIssuingCAList);
            ValidateXMLTextIsMissing(profile, "DnsSuffix");
            ValidateXMLTextIsMissing(profile, "TrustedNetworkDetection");

            VPNProfile profileObj = new CSPProfile(profile, pro.GetProfileName());
            Assert.AreEqual(standardUserRouteList.Count + 3, profileObj.RouteList.Count);
            Assert.AreEqual(0, profileObj.RouteList.Where(r => r.ExclusionRoute).ToList().Count);
        }

        [DataTestMethod]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void UserLoadRegistryWithDNSExcludeSettings(ProfileType profileType)
        {
            CreateBasicUserProfileInRegistry(profileType);
            AccessRegistry.SaveMachineData(RegistrySettings.DNSExcludeRouteList, new Dictionary<string, string>() {
                                                            { "www.google.co.uk", "Search Service" },
                                                            { "www.example.com", "Example Website" } }, RegistrySettings.GetProfileOffset(profileType));

            VPNProfileCreator pro = new VPNProfileCreator(profileType, false);
            pro.LoadFromRegistry();

            pro.Generate();

            string profile = pro.GetProfile();

            TestContext.WriteLine(pro.GetValidationFailures());
            TestContext.WriteLine(pro.GetValidationWarnings());
            TestContext.WriteLine(profile);
            Assert.IsTrue(!pro.ValidateFailed());
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationFailures()));
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationWarnings()));
            Assert.IsFalse(string.IsNullOrWhiteSpace(profile));

            ValidateXMLText(profile, "Servers", standardServerName);
            ValidateXMLText(profile, "RoutingPolicyType", "SplitTunnel");
            ValidateXMLText(profile, "UserMethod", "Eap");
            ValidateNPSList(profile, standardNPSServerList);
            ValidateRootThumbprintList(profile, standardRootCAList);
            ValidateIssuingThumbprintList(profile, standardIssuingCAList);
            ValidateXMLTextIsMissing(profile, "DnsSuffix");
            ValidateXMLTextIsMissing(profile, "TrustedNetworkDetection");

            VPNProfile profileObj = new CSPProfile(profile, pro.GetProfileName());
            Assert.AreEqual(standardUserRouteList.Count + 3, profileObj.RouteList.Count);
            Assert.AreEqual(0, profileObj.RouteList.Where(r => r.ExclusionRoute).ToList().Count);
        }

        [DataTestMethod]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void UserLoadRegistryWithDNSIncludeInitialFailSettings(ProfileType profileType)
        {
            CreateBasicUserProfileInRegistry(profileType);
            AccessRegistry.SaveMachineData(RegistrySettings.ForceTunnel, 1, RegistrySettings.GetProfileOffset(profileType));
            AccessRegistry.SaveMachineData(RegistrySettings.DNSRouteList, new Dictionary<string, string>() {
                                                            { "www.google.co.uk", "Search Service" },
                                                            { "www.example.com", "Example Website" } }, RegistrySettings.GetProfileOffset(profileType));

            PrivateType type = new PrivateType(typeof(HttpService));

            //Trigger the HttpService to throw errors on every connection attempt
            type.SetStaticField("breakNetwork", BindingFlags.NonPublic, true);

            VPNProfileCreator pro = new VPNProfileCreator(profileType, false);
            pro.LoadFromRegistry();

            pro.Generate();

            string profile = pro.GetProfile();

            TestContext.WriteLine(pro.GetValidationFailures());
            TestContext.WriteLine(pro.GetValidationWarnings());
            TestContext.WriteLine(profile);
            Assert.IsTrue(!pro.ValidateFailed());
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationFailures()));
            Assert.IsFalse(string.IsNullOrWhiteSpace(pro.GetValidationWarnings()));
            Assert.IsFalse(string.IsNullOrWhiteSpace(profile));

            ValidateXMLText(profile, "Servers", standardServerName);
            ValidateXMLText(profile, "RoutingPolicyType", "SplitTunnel");
            ValidateXMLText(profile, "UserMethod", "Eap");
            ValidateNPSList(profile, standardNPSServerList);
            ValidateRootThumbprintList(profile, standardRootCAList);
            ValidateIssuingThumbprintList(profile, standardIssuingCAList);
            ValidateXMLTextIsMissing(profile, "DnsSuffix");
            ValidateXMLTextIsMissing(profile, "TrustedNetworkDetection");

            VPNProfile profileObj = new CSPProfile(profile, pro.GetProfileName());
            Assert.AreEqual(standardUserRouteList.Count, profileObj.RouteList.Count);
            Assert.AreEqual(0, profileObj.RouteList.Where(r => r.ExclusionRoute).ToList().Count);
        }

        [DataTestMethod]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void UserLoadRegistryWithDNSIncludeFailOnSecondAttemptSettings(ProfileType profileType)
        {
            Dictionary<string, string> DNSList = new Dictionary<string, string>() {
                                                            { "www.google.co.uk", "Search Service" },
                                                            { "www.example.com", "Example Website" } };

            CreateBasicUserProfileInRegistry(profileType);

            AccessRegistry.SaveMachineData(RegistrySettings.DNSRouteList, DNSList, RegistrySettings.GetProfileOffset(profileType));

            PrivateType type = new PrivateType(typeof(HttpService));

            //Trigger the HttpService to throw errors on every connection attempt
            type.SetStaticField("breakNetwork", BindingFlags.NonPublic, true);

            VPNProfileCreator pro = new VPNProfileCreator(profileType, false);

            //Call 1

            pro.LoadFromRegistry();

            pro.Generate();

            TestContext.WriteLine(pro.GetValidationFailures());
            TestContext.WriteLine(pro.GetValidationWarnings());
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationFailures()));
            Assert.IsFalse(string.IsNullOrWhiteSpace(pro.GetValidationWarnings()));

            //Re-enable the network setup
            type.SetStaticField("breakNetwork", BindingFlags.NonPublic, false);

            //Call 2
            pro.LoadFromRegistry();

            pro.Generate();

            string profile = pro.GetProfile();

            TestContext.WriteLine(pro.GetValidationFailures());
            TestContext.WriteLine(pro.GetValidationWarnings());
            TestContext.WriteLine(profile);
            Assert.IsTrue(!pro.ValidateFailed());
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationFailures()));
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationWarnings()));
            Assert.IsFalse(string.IsNullOrWhiteSpace(profile));

            ValidateXMLText(profile, "Servers", standardServerName);
            ValidateXMLText(profile, "RoutingPolicyType", "SplitTunnel");
            ValidateXMLText(profile, "UserMethod", "Eap");
            ValidateNPSList(profile, standardNPSServerList);
            ValidateRootThumbprintList(profile, standardRootCAList);
            ValidateIssuingThumbprintList(profile, standardIssuingCAList);
            ValidateXMLTextIsMissing(profile, "DnsSuffix");
            ValidateXMLTextIsMissing(profile, "TrustedNetworkDetection");

            VPNProfile profileObj = new CSPProfile(profile, pro.GetProfileName());
            Assert.AreEqual(standardUserRouteList.Count + 3, profileObj.RouteList.Where(r => !r.ExclusionRoute).ToList().Count);
            Assert.AreEqual(0, profileObj.RouteList.Where(r => r.ExclusionRoute).ToList().Count);
        }

        [TestMethod]
        public void DeviceLoadRegistryWithMinimalSettings()
        {
            CreateBasicDeviceProfileInRegistry();

            VPNProfileCreator pro = new VPNProfileCreator(ProfileType.Machine, false);
            pro.LoadFromRegistry();

            pro.Generate();

            string profile = pro.GetProfile();

            TestContext.WriteLine(pro.GetValidationFailures());
            TestContext.WriteLine(pro.GetValidationWarnings());
            TestContext.WriteLine(profile);
            Assert.IsTrue(!pro.ValidateFailed());
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationFailures()));
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationWarnings()));
            Assert.IsFalse(string.IsNullOrWhiteSpace(profile));
            Assert.AreEqual(pro.GetProfileName(), "AOVPN Device Profile");
            ValidateXMLText(profile, "Servers", standardServerName);
            ValidateXMLText(profile, "RoutingPolicyType", "SplitTunnel");
            ValidateXMLText(profile, "MachineMethod", "Certificate");
            ValidateXMLTextIsMissing(profile, "UserMethod");
        }

        [TestMethod]
        public void DeviceLoadRegistryWithRouteMetric()
        {
            CreateBasicDeviceProfileInRegistry();

            AccessRegistry.SaveMachineData(RegistrySettings.RouteMetric, 10, RegistrySettings.GetProfileOffset(ProfileType.Machine));

            VPNProfileCreator pro = new VPNProfileCreator(ProfileType.Machine, false);
            pro.LoadFromRegistry();

            pro.Generate();

            string profile = pro.GetProfile();

            TestContext.WriteLine(pro.GetValidationFailures());
            TestContext.WriteLine(pro.GetValidationWarnings());
            TestContext.WriteLine(profile);
            Assert.IsTrue(!pro.ValidateFailed());
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationFailures()));
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationWarnings()));
            Assert.IsFalse(string.IsNullOrWhiteSpace(profile));
            Assert.AreEqual(pro.GetProfileName(), "AOVPN Device Profile");
            ValidateXMLText(profile, "Servers", standardServerName);
            ValidateXMLText(profile, "RoutingPolicyType", "SplitTunnel");
            ValidateXMLText(profile, "MachineMethod", "Certificate");
            ValidateXMLTextIsMissing(profile, "UserMethod");
        }

        [TestMethod]
        public void DeviceLoadRegistryWithDomainNameInfoSettings()
        {

            Dictionary<string, string> domainInfoList = new Dictionary<string, string> {
                                                            { ".", "192.168.0.1, 192.168.0.2" },
                                                            { ".example.com", "192.168.0.1,192.168.0.2" },
                                                            { "www.example.com", "" }
                                                        };
            List<string> SuffixList = new List<string>
                        {
                            "test.local",
                            "testing.net",
                            "somewherelse.partner"
                        };

            CreateBasicDeviceProfileInRegistry();

            AccessRegistry.SaveMachineData(RegistrySettings.DNSSuffixKey, SuffixList, RegistrySettings.GetProfileOffset(ProfileType.Machine));
            AccessRegistry.SaveMachineData(RegistrySettings.TrustedNetworksKey, SuffixList, RegistrySettings.GetProfileOffset(ProfileType.Machine));
            AccessRegistry.SaveMachineData(RegistrySettings.DomainNameInfoKey, domainInfoList, RegistrySettings.GetProfileOffset(ProfileType.Machine));


            VPNProfileCreator pro = new VPNProfileCreator(ProfileType.Machine, false);
            pro.LoadFromRegistry();

            pro.Generate();

            string profile = pro.GetProfile();

            TestContext.WriteLine(pro.GetValidationFailures());
            TestContext.WriteLine(pro.GetValidationWarnings());
            TestContext.WriteLine(profile);
            Assert.IsTrue(!pro.ValidateFailed());
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationFailures()));
            Assert.AreEqual(pro.GetValidationWarnings().Trim().Split(Environment.NewLine.ToCharArray()).Count(), 1); //Check that there was only the 1 validation warning
            Assert.IsFalse(string.IsNullOrWhiteSpace(profile));
            Assert.AreEqual(pro.GetProfileName(), "AOVPN Device Profile");
            ValidateXMLText(profile, "Servers", standardServerName);
            ValidateXMLText(profile, "RoutingPolicyType", "SplitTunnel");
            ValidateXMLText(profile, "MachineMethod", "Certificate");
            ValidateXMLTextIsMissing(profile, "UserMethod");

            VPNProfile profileObj = new CSPProfile(profile, pro.GetProfileName());
            Assert.AreEqual(domainInfoList.Count, profileObj.DomainNameInformation.Count);
            foreach (KeyValuePair<string, string> item in domainInfoList)
            {
                Assert.IsTrue(profileObj.DomainNameInformation.Contains(new DomainNameInformation(item.Key, item.Value.Replace(" ", ""))));
            }
        }

        [TestMethod]
        public void DeviceLoadRegistryForceTunnel()
        {
            CreateBasicDeviceProfileInRegistry();

            AccessRegistry.SaveMachineData(RegistrySettings.ForceTunnel, true, RegistrySettings.GetProfileOffset(ProfileType.Machine));

            VPNProfileCreator pro = new VPNProfileCreator(ProfileType.Machine, false);
            pro.LoadFromRegistry();

            pro.Generate();

            string profile = pro.GetProfile();

            TestContext.WriteLine(pro.GetValidationFailures());
            TestContext.WriteLine(pro.GetValidationWarnings());
            TestContext.WriteLine(profile);
            Assert.IsFalse(pro.ValidateFailed());
            Assert.IsTrue(string.IsNullOrWhiteSpace(pro.GetValidationFailures()));
            Assert.IsFalse(string.IsNullOrWhiteSpace(pro.GetValidationWarnings()));
            Assert.IsTrue(pro.ValidateWarnings());
            Assert.IsFalse(string.IsNullOrWhiteSpace(profile));

            ValidateXMLText(profile, "Servers", standardServerName);
            ValidateXMLText(profile, "RoutingPolicyType", "SplitTunnel");
            ValidateXMLText(profile, "MachineMethod", "Certificate");
            ValidateXMLTextIsMissing(profile, "UserMethod");
        }
    }
}