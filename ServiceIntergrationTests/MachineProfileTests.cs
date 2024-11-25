using System.Collections.Generic;
using DPCLibrary.Enums;
using DPCService.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DPCLibrary.Models;
using System;
using System.Linq;
using System.IO;
using DPCLibrary.Utils;
using DPCService.Core;

namespace ServiceIntegrationTests
{
    [TestClass]
    [TestCategory("Administrator")]
    public class MachineProfileTests
    {
        /// <summary>
        ///  Gets or sets the test context which provides
        ///  information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext { get; set; }

        //Maintain a single, consistent view of the OS State across Tests to help with clearing profiles
        private SharedData sharedData;

        [TestInitialize]
        public void PreTestInitialize()
        {
            sharedData = new SharedData(60, true, false, TestContext.CancellationTokenSource.Token);
            HelperFunctions.ClearETWEvents();
            HelperFunctions.ClearProfiles(sharedData);
            HelperFunctions.ClearRegistry();
        }

        [TestCleanup]
        public void PostTestCleanup()
        {
            bool skipProfileErrors = HelperFunctions.ClearProfiles(sharedData);
            if (skipProfileErrors)
            {
                HelperFunctions.ClearSpecificEventId(1158); //Failed to Delete Profile
            }
            HelperFunctions.CheckNoErrors(TestContext);
        }

        [DataTestMethod]
        [TestCategory("MachineTunnel")]
        [TestCategory("TrafficFilters")]
        public void BasicMachineProfileWithTrafficFilters()
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(ProfileType.Machine, false);
            profile.LoadMachineProfile(profileName,
                    TunnelType.SplitTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    routeList: new Dictionary<string, string>
                        {
                            { "10.0.0.0/8", "Server Network" }
                        },
                    trafficFilters: new List<TrafficFilter>() { new TrafficFilter("TF2") {
                                                                    AppId = "C:\\Test\\Test.exe",
                                                                    Protocol = Protocol.TCP
                                                                },
                                                                new TrafficFilter("TF3") {
                                                                    AppId = "C:\\Test\\Test2.exe",
                                                                    Protocol = Protocol.TCP,
                                                                    RemoteAddresses = "10.0.0.1, 192.168.0.0/16",
                                                                    Direction = ProtocolDirection.Outbound,
                                                                    RoutingPolicyType = TunnelType.SplitTunnel
                                                                }
                    }
                );
            profile.Generate();
            TestContext.WriteLine(profile.GetValidationFailures());
            TestContext.WriteLine(profile.GetValidationWarnings());
            Assert.IsFalse(profile.ValidateFailed());
            Assert.IsFalse(profile.ValidateWarnings());

            sharedData.AddProfileUpdate(profile.GetProfileUpdate());

            sharedData.HandleProfileUpdates();
            Assert.IsTrue(HelperFunctions.CheckProfileExists(profileName));

            //Assert that all updates have happened
            Assert.AreEqual(0, sharedData.PendingUpdates());

            //Check update was fully accepted by WMI
            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
        }

        [DataTestMethod]
        [TestCategory("MachineTunnel")]
        [TestCategory("TrafficFilters")]
        public void BasicMachineProfileWithTrafficFiltersProtocolOnly()
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(ProfileType.Machine, false);
            profile.LoadMachineProfile(profileName,
                    TunnelType.SplitTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    routeList: new Dictionary<string, string>
                        {
                            { "10.0.0.0/8", "Server Network" }
                        },
                    trafficFilters: new List<TrafficFilter>() { new TrafficFilter("TF2") {
                                                                    Protocol = Protocol.TCP
                                                                }
                    }
                );
            profile.Generate();
            TestContext.WriteLine(profile.GetValidationFailures());
            TestContext.WriteLine(profile.GetValidationWarnings());
            Assert.IsFalse(profile.ValidateFailed());
            Assert.IsFalse(profile.ValidateWarnings());

            sharedData.AddProfileUpdate(profile.GetProfileUpdate());

            sharedData.HandleProfileUpdates();
            Assert.IsTrue(HelperFunctions.CheckProfileExists(profileName));

            //Assert that all updates have happened
            Assert.AreEqual(0, sharedData.PendingUpdates());

            //Check update was fully accepted by WMI
            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
        }

        [DataTestMethod]
        [TestCategory("MachineTunnel")]
        [TestCategory("TrafficFilters")]
        public void BasicMachineProfileWithTrafficFiltersPortAllowICMP()
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(ProfileType.Machine, false);
            profile.LoadMachineProfile(profileName,
                    TunnelType.SplitTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    routeList: new Dictionary<string, string>
                        {
                            { "10.0.0.0/8", "Server Network" }
                        },
                    trafficFilters: new List<TrafficFilter>() { new TrafficFilter("TF2") {
                                                                    Protocol = Protocol.ICMP
                                                                }
                    }
                );
            profile.Generate();
            TestContext.WriteLine(profile.GetValidationFailures());
            TestContext.WriteLine(profile.GetValidationWarnings());
            Assert.IsFalse(profile.ValidateFailed());
            Assert.IsFalse(profile.ValidateWarnings());

            sharedData.AddProfileUpdate(profile.GetProfileUpdate());

            sharedData.HandleProfileUpdates();
            Assert.IsTrue(HelperFunctions.CheckProfileExists(profileName));

            //Assert that all updates have happened
            Assert.AreEqual(0, sharedData.PendingUpdates());

            //Check update was fully accepted by WMI
            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
        }

        [DataTestMethod]
        [TestCategory("MachineTunnel")]
        [TestCategory("TrafficFilters")]
        public void BasicMachineProfileWithTrafficFiltersPortNoAppId()
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(ProfileType.Machine, false);
            profile.LoadMachineProfile(profileName,
                    TunnelType.SplitTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    routeList: new Dictionary<string, string>
                        {
                            { "10.0.0.0/8", "Server Network" }
                        },
                    trafficFilters: new List<TrafficFilter>() { new TrafficFilter("TF2") {
                                                                    Protocol = Protocol.TCP
                                                                }
                    }
                );
            profile.Generate();
            TestContext.WriteLine(profile.GetValidationFailures());
            TestContext.WriteLine(profile.GetValidationWarnings());
            Assert.IsFalse(profile.ValidateFailed());
            Assert.IsFalse(profile.ValidateWarnings());

            sharedData.AddProfileUpdate(profile.GetProfileUpdate());

            sharedData.HandleProfileUpdates();
            Assert.IsTrue(HelperFunctions.CheckProfileExists(profileName));

            //Assert that all updates have happened
            Assert.AreEqual(0, sharedData.PendingUpdates());

            //Check update was fully accepted by WMI
            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
        }

        [DataTestMethod]
        [TestCategory("MachineTunnel")]
        [TestCategory("TrafficFilters")]
        public void BasicMachineProfileWithTrafficFiltersForceTunnel()
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(ProfileType.Machine, false);
            profile.LoadMachineProfile(profileName,
                    TunnelType.SplitTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    routeList: new Dictionary<string, string>
                        {
                            { "10.0.0.0/8", "Server Network" }
                        },
                    trafficFilters: new List<TrafficFilter>() { new TrafficFilter("TF2") {
                                                                    AppId = "C:\\Test\\Test.exe",
                                                                    Protocol = Protocol.TCP,
                                                                    RoutingPolicyType = TunnelType.ForceTunnel
                                                                }
                    }
                );
            profile.Generate();
            TestContext.WriteLine(profile.GetValidationFailures());
            TestContext.WriteLine(profile.GetValidationWarnings());
            Assert.IsFalse(profile.ValidateFailed());
            Assert.IsFalse(profile.ValidateWarnings());

            sharedData.AddProfileUpdate(profile.GetProfileUpdate());

            sharedData.HandleProfileUpdates();
            Assert.IsTrue(HelperFunctions.CheckProfileExists(profileName));

            //Assert that all updates have happened
            Assert.AreEqual(0, sharedData.PendingUpdates());

            //Check update was fully accepted by WMI
            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
        }

        [DataTestMethod]
        [TestCategory("MachineTunnel")]
        [TestCategory("TrafficFilters")]
        public void BasicMachineProfileWithTrafficFiltersInboundRule()
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(ProfileType.Machine, false);
            profile.LoadMachineProfile(profileName,
                    TunnelType.SplitTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    routeList: new Dictionary<string, string>
                        {
                            { "10.0.0.0/8", "Server Network" }
                        },
                    trafficFilters: new List<TrafficFilter>() { new TrafficFilter("TF2") {
                                                                    AppId = "C:\\Test\\Test.exe",
                                                                    Protocol = Protocol.TCP,
                                                                    Direction = ProtocolDirection.Inbound
                                                                }
                    }
                );
            profile.Generate();
            TestContext.WriteLine(profile.GetValidationFailures());
            TestContext.WriteLine(profile.GetValidationWarnings());
            Assert.IsFalse(profile.ValidateFailed());
            Assert.IsFalse(profile.ValidateWarnings());

            sharedData.AddProfileUpdate(profile.GetProfileUpdate());

            sharedData.HandleProfileUpdates();
            Assert.IsTrue(HelperFunctions.CheckProfileExists(profileName));

            //Assert that all updates have happened
            Assert.AreEqual(0, sharedData.PendingUpdates());

            //Check update was fully accepted by WMI
            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
        }

        [DataTestMethod]
        [TestCategory("MachineTunnel")]
        [TestCategory("TrafficFilters")]
        public void BasicMachineProfileWithTrafficFiltersRemoteSplit()
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(ProfileType.Machine, false);
            profile.LoadMachineProfile(profileName,
                    TunnelType.SplitTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    routeList: new Dictionary<string, string>
                        {
                            { "10.0.0.0/8", "Server Network" }
                        },
                    trafficFilters: new List<TrafficFilter>() { new TrafficFilter("TF2") {
                                                                    AppId = "C:\\Test\\Test.exe",
                                                                    Protocol = Protocol.TCP,
                                                                    RemoteAddresses = "192.168.0.0/16",
                                                                    RemotePorts = "80, 443",
                                                                    Direction = ProtocolDirection.Outbound,
                                                                    RoutingPolicyType = TunnelType.SplitTunnel
                                                                }
                    }
                );
            profile.Generate();
            TestContext.WriteLine(profile.GetValidationFailures());
            TestContext.WriteLine(profile.GetValidationWarnings());
            Assert.IsFalse(profile.ValidateFailed());
            Assert.IsFalse(profile.ValidateWarnings());

            sharedData.AddProfileUpdate(profile.GetProfileUpdate());

            sharedData.HandleProfileUpdates();
            Assert.IsTrue(HelperFunctions.CheckProfileExists(profileName));

            //Assert that all updates have happened
            Assert.AreEqual(0, sharedData.PendingUpdates());

            //Check update was fully accepted by WMI
            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
        }

        [DataTestMethod]
        [TestCategory("MachineTunnel")]
        [TestCategory("TrafficFilters")]


        public void BasicMachineProfileWithTrafficFiltersRemotePortSplit()
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(ProfileType.Machine, false);
            profile.LoadMachineProfile(profileName,
                    TunnelType.SplitTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    routeList: new Dictionary<string, string>
                        {
                            { "10.0.0.0/8", "Server Network" }
                        },
                    trafficFilters: new List<TrafficFilter>() { new TrafficFilter("TF2") {
                                                                    AppId = "C:\\Test\\Test.exe",
                                                                    Protocol = Protocol.TCP,
                                                                    RemotePorts = "80, 443",
                                                                    Direction = ProtocolDirection.Outbound,
                                                                    RoutingPolicyType = TunnelType.SplitTunnel
                                                                }
                    }
                );
            profile.Generate();
            TestContext.WriteLine(profile.GetValidationFailures());
            TestContext.WriteLine(profile.GetValidationWarnings());
            Assert.IsFalse(profile.ValidateFailed());
            Assert.IsFalse(profile.ValidateWarnings());

            sharedData.AddProfileUpdate(profile.GetProfileUpdate());

            sharedData.HandleProfileUpdates();
            Assert.IsTrue(HelperFunctions.CheckProfileExists(profileName));

            //Assert that all updates have happened
            Assert.AreEqual(0, sharedData.PendingUpdates());

            //Check update was fully accepted by WMI
            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
        }

        [DataTestMethod]
        [TestCategory("MachineTunnel")]
        [TestCategory("TrafficFilters")]


        public void BasicMachineProfileWithTrafficFiltersRemoteAddress()
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(ProfileType.Machine, false);
            profile.LoadMachineProfile(profileName,
                    TunnelType.SplitTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    routeList: new Dictionary<string, string>
                        {
                            { "10.0.0.0/8", "Server Network" }
                        },
                    trafficFilters: new List<TrafficFilter>() { new TrafficFilter("TF2") {
                                                                    AppId = "C:\\Test\\Test.exe",
                                                                    Protocol = Protocol.TCP,
                                                                    RemoteAddresses = "10.0.0.1,192.168.0.0/16",
                                                                    Direction = ProtocolDirection.Outbound,
                                                                    RoutingPolicyType = TunnelType.SplitTunnel
                                                                }
                    }
                );
            profile.Generate();
            TestContext.WriteLine(profile.GetValidationFailures());
            TestContext.WriteLine(profile.GetValidationWarnings());
            Assert.IsFalse(profile.ValidateFailed());
            Assert.IsFalse(profile.ValidateWarnings());

            sharedData.AddProfileUpdate(profile.GetProfileUpdate());

            sharedData.HandleProfileUpdates();
            Assert.IsTrue(HelperFunctions.CheckProfileExists(profileName));

            //Assert that all updates have happened
            Assert.AreEqual(0, sharedData.PendingUpdates());

            //Check update was fully accepted by WMI
            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
        }

        [DataTestMethod]
        [TestCategory("MachineTunnel")]
        [TestCategory("TrafficFilters")]


        public void BasicMachineProfileWithTrafficFiltersLocalAddress()
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(ProfileType.Machine, false);
            profile.LoadMachineProfile(profileName,
                    TunnelType.SplitTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    routeList: new Dictionary<string, string>
                        {
                            { "10.0.0.0/8", "Server Network" }
                        },
                    trafficFilters: new List<TrafficFilter>() { new TrafficFilter("TF2") {
                                                                    AppId = "C:\\Test\\Test.exe",
                                                                    Protocol = Protocol.TCP,
                                                                    LocalAddresses = "10.0.0.1, 192.168.0.0/16",
                                                                    Direction = ProtocolDirection.Outbound,
                                                                    RoutingPolicyType = TunnelType.SplitTunnel
                                                                }
                    }
                );
            profile.Generate();
            TestContext.WriteLine(profile.GetValidationFailures());
            TestContext.WriteLine(profile.GetValidationWarnings());
            Assert.IsFalse(profile.ValidateFailed());
            Assert.IsFalse(profile.ValidateWarnings());

            sharedData.AddProfileUpdate(profile.GetProfileUpdate());

            sharedData.HandleProfileUpdates();
            Assert.IsTrue(HelperFunctions.CheckProfileExists(profileName));

            //Assert that all updates have happened
            Assert.AreEqual(0, sharedData.PendingUpdates());

            //Check update was fully accepted by WMI
            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
        }

        [DataTestMethod]
        [TestCategory("MachineTunnel")]
        [TestCategory("TrafficFilters")]


        public void BasicMachineProfileWithTrafficFiltersAllOptionsSplit()
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(ProfileType.Machine, false);
            profile.LoadMachineProfile(profileName,
                    TunnelType.SplitTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    routeList: new Dictionary<string, string>
                        {
                            { "10.0.0.0/8", "Server Network" }
                        },
                    trafficFilters: new List<TrafficFilter>() { new TrafficFilter("TF2") {
                                                                    AppId = "C:\\Test\\Test.exe",
                                                                    Protocol = Protocol.TCP,
                                                                    RemoteAddresses = "192.168.0.0/16",
                                                                    LocalPorts = "1000-2000",
                                                                    RemotePorts = "80, 443",
                                                                    Direction = ProtocolDirection.Outbound,
                                                                    RoutingPolicyType = TunnelType.SplitTunnel
                                                                }
                    }
                );
            profile.Generate();
            TestContext.WriteLine(profile.GetValidationFailures());
            TestContext.WriteLine(profile.GetValidationWarnings());
            Assert.IsFalse(profile.ValidateFailed());
            Assert.IsFalse(profile.ValidateWarnings());

            sharedData.AddProfileUpdate(profile.GetProfileUpdate());

            sharedData.HandleProfileUpdates();
            Assert.IsTrue(HelperFunctions.CheckProfileExists(profileName));

            //Assert that all updates have happened
            Assert.AreEqual(0, sharedData.PendingUpdates());

            //Check update was fully accepted by WMI
            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
        }

        [DataTestMethod]
        [TestCategory("MachineTunnel")]
        [TestCategory("TrafficFilters")]


        public void BasicMachineProfileWithTrafficFiltersAllOptionsSplitMixedPorts()
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(ProfileType.Machine, false);
            profile.LoadMachineProfile(profileName,
                    TunnelType.SplitTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    routeList: new Dictionary<string, string>
                        {
                            { "10.0.0.0/8", "Server Network" }
                        },
                    trafficFilters: new List<TrafficFilter>() { new TrafficFilter("TF2") {
                                                                    AppId = "C:\\Test\\Test.exe",
                                                                    Protocol = Protocol.TCP,
                                                                    RemoteAddresses = "192.168.0.0/16",
                                                                    LocalPorts = "1234,1000-2000,443",
                                                                    RemotePorts = "80, 500-1000, 443",
                                                                    Direction = ProtocolDirection.Outbound,
                                                                    RoutingPolicyType = TunnelType.SplitTunnel
                                                                }
                    }
                );
            profile.Generate();
            TestContext.WriteLine(profile.GetValidationFailures());
            TestContext.WriteLine(profile.GetValidationWarnings());
            Assert.IsFalse(profile.ValidateFailed());
            Assert.IsFalse(profile.ValidateWarnings());

            sharedData.AddProfileUpdate(profile.GetProfileUpdate());

            sharedData.HandleProfileUpdates();
            Assert.IsTrue(HelperFunctions.CheckProfileExists(profileName));

            //Assert that all updates have happened
            Assert.AreEqual(0, sharedData.PendingUpdates());

            //Check update was fully accepted by WMI
            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
        }

        [DataTestMethod]
        [TestCategory("MachineTunnel")]
        [TestCategory("TrafficFilters")]


        public void BasicMachineProfileWithTrafficFiltersLocalAndRemoteAddresses()
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(ProfileType.Machine, false);
            profile.LoadMachineProfile(profileName,
                    TunnelType.SplitTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    routeList: new Dictionary<string, string>
                        {
                            { "10.0.0.0/8", "Server Network" }
                        },
                    trafficFilters: new List<TrafficFilter>() { new TrafficFilter("TF2") {
                                                                    AppId = "C:\\Test\\Test.exe",
                                                                    Protocol = Protocol.TCP,
                                                                    RemoteAddresses = "192.168.0.0/16",
                                                                    LocalAddresses = "10.0.0.0/8"
                                                                }
                    }
                );
            profile.Generate();
            TestContext.WriteLine(profile.GetValidationFailures());
            TestContext.WriteLine(profile.GetValidationWarnings());
            Assert.IsFalse(profile.ValidateFailed());
            Assert.IsTrue(profile.ValidateWarnings()); //Expect Warning about local and remote addresses

            sharedData.AddProfileUpdate(profile.GetProfileUpdate());

            sharedData.HandleProfileUpdates();
            Assert.IsTrue(HelperFunctions.CheckProfileExists(profileName));

            //Assert that all updates have happened
            Assert.AreEqual(0, sharedData.PendingUpdates());

            //Check update was fully accepted by WMI
            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
        }

        [DataTestMethod]
        [TestCategory("MachineTunnel")]
        [TestCategory("TrafficFilters")]


        public void BasicMachineProfileWithTrafficFiltersAllOptionsForce()
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(ProfileType.Machine, false);
            profile.LoadMachineProfile(profileName,
                    TunnelType.SplitTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    routeList: new Dictionary<string, string>
                        {
                            { "10.0.0.0/8", "Server Network" }
                        },
                    trafficFilters: new List<TrafficFilter>() { new TrafficFilter("TF2") {
                                                                    AppId = "C:\\Test\\Test.exe",
                                                                    Protocol = Protocol.TCP,
                                                                    RemoteAddresses = "192.168.0.0/16",
                                                                    LocalPorts = "1000-2000",
                                                                    RemotePorts = "80, 443",
                                                                    Direction = ProtocolDirection.Outbound,
                                                                    RoutingPolicyType = TunnelType.ForceTunnel
                                                                }
                    }
                );
            profile.Generate();
            TestContext.WriteLine(profile.GetValidationFailures());
            TestContext.WriteLine(profile.GetValidationWarnings());
            Assert.IsFalse(profile.ValidateFailed());
            Assert.IsFalse(profile.ValidateWarnings());

            sharedData.AddProfileUpdate(profile.GetProfileUpdate());

            sharedData.HandleProfileUpdates();
            Assert.IsTrue(HelperFunctions.CheckProfileExists(profileName));

            //Assert that all updates have happened
            Assert.AreEqual(0, sharedData.PendingUpdates());

            //Check update was fully accepted by WMI
            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
        }

        [DataTestMethod]
        [TestCategory("MachineTunnel")]
        [TestCategory("TrafficFilters")]


        public void BasicMachineProfileWithTrafficFiltersDefault()
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(ProfileType.Machine, false);
            profile.LoadMachineProfile(profileName,
                    TunnelType.SplitTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    routeList: new Dictionary<string, string>
                        {
                            { "10.0.0.0/8", "Server Network" }
                        },
                    trafficFilters: new List<TrafficFilter>() { new TrafficFilter("TF2") {
                                                                }
                    }
                );
            profile.Generate();
            TestContext.WriteLine(profile.GetValidationFailures());
            TestContext.WriteLine(profile.GetValidationWarnings());
            Assert.IsFalse(profile.ValidateFailed());
            Assert.IsTrue(profile.ValidateWarnings()); //Should throw bad filter warning

            sharedData.AddProfileUpdate(profile.GetProfileUpdate());

            sharedData.HandleProfileUpdates();
            Assert.IsTrue(HelperFunctions.CheckProfileExists(profileName));

            //Assert that all updates have happened
            Assert.AreEqual(0, sharedData.PendingUpdates());

            //Check update was fully accepted by WMI
            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
        }

        [TestMethod]
        [TestCategory("MachineTunnel")]
        public void BasicMachineProfileWithManualProxy()
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(ProfileType.Machine, false);
            profile.LoadMachineProfile(profileName,
                    TunnelType.SplitTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    useProxy: true,
                    proxyType: ProxyType.Manual,
                    proxyValue: "http://proxy.test.local:8080",
                    routeList: new Dictionary<string, string>
                        {
                            { "10.0.0.1", "DC1" }
                        }
                );
            profile.Generate();
            TestContext.WriteLine(profile.GetValidationFailures());
            TestContext.WriteLine(profile.GetValidationWarnings());
            Assert.IsFalse(profile.ValidateFailed());
            Assert.IsFalse(profile.ValidateWarnings());

            sharedData.AddProfileUpdate(profile.GetProfileUpdate());

            sharedData.HandleProfileUpdates();
            Assert.IsTrue(HelperFunctions.CheckProfileExists(profileName));

            //Assert that all updates have happened
            Assert.AreEqual(0, sharedData.PendingUpdates());

            //Check update was fully accepted by WMI
            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
        }

        [DataTestMethod]
        [TestCategory("MachineTunnel")]
        [DataRow("http://proxy.test.local:8080")]
        [DataRow("https://proxy.test.local")]
        [DataRow("proxy.test.local:8080")]
        [DataRow("proxy.test.local")]
        [DataRow("http://proxy.test.local")]
        public void BasicMachineProfileWithManualProxyAndExclusions(string proxyAddress)
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(ProfileType.Machine, false);
            profile.LoadMachineProfile(profileName,
                    TunnelType.SplitTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    useProxy: true,
                    proxyType: ProxyType.Manual,
                    proxyValue: proxyAddress,
                    proxyBypassForLocal: true,
                    proxyExcludeList: new List<string>() { "*.test.local", "www.test.com" },
                    routeList: new Dictionary<string, string>
                        {
                            { "10.0.0.1", "DC1" }
                        }
                );
            profile.Generate();
            TestContext.WriteLine(profile.GetValidationFailures());
            TestContext.WriteLine(profile.GetValidationWarnings());
            Assert.IsFalse(profile.ValidateFailed());
            Assert.IsFalse(profile.ValidateWarnings());

            sharedData.AddProfileUpdate(profile.GetProfileUpdate());

            sharedData.HandleProfileUpdates();
            Assert.IsTrue(HelperFunctions.CheckProfileExists(profileName));

            //Assert that all updates have happened
            Assert.AreEqual(0, sharedData.PendingUpdates());

            //Check update was fully accepted by WMI
            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
        }

        [TestMethod]
        [TestCategory("MachineTunnel")]
        public void BasicMachineProfileWithPACProxy()
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(ProfileType.Machine, false);
            profile.LoadMachineProfile(profileName,
                    TunnelType.SplitTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    useProxy: true,
                    proxyType: ProxyType.PAC,
                    proxyValue: "http://proxy.test.local/proxyfile.pac",
                    routeList: new Dictionary<string, string>
                        {
                            { "10.0.0.1", "DC1" }
                        }
                );
            profile.Generate();
            TestContext.WriteLine(profile.GetValidationFailures());
            TestContext.WriteLine(profile.GetValidationWarnings());
            Assert.IsFalse(profile.ValidateFailed());
            Assert.IsFalse(profile.ValidateWarnings());

            sharedData.AddProfileUpdate(profile.GetProfileUpdate());

            sharedData.HandleProfileUpdates();
            Assert.IsTrue(HelperFunctions.CheckProfileExists(profileName));

            //Assert that all updates have happened
            Assert.AreEqual(0, sharedData.PendingUpdates());

            //Check update was fully accepted by WMI
            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
        }

        [TestMethod]
        [TestCategory("MachineTunnel")]
        public void BasicMachineProfile()
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(ProfileType.Machine, false);
            profile.LoadMachineProfile(profileName,
                    TunnelType.SplitTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    routeList: new Dictionary<string, string>
                        {
                            { "10.0.0.1", "DC1" }
                        }
                );
            profile.Generate();
            TestContext.WriteLine(profile.GetValidationFailures());
            TestContext.WriteLine(profile.GetValidationWarnings());
            Assert.IsFalse(profile.ValidateFailed());
            Assert.IsFalse(profile.ValidateWarnings());

            sharedData.AddProfileUpdate(profile.GetProfileUpdate());

            sharedData.HandleProfileUpdates();
            Assert.IsTrue(HelperFunctions.CheckProfileExists(profileName));

            //Assert that all updates have happened
            Assert.AreEqual(0, sharedData.PendingUpdates());

            //Check update was fully accepted by WMI
            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
        }

        [TestMethod]
        [TestCategory("MachineTunnel")]
        public void BasicMachineProfileRename()
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(ProfileType.Machine, false);
            profile.LoadMachineProfile(profileName,
                    TunnelType.SplitTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    routeList: new Dictionary<string, string>
                        {
                            { "10.0.0.1", "DC1" }
                        }
                );
            profile.Generate();
            TestContext.WriteLine(profile.GetValidationFailures());
            TestContext.WriteLine(profile.GetValidationWarnings());
            Assert.IsFalse(profile.ValidateFailed());
            Assert.IsFalse(profile.ValidateWarnings());

            sharedData.AddProfileUpdate(profile.GetProfileUpdate());

            sharedData.HandleProfileUpdates();
            Assert.IsTrue(HelperFunctions.CheckProfileExists(profileName));

            //Assert that all updates have happened
            Assert.AreEqual(0, sharedData.PendingUpdates());

            //Check update was fully accepted by WMI
            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);

            //Rename Profile and reinstall
            profileName = TestContext.TestName + "-Updated";

            profile.LoadMachineProfile(profileName,
                    TunnelType.SplitTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    routeList: new Dictionary<string, string>
                        {
                            { "10.0.0.1", "DC1" }
                        }
                );
            profile.Generate();
            TestContext.WriteLine(profile.GetValidationFailures());
            TestContext.WriteLine(profile.GetValidationWarnings());
            Assert.IsFalse(profile.ValidateFailed());
            Assert.IsFalse(profile.ValidateWarnings());

            sharedData.AddProfileUpdate(profile.GetProfileUpdate());

            sharedData.HandleProfileUpdates();
            Assert.IsTrue(HelperFunctions.CheckProfileExists(profileName));

            //Assert that all updates have happened
            Assert.AreEqual(0, sharedData.PendingUpdates());

            //Check update was fully accepted by WMI
            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
        }

        [DataTestMethod]
        [TestCategory("MachineTunnel")]
        [DataRow((uint)576)]
        [DataRow((uint)1000)]
        [DataRow((uint)1200)]
        [DataRow((uint)1300)]
        [DataRow((uint)1400)]
        public void BasicMachineProfileWithMTU(uint MTUValue)
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(ProfileType.Machine, false);
            profile.LoadMachineProfile(profileName,
                    TunnelType.SplitTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    routeList: new Dictionary<string, string>
                        {
                            { "10.0.0.1", "DC1" }
                        },
                    mTU: MTUValue
                );
            profile.Generate();
            TestContext.WriteLine(profile.GetValidationFailures());
            TestContext.WriteLine(profile.GetValidationWarnings());
            Assert.IsFalse(profile.ValidateFailed());
            Assert.IsFalse(profile.ValidateWarnings());

            sharedData.AddProfileUpdate(profile.GetProfileUpdate());

            sharedData.HandleProfileUpdates();
            Assert.IsTrue(HelperFunctions.CheckProfileExists(profileName));

            //Assert that all updates have happened
            Assert.AreEqual(0, sharedData.PendingUpdates());

            //Check update was fully accepted by WMI
            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
        }

        [DataTestMethod]
        [TestCategory("MachineTunnel")]
        [DataRow((uint)0)]
        [DataRow((uint)1)]
        [DataRow((uint)400)]
        [DataRow((uint)1500)]
        public void BasicMachineProfileWithMTUWarning(uint MTUValue)
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(ProfileType.Machine, false);
            profile.LoadMachineProfile(profileName,
                    TunnelType.SplitTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    routeList: new Dictionary<string, string>
                        {
                            { "10.0.0.1", "DC1" }
                        },
                    mTU: MTUValue
                );
            profile.Generate();
            TestContext.WriteLine(profile.GetValidationFailures());
            TestContext.WriteLine(profile.GetValidationWarnings());
            Assert.IsFalse(profile.ValidateFailed());
            Assert.IsTrue(profile.ValidateWarnings()); //The Profile should include a warning about the MTU being wrong

            sharedData.AddProfileUpdate(profile.GetProfileUpdate());

            sharedData.HandleProfileUpdates();
            Assert.IsTrue(HelperFunctions.CheckProfileExists(profileName));

            //Assert that all updates have happened
            Assert.AreEqual(0, sharedData.PendingUpdates());

            //Check update was fully accepted by WMI
            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
        }

        [TestMethod]
        [TestCategory("MachineTunnel")]
        public void BasicMachineProfileWithCertificateEKUFilter()
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(ProfileType.Machine, false);
            profile.LoadMachineProfile(profileName,
                    TunnelType.SplitTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    routeList: new Dictionary<string, string>
                        {
                            { "10.0.0.1", "DC1" }
                        },
                    machineEKU: new List<string> { "1.3.6.1.4.1.57200.1.100.2", "1.3.6.1.4.1.100.1.100.2" }
                );
            profile.Generate();
            TestContext.WriteLine(profile.GetValidationFailures());
            TestContext.WriteLine(profile.GetValidationWarnings());
            Assert.IsFalse(profile.ValidateFailed());
            Assert.IsFalse(profile.ValidateWarnings());

            sharedData.AddProfileUpdate(profile.GetProfileUpdate());

            sharedData.HandleProfileUpdates();
            Assert.IsTrue(HelperFunctions.CheckProfileExists(profileName));

            //Assert that all updates have happened
            Assert.AreEqual(0, sharedData.PendingUpdates());

            //Check update was fully accepted by WMI
            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
        }

        [TestMethod]
        [TestCategory("MachineTunnel")]
        public void BasicMachineProfileRegisterDNS()
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(ProfileType.Machine, false);
            profile.LoadMachineProfile(profileName,
                    TunnelType.SplitTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    routeList: new Dictionary<string, string>
                        {
                            { "10.0.0.1", "DC1" }
                        },
                    registerDNS: true,
                    dnsAlreadyRegistered: false
                );
            profile.Generate();
            TestContext.WriteLine(profile.GetValidationFailures());
            TestContext.WriteLine(profile.GetValidationWarnings());
            Assert.IsFalse(profile.ValidateFailed());
            Assert.IsFalse(profile.ValidateWarnings());

            sharedData.AddProfileUpdate(profile.GetProfileUpdate());

            sharedData.HandleProfileUpdates();
            Assert.IsTrue(HelperFunctions.CheckProfileExists(profileName));

            //Assert that all updates have happened
            Assert.AreEqual(0, sharedData.PendingUpdates());

            //Check update was fully accepted by WMI
            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
        }

        [TestMethod]
        [TestCategory("MachineTunnel")]
        public void BasicMachineProfileRegisterDNSWithUserTunnelAlsoRegistered()
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(ProfileType.Machine, false);
            profile.LoadMachineProfile(profileName,
                    TunnelType.SplitTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    routeList: new Dictionary<string, string>
                        {
                            { "10.0.0.1", "DC1" }
                        },
                    registerDNS: true,
                    dnsAlreadyRegistered: true
                );
            profile.Generate();
            TestContext.WriteLine(profile.GetValidationFailures());
            TestContext.WriteLine(profile.GetValidationWarnings());
            Assert.IsFalse(profile.ValidateFailed());
            Assert.IsFalse(profile.ValidateWarnings()); //Machine Tunnel Takes priority so no warning should be given

            sharedData.AddProfileUpdate(profile.GetProfileUpdate());

            sharedData.HandleProfileUpdates();
            Assert.IsTrue(HelperFunctions.CheckProfileExists(profileName));

            //Assert that all updates have happened
            Assert.AreEqual(0, sharedData.PendingUpdates());

            //Check update was fully accepted by WMI
            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
        }

        [TestMethod]
        [TestCategory("MachineTunnel")]
        public void BasicMachineWithRoutesProfile()
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(ProfileType.Machine, false);
            profile.LoadMachineProfile(profileName,
                    TunnelType.SplitTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    routeList: new Dictionary<string, string>()
                    {
                        { "192.168.200.1", "" },
                        { "10.0.0.0/8", "Internal" },
                        { "20.56.241.0/24", "External Route" },
                    }
                );
            profile.Generate();
            TestContext.WriteLine(profile.GetValidationFailures());
            TestContext.WriteLine(profile.GetValidationWarnings());
            Assert.IsFalse(profile.ValidateFailed());
            Assert.IsFalse(profile.ValidateWarnings());

            sharedData.AddProfileUpdate(profile.GetProfileUpdate());

            sharedData.HandleProfileUpdates();
            Assert.IsTrue(HelperFunctions.CheckProfileExists(profileName));

            //Assert that all updates have happened
            Assert.AreEqual(0, sharedData.PendingUpdates());

            //Check update was fully accepted by WMI
            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
        }

        [DataTestMethod]
        [TestCategory("MachineTunnel")]
        [DataRow((uint)0)]
        [DataRow((uint)1)]
        [DataRow((uint)10)]
        [DataRow((uint)100)]
        [DataRow((uint)1000)]
        [DataRow((uint)9999)]
        [DataRow((uint)10000)]
        [DataRow((uint)2147483647)]
        public void BasicMachineWithRoutesProfileAndRouteMetric(uint metric)
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(ProfileType.Machine, false);
            profile.LoadMachineProfile(profileName,
                    TunnelType.SplitTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    routeList: new Dictionary<string, string>()
                    {
                        { "192.168.200.1", "" },
                        { "10.0.0.0/8", "Internal" },
                        { "20.56.241.0/24", "External Route" },
                    },
                    routeMetric: metric
                );
            profile.Generate();
            TestContext.WriteLine(profile.GetValidationFailures());
            TestContext.WriteLine(profile.GetValidationWarnings());
            Assert.IsFalse(profile.ValidateFailed());
            Assert.IsFalse(profile.ValidateWarnings());

            sharedData.AddProfileUpdate(profile.GetProfileUpdate());

            sharedData.HandleProfileUpdates();
            Assert.IsTrue(HelperFunctions.CheckProfileExists(profileName));

            //Assert that all updates have happened
            Assert.AreEqual(0, sharedData.PendingUpdates());

            //Check update was fully accepted by WMI
            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
        }

        [TestMethod]
        [TestCategory("MachineTunnel")]
        public void BasicMachineWithDomainNameInfoProfile()
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(ProfileType.Machine, false);
            profile.LoadMachineProfile(profileName,
                    TunnelType.SplitTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    domainInformationList: new Dictionary<string, string>
                        {
                            { ".", "192.168.0.1, 192.168.0.2" },
                            { ".example.com", "192.168.0.1,192.168.0.2" },
                            { "www.example.com", "" }
                        },
                    routeList: new Dictionary<string, string>
                        {
                            { "10.0.0.1", "DC1" }
                        }
                );
            profile.Generate();
            TestContext.WriteLine(profile.GetValidationFailures());
            TestContext.WriteLine(profile.GetValidationWarnings());
            Assert.IsFalse(profile.ValidateFailed());
            Assert.IsTrue(profile.ValidateWarnings()); //Expecting Trusted Network Autogenerated Warning only
            Assert.AreEqual(profile.GetValidationWarnings().Trim().Split(Environment.NewLine.ToCharArray()).Count(), 1); //Check that there was only the 1 validation warning

            sharedData.AddProfileUpdate(profile.GetProfileUpdate());

            sharedData.HandleProfileUpdates();
            Assert.IsTrue(HelperFunctions.CheckProfileExists(profileName));

            //Assert that all updates have happened
            Assert.AreEqual(0, sharedData.PendingUpdates());

            //Check update was fully accepted by WMI
            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
        }

        [TestMethod]
        [TestCategory("MachineTunnel")]
        public void BasicMachineWithDomainNameInfoAndTrustedNetworkProfile()
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(ProfileType.Machine, false);
            profile.LoadMachineProfile(profileName,
                    TunnelType.SplitTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    domainInformationList: new Dictionary<string, string>
                        {
                            { ".", "192.168.0.1, 192.168.0.2" },
                            { ".example.com", "192.168.0.1,192.168.0.2" },
                            { "www.example.com", "" }
                        },
                    routeList: new Dictionary<string, string>
                        {
                            { "10.0.0.1", "DC1" }
                        },
                    trustedNetworkList: new List<string>() { "test.com", "example.net" }
                );
            profile.Generate();
            TestContext.WriteLine(profile.GetValidationFailures());
            TestContext.WriteLine(profile.GetValidationWarnings());
            Assert.IsFalse(profile.ValidateFailed());
            Assert.IsFalse(profile.ValidateWarnings()); //Expecting Trusted Network Autogenerated Warning only

            sharedData.AddProfileUpdate(profile.GetProfileUpdate());

            sharedData.HandleProfileUpdates();
            Assert.IsTrue(HelperFunctions.CheckProfileExists(profileName));

            //Assert that all updates have happened
            Assert.AreEqual(0, sharedData.PendingUpdates());

            //Check update was fully accepted by WMI
            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
        }

        [TestMethod]
        [TestCategory("MachineTunnel")]
        public void BasicMachineWithTrustedNetworkProfile()
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(ProfileType.Machine, false);
            profile.LoadMachineProfile(profileName,
                    TunnelType.SplitTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    trustedNetworkList: new List<string>() { "example.com" },
                    routeList: new Dictionary<string, string>
                        {
                            { "10.0.0.1", "DC1" }
                        }
                );
            profile.Generate();
            TestContext.WriteLine(profile.GetValidationFailures());
            TestContext.WriteLine(profile.GetValidationWarnings());
            Assert.IsFalse(profile.ValidateFailed());
            Assert.IsFalse(profile.ValidateWarnings()); //Expecting Trusted Network Autogenerated Warning only

            sharedData.AddProfileUpdate(profile.GetProfileUpdate());

            sharedData.HandleProfileUpdates();
            Assert.IsTrue(HelperFunctions.CheckProfileExists(profileName));

            //Assert that all updates have happened
            Assert.AreEqual(0, sharedData.PendingUpdates());

            //Check update was fully accepted by WMI
            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
        }

        [DataTestMethod]
        [TestCategory("MachineTunnel")]
        [TestCategory("OverrideProfile")]
        public void OverrideMachineProfile()
        {
            string profileName = TestContext.TestName;
            string overrideProfile = "<VPNProfile> <AlwaysOn>true</AlwaysOn> <DeviceTunnel>true</DeviceTunnel> <RegisterDNS>false</RegisterDNS> <NativeProfile> <Servers>aovpndpcunittest.systemcenter.ninja</Servers>  <RoutingPolicyType>SplitTunnel</RoutingPolicyType> <NativeProtocolType>IKEv2</NativeProtocolType>  <DisableClassBasedDefaultRoute>true</DisableClassBasedDefaultRoute>  <Authentication>   <MachineMethod>Certificate</MachineMethod>  </Authentication> </NativeProfile> <!--DC1--> <Route>  <Address>10.0.0.1</Address>  <PrefixSize>32</PrefixSize> </Route></VPNProfile>";

            VPNProfileCreator profile = new VPNProfileCreator(ProfileType.Machine, false);
            profile.LoadMachineProfile(profileName,
                    TunnelType.SplitTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    overrideProfile
                );
            profile.Generate();

            VPNProfileCreator originalProfile = new VPNProfileCreator(ProfileType.Machine, false);
            originalProfile.LoadMachineProfile(profileName,
                    TunnelType.SplitTunnel,
                    HelperFunctions.DefaultConnectionURL
                );
            originalProfile.Generate();

            TestContext.WriteLine(profile.GetValidationFailures());
            TestContext.WriteLine(profile.GetValidationWarnings());
            Assert.IsFalse(profile.ValidateFailed());
            Assert.IsFalse(profile.ValidateWarnings());

            sharedData.AddProfileUpdate(profile.GetProfileUpdate());

            sharedData.HandleProfileUpdates();
            Assert.IsTrue(HelperFunctions.CheckProfileExists(profileName));

            //Assert that all updates have happened
            Assert.AreEqual(0, sharedData.PendingUpdates());

            //Check update was fully accepted by WMI
            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
            //Check that the override actually happened
            Assert.IsFalse(VPNProfile.CompareToInstalledProfile(profileName, originalProfile.GetProfile(), TestContext.CancellationTokenSource.Token));
        }

        [DataTestMethod]
        [TestCategory("MachineTunnel")]
        [TestCategory("OverrideProfile")]
        public void OverrideMachineProfileErrorsAreWarnings()
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(ProfileType.Machine, false);
            profile.LoadMachineProfile(profileName,
                    TunnelType.SplitTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    "<VPNProfile><DeviceTunnel>true</DeviceTunnel><AlwaysOn>true</AlwaysOn><NativeProfile><NativeProtocolType>Automatic</NativeProtocolType><Authentication><UserMethod>Mschapv2</UserMethod></Authentication></NativeProfile></VPNProfile>"
                );
            profile.Generate();
            TestContext.WriteLine(profile.GetValidationFailures());
            TestContext.WriteLine(profile.GetValidationWarnings());
            Assert.IsFalse(profile.ValidateFailed());
            Assert.IsTrue(profile.ValidateWarnings()); //Warnings are required due to broken override XML

            sharedData.AddProfileUpdate(profile.GetProfileUpdate());

            sharedData.HandleProfileUpdates();
            Assert.IsTrue(HelperFunctions.CheckProfileExists(profileName));

            //Assert that all updates have happened
            Assert.AreEqual(0, sharedData.PendingUpdates());

            //Check update was fully accepted by WMI
            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);

            HelperFunctions.ClearSpecificEventId(1160); //Failed to Update VPNStrategy
            HelperFunctions.ClearSpecificEventId(1190); // Failed to Update Machine Certificate Filter EKU
        }

        [DataTestMethod]
        [TestCategory("MachineTunnel")]
        [DataRow("")]
        [DataRow(null)]
        [DataRow("%TEMP%")]
        [DataRow("bob")]
        [DataRow("C:\\Windows\\Temp")]
        [DataRow("C:\\Windows\\Temp\\profile.xml")]
        [DataRow("C:\\Windows\\Temp\\Profile2")]
        public void BasicMachineDebugSave(string savePath)
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(ProfileType.Machine, false);
            profile.LoadMachineProfile(profileName,
                    TunnelType.SplitTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    trustedNetworkList: new List<string>() { "example.com" },
                    routeList: new Dictionary<string, string>
                        {
                            { "10.0.0.1", "DC1" }
                        }
                );
            profile.Generate();
            TestContext.WriteLine(profile.GetValidationFailures());
            TestContext.WriteLine(profile.GetValidationWarnings());
            Assert.IsFalse(profile.ValidateFailed());
            Assert.IsFalse(profile.ValidateWarnings()); //Expecting Trusted Network Autogenerated Warning only

            sharedData.AddProfileUpdate(profile.GetProfileUpdate());

            sharedData.HandleProfileUpdates();
            Assert.IsTrue(HelperFunctions.CheckProfileExists(profileName));

            //Assert that all updates have happened
            Assert.AreEqual(0, sharedData.PendingUpdates());

            //Check update was fully accepted by WMI
            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
            if (File.Exists(savePath))
            {
                File.Delete(savePath);
            }
            Assert.IsFalse(File.Exists(savePath));

            if (DeviceInfo.GetOSVersion().WMIWorking)
            {

                string WMIProfile = AccessWMI.GetProfileData(profileName, TestContext.CancellationTokenSource.Token);

                string actualSavePath = VPNProfileCreator.SaveProfile(profileName, WMIProfile, savePath);
                if (!string.IsNullOrWhiteSpace(savePath))
                {
                    Assert.IsTrue(File.Exists(actualSavePath));
                    string fileText = File.ReadAllText(actualSavePath);
                    CSPProfile fileProfile = new CSPProfile(fileText, profileName);
                    fileProfile.DeviceTunnel = true; //Bug in CSP which doesn't show device tunnel as correct all the time, WMIProfile gets round this by looking directly
                    Assert.IsTrue(VPNProfile.CompareProfiles(new CSPProfile(profile.GetProfile(), profile.GetProfileName()), fileProfile));
                    Assert.IsTrue(fileText.Contains("\n")); //Ensure pretty print is working
                    Assert.IsTrue(fileText.Contains(" ")); //Ensure pretty print is working
                }
            }
            else
            {
                string actualSavePath = ProfileMonitor.SaveWMIProfile(savePath, profileName, "TestProfile", TestContext.CancellationTokenSource.Token);
                if (!string.IsNullOrWhiteSpace(savePath))
                {
                    Assert.IsTrue(File.Exists(actualSavePath));
                    string fileText = File.ReadAllText(actualSavePath);

                    VPNProfile cspProfile = new CSPProfile(profile.GetProfile(), profileName);

                    ProfileInfo profileInfo = ManageRasphonePBK.ListProfiles(profileName, DeviceInfo.CurrentUserSID());
                    VPNProfile wmiProfile = new WMIProfile(profileInfo, TestContext.CancellationTokenSource.Token);

                    Assert.AreEqual(cspProfile.ToString(), fileText, true); //CSP Casing may not always match WMI Casing
                    Assert.AreEqual(wmiProfile.ToString(), fileText);
                }
            }
        }
    }
}
