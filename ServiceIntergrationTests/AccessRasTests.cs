using DPCLibrary.Enums;
using DPCLibrary.Utils;
using DPCService.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace ServiceIntegrationTests
{
    [TestClass]
    public class AccessRasTests
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

        [TestMethod]
        public void GetBasicVPNStrategy()
        {
            VPNProfileCreator profile = HelperFunctions.BasicUserProfile(sharedData, TestContext.TestName, TestContext.CancellationTokenSource.Token, TestContext);
            AccessRasApi.GetVPNStrategy(profile.GetProfileName());
        }

        [TestMethod]
        public void GetNoUpdateDoesNotUpdateVPNStrategy()
        {
            VPNProfileCreator profile = HelperFunctions.BasicUserProfile(sharedData, TestContext.TestName, TestContext.CancellationTokenSource.Token, TestContext);
            Assert.IsFalse(AccessRasApi.SetVPNStrategy(profile.GetProfileName(), AccessRasApi.GetVPNStrategy(profile.GetProfileName())));
        }

        [DataTestMethod]
        [DataRow(VPNStrategy.Default)]
        //[DataRow(VPNStrategy.GREOnly)] //Not Supported due to breaking the WMI interface
        [DataRow(VPNStrategy.Ikev2First)]
        //[DataRow(VPNStrategy.Ikev2Only)] //This is profile default so will fail as no update is required
        [DataRow(VPNStrategy.Ikev2Sstp)]
        [DataRow(VPNStrategy.L2tpFirst)]
        [DataRow(VPNStrategy.L2tpOnly)]
        [DataRow(VPNStrategy.L2tpSstp)]
        [DataRow(VPNStrategy.PptpFirst)]
        [DataRow(VPNStrategy.PptpOnly)]
        [DataRow(VPNStrategy.PptpSstp)]
        //[DataRow(VPNStrategy.ProtocolList)] //Not Supported due to breaking the WMI interface
        [DataRow(VPNStrategy.SstpFirst)]
        [DataRow(VPNStrategy.SstpOnly)]
        public void UpdateVPNStrategy(VPNStrategy strategy)
        {
            VPNProfileCreator profile = HelperFunctions.BasicUserProfile(sharedData, TestContext.TestName, TestContext.CancellationTokenSource.Token, TestContext);
            string profileName = profile.GetProfileName();

            Assert.AreNotEqual(AccessRasApi.GetVPNStrategy(profileName), strategy);
            Assert.IsTrue(AccessRasApi.SetVPNStrategy(profileName, strategy));
            Assert.AreEqual(AccessRasApi.GetVPNStrategy(profileName), strategy);

            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
        }

        [DataTestMethod]
        [DataRow(VPNStrategy.GREOnly)]
        [DataRow(VPNStrategy.ProtocolList)]
        public void UpdateVPNStrategyNotSupported(VPNStrategy strategy)
        {
            VPNProfileCreator profile = HelperFunctions.BasicUserProfile(sharedData, TestContext.TestName, TestContext.CancellationTokenSource.Token, TestContext);
            string profileName = profile.GetProfileName();

            Assert.AreNotEqual(AccessRasApi.GetVPNStrategy(profileName), strategy);
            Assert.ThrowsException<InvalidOperationException>(() => AccessRasApi.SetVPNStrategy(profileName, strategy));
            Assert.AreNotEqual(AccessRasApi.GetVPNStrategy(profileName), strategy);

            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
        }

        [DataTestMethod]
        [DataRow(VPNStrategy.Default)]
        //[DataRow(VPNStrategy.GREOnly)] //Not Supported due to breaking the WMI interface
        [DataRow(VPNStrategy.Ikev2First)]
        //[DataRow(VPNStrategy.Ikev2Only)] //This is profile default so will fail as no update is required
        [DataRow(VPNStrategy.Ikev2Sstp)]
        [DataRow(VPNStrategy.L2tpFirst)]
        [DataRow(VPNStrategy.L2tpOnly)]
        [DataRow(VPNStrategy.L2tpSstp)]
        [DataRow(VPNStrategy.PptpFirst)]
        [DataRow(VPNStrategy.PptpOnly)]
        [DataRow(VPNStrategy.PptpSstp)]
        //[DataRow(VPNStrategy.ProtocolList)] //Not Supported due to breaking the WMI interface
        [DataRow(VPNStrategy.SstpFirst)]
        [DataRow(VPNStrategy.SstpOnly)]
        public void UpdateVPNStrategyForceTunnel(VPNStrategy strategy)
        {
            VPNProfileCreator profile = HelperFunctions.BasicForceTunnelUserProfile(sharedData, TestContext.TestName, TestContext.CancellationTokenSource.Token);
            string profileName = profile.GetProfileName();
            Assert.AreNotEqual(AccessRasApi.GetVPNStrategy(profileName), strategy);
            Assert.IsTrue(AccessRasApi.SetVPNStrategy(profileName, strategy));
            Assert.AreEqual(AccessRasApi.GetVPNStrategy(profileName), strategy);

            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
        }

        [DataTestMethod]
        [DataRow(VPNStrategy.GREOnly)] //Not Supported due to breaking the WMI interface
        [DataRow(VPNStrategy.ProtocolList)] //Not Supported due to breaking the WMI interface
        public void UpdateVPNStrategyForceTunnelNotSupported(VPNStrategy strategy)
        {
            VPNProfileCreator profile = HelperFunctions.BasicForceTunnelUserProfile(sharedData, TestContext.TestName, TestContext.CancellationTokenSource.Token);
            string profileName = profile.GetProfileName();
            Assert.AreNotEqual(AccessRasApi.GetVPNStrategy(profileName), strategy);

            Assert.ThrowsException<InvalidOperationException>(() => AccessRasApi.SetVPNStrategy(profileName, strategy));
            Assert.AreNotEqual(AccessRasApi.GetVPNStrategy(profileName), strategy);

            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
        }

        [DataTestMethod]
        [DataRow(VPNStrategy.Default)]
        //[DataRow(VPNStrategy.GREOnly)] //Not Supported due to breaking the WMI interface
        [DataRow(VPNStrategy.Ikev2First)]
        //[DataRow(VPNStrategy.Ikev2Only)] //This is profile default so will fail as no update is required
        [DataRow(VPNStrategy.Ikev2Sstp)]
        [DataRow(VPNStrategy.L2tpFirst)]
        [DataRow(VPNStrategy.L2tpOnly)]
        [DataRow(VPNStrategy.L2tpSstp)]
        [DataRow(VPNStrategy.PptpFirst)]
        //[DataRow(VPNStrategy.PptpOnly)] //Doesn't work in Win11 with custom crypto enabled, see UpdateVPNStrategyCustomCryptographyNotValid test
        [DataRow(VPNStrategy.PptpSstp)]
        //[DataRow(VPNStrategy.ProtocolList)] //Not Supported due to breaking the WMI interface
        [DataRow(VPNStrategy.SstpFirst)]
        //[DataRow(VPNStrategy.SstpOnly)] //Doesn't work in Win11 with custom crypto enabled, see UpdateVPNStrategyCustomCryptographyNotValid test
        public void UpdateVPNStrategyCustomCryptography(VPNStrategy strategy)
        {
            string profileName = TestContext.TestName;
            ProfileType profileType = ProfileType.User;

            Assert.AreEqual(0,ManageRasphonePBK.ListProfiles(profileName).Count);

            VPNProfileCreator profile = new VPNProfileCreator(profileType, false);
            profile.LoadUserProfile(profileName,
                    TunnelType.SplitTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    new List<string>() { "47beabc922eae80e78783462a79f45c254fde68b" },
                    new List<string>() { "27ac9369faf25207bb2627cefaccbe4ef9c319b8" },
                    new List<string>() { "NPS01.Test.local" },
                    routeList: new Dictionary<string, string>
                        {
                            { "10.0.0.0/8", "Server Network" }
                        },
                    customCryptography: true,
                    authenticationTransformConstants: "SHA256128",
                    cipherTransformConstants: "AES128",
                    pfsGroup: "PFS2048",
                    dHGroup: "Group14",
                    integrityCheckMethod: "SHA256",
                    encryptionMethod: "AES128"
                );
            profile.Generate();
            Assert.IsFalse(profile.ValidateFailed());
            Assert.IsFalse(profile.ValidateWarnings());

            sharedData.AddProfileUpdate(profile.GetProfileUpdate());

            sharedData.HandleProfileUpdates();
            Assert.IsTrue(ManageRasphonePBK.ListProfiles(profileName).Count == 1);

            //Assert that all updates have happened
            Assert.AreEqual(0, sharedData.PendingUpdates());

            //Check update was fully accepted by WMI
            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
            Assert.AreNotEqual(AccessRasApi.GetVPNStrategy(profileName), strategy);
            Assert.IsTrue(AccessRasApi.SetVPNStrategy(profileName, strategy));
            Assert.AreEqual(AccessRasApi.GetVPNStrategy(profileName), strategy);
            //Check update was fully accepted by WMI
            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
        }

        [DataTestMethod]
        [DataRow(VPNStrategy.PptpOnly)]
        [DataRow(VPNStrategy.SstpOnly)]
        public void UpdateVPNStrategyCustomCryptographyNotValid(VPNStrategy strategy)
        {
            string profileName = TestContext.TestName;
            ProfileType profileType = ProfileType.User;

            Assert.AreEqual(0, ManageRasphonePBK.ListProfiles(profileName).Count);

            VPNProfileCreator profile = new VPNProfileCreator(profileType, false);
            profile.LoadUserProfile(profileName,
                    TunnelType.SplitTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    new List<string>() { "47beabc922eae80e78783462a79f45c254fde68b" },
                    new List<string>() { "27ac9369faf25207bb2627cefaccbe4ef9c319b8" },
                    new List<string>() { "NPS01.Test.local" },
                    routeList: new Dictionary<string, string>
                        {
                            { "10.0.0.0/8", "Server Network" }
                        },
                    customCryptography: true,
                    authenticationTransformConstants: "SHA256128",
                    cipherTransformConstants: "AES128",
                    pfsGroup: "PFS2048",
                    dHGroup: "Group14",
                    integrityCheckMethod: "SHA256",
                    encryptionMethod: "AES128",
                    vPNStrategy: VPNStrategy.Ikev2Only
                );
            profile.Generate();
            Assert.IsFalse(profile.ValidateFailed());
            Assert.IsFalse(profile.ValidateWarnings());

            sharedData.AddProfileUpdate(profile.GetProfileUpdate());

            sharedData.HandleProfileUpdates();
            Assert.IsTrue(ManageRasphonePBK.ListProfiles(profileName).Count == 1);

            //Assert that all updates have happened
            Assert.AreEqual(0, sharedData.PendingUpdates());

            //Check update was fully accepted by WMI
            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
            Assert.AreNotEqual(AccessRasApi.GetVPNStrategy(profileName), strategy);

            profile.LoadUserProfile(profileName,
                TunnelType.SplitTunnel,
                HelperFunctions.DefaultConnectionURL,
                new List<string>() { "47beabc922eae80e78783462a79f45c254fde68b" },
                new List<string>() { "27ac9369faf25207bb2627cefaccbe4ef9c319b8" },
                new List<string>() { "NPS01.Test.local" },
                routeList: new Dictionary<string, string>
                    {
                                    { "10.0.0.0/8", "Server Network" }
                    },
                customCryptography: true,
                authenticationTransformConstants: "SHA256128",
                cipherTransformConstants: "AES128",
                pfsGroup: "PFS2048",
                dHGroup: "Group14",
                integrityCheckMethod: "SHA256",
                encryptionMethod: "AES128",
                vPNStrategy: strategy
            );
            profile.Generate();
            Assert.IsFalse(profile.ValidateFailed());
            Assert.IsTrue(profile.ValidateWarnings()); //Should have warning disabling customcryptography

            sharedData.AddProfileUpdate(profile.GetProfileUpdate());

            sharedData.HandleProfileUpdates();
            Assert.IsTrue(ManageRasphonePBK.ListProfiles(profileName).Count == 1);

            //Assert that all updates have happened
            Assert.AreEqual(0, sharedData.PendingUpdates());

            //Check update was fully accepted by WMI
            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
            Assert.AreEqual(AccessRasApi.GetVPNStrategy(profileName), strategy);
        }

        [TestMethod]
        public void AllowIKEv2StrategyUpdate()
        {
            VPNProfileCreator profile = HelperFunctions.BasicUserProfile(sharedData, TestContext.TestName, TestContext.CancellationTokenSource.Token, TestContext);
            string profileName = profile.GetProfileName();

            //Move to SSTPOnly first as default profile is IKEv2 so it won't be changed
            Assert.AreNotEqual(AccessRasApi.GetVPNStrategy(profileName), VPNStrategy.SstpOnly);
            Assert.IsTrue(AccessRasApi.SetVPNStrategy(profileName, VPNStrategy.SstpOnly));
            Assert.AreEqual(AccessRasApi.GetVPNStrategy(profileName), VPNStrategy.SstpOnly);

            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);

            //Change Back to IKEv2Only
            Assert.AreNotEqual(AccessRasApi.GetVPNStrategy(profileName), VPNStrategy.Ikev2Only);
            Assert.IsTrue(AccessRasApi.SetVPNStrategy(profileName, VPNStrategy.Ikev2Only));
            Assert.AreEqual(AccessRasApi.GetVPNStrategy(profileName), VPNStrategy.Ikev2Only);

            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
        }

        [TestMethod]
        public void AllowForceTunnelIKEv2StrategyUpdate()
        {
            VPNProfileCreator profile = HelperFunctions.BasicForceTunnelUserProfile(sharedData, TestContext.TestName, TestContext.CancellationTokenSource.Token);
            string profileName = profile.GetProfileName();
            //Move to SSTPOnly first as default profile is IKEv2 so it won't be changed
            Assert.AreNotEqual(AccessRasApi.GetVPNStrategy(profileName), VPNStrategy.SstpOnly);
            Assert.IsTrue(AccessRasApi.SetVPNStrategy(profileName, VPNStrategy.SstpOnly));
            Assert.AreEqual(AccessRasApi.GetVPNStrategy(profileName), VPNStrategy.SstpOnly);
            //Change Back to IKEv2Only
            Assert.AreNotEqual(AccessRasApi.GetVPNStrategy(profileName), VPNStrategy.Ikev2Only);
            Assert.IsTrue(AccessRasApi.SetVPNStrategy(profileName, VPNStrategy.Ikev2Only));
            Assert.AreEqual(AccessRasApi.GetVPNStrategy(profileName), VPNStrategy.Ikev2Only);

            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
        }

        [DataTestMethod]
        //[DataRow(0)] //Default option so won't update without changing to something else first
        [DataRow((uint)1)]
        [DataRow((uint)15)]
        [DataRow((uint)100)]
        [DataRow((uint)1000)]
        [DataRow((uint)1600)]
        [DataRow((uint)9999)] //Anything higher than 9999 is rejected by Windows
        public void UpdateMetric(uint newMetric)
        {
            VPNProfileCreator profile = HelperFunctions.BasicUserProfile(sharedData, TestContext.TestName, TestContext.CancellationTokenSource.Token, TestContext);
            string profileName = profile.GetProfileName();
            Assert.AreNotEqual(AccessRasApi.GetVPNIPv4Metric(profileName), newMetric);
            Assert.IsTrue(AccessRasApi.SetVPNMetric(profileName, newMetric));
            Assert.AreEqual(AccessRasApi.GetVPNIPv4Metric(profileName), newMetric);

            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
        }

        [TestMethod]
        public void TestDefaultMetricUpdate()
        {
            VPNProfileCreator profile = HelperFunctions.BasicUserProfile(sharedData, TestContext.TestName, TestContext.CancellationTokenSource.Token, TestContext);
            string profileName = profile.GetProfileName();
            //Move to static metric so that the profile can be switched back to Default
            Assert.AreNotEqual((uint)15, AccessRasApi.GetVPNIPv4Metric(profileName));
            Assert.IsTrue(AccessRasApi.SetVPNMetric(profileName, 15));
            Assert.AreEqual((uint) 15, AccessRasApi.GetVPNIPv4Metric(profileName));

            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);

            //Change Back to Default (0)
            Assert.AreNotEqual((uint)0, AccessRasApi.GetVPNIPv4Metric(profileName));
            Assert.IsTrue(AccessRasApi.SetVPNMetric(profileName, 0));
            Assert.AreEqual((uint)0, AccessRasApi.GetVPNIPv4Metric(profileName));

            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
        }

        [TestMethod]
        public void GetBasicRasCredentials()
        {
            VPNProfileCreator profile = HelperFunctions.BasicUserProfile(sharedData, TestContext.TestName, TestContext.CancellationTokenSource.Token, TestContext);
            string profileName = profile.GetProfileName();

            Assert.IsFalse(AccessRasApi.GetDisableRasCredStatus(profileName));

            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
        }

        [TestMethod]
        public void GetNoUpdateDoesNotUpdateRasCredentials()
        {
            VPNProfileCreator profile = HelperFunctions.BasicUserProfile(sharedData, TestContext.TestName, TestContext.CancellationTokenSource.Token, TestContext);
            string profileName = profile.GetProfileName();

            Assert.IsFalse(AccessRasApi.SetDisableRasCredFlag(profileName, AccessRasApi.GetDisableRasCredStatus(profileName)));

            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
        }

        [TestMethod]
        public void UpdateRasCredentials()
        {
            VPNProfileCreator profile = HelperFunctions.BasicUserProfile(sharedData, TestContext.TestName, TestContext.CancellationTokenSource.Token, TestContext);
            string profileName = profile.GetProfileName();

            Assert.IsFalse(AccessRasApi.GetDisableRasCredStatus(profileName));
            Assert.IsTrue(AccessRasApi.SetDisableRasCredFlag(profileName, true));
            Assert.IsTrue(AccessRasApi.GetDisableRasCredStatus(profileName));

            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
        }

        [TestMethod]
        public void DisableRasCredentials()
        {
            VPNProfileCreator profile = HelperFunctions.BasicUserProfile(sharedData, TestContext.TestName, TestContext.CancellationTokenSource.Token, TestContext);
            string profileName = profile.GetProfileName();

            Assert.IsFalse(AccessRasApi.GetDisableRasCredStatus(profileName));
            Assert.IsTrue(AccessRasApi.SetDisableRasCredFlag(profileName, true));
            Assert.IsTrue(AccessRasApi.GetDisableRasCredStatus(profileName));

            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);

            Assert.IsTrue(AccessRasApi.SetDisableRasCredFlag(profileName, false));
            Assert.IsFalse(AccessRasApi.GetDisableRasCredStatus(profileName));

            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
        }

        [TestMethod]
        public void GetBasicInterfacev4Metric()
        {
            VPNProfileCreator profile = HelperFunctions.BasicUserProfile(sharedData, TestContext.TestName, TestContext.CancellationTokenSource.Token, TestContext);
            string profileName = profile.GetProfileName();

            Assert.AreEqual((uint)0,AccessRasApi.GetVPNIPv4Metric(profileName));

            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
        }

        [TestMethod]
        public void GetBasicInterfacev6Metric()
        {
            VPNProfileCreator profile = HelperFunctions.BasicUserProfile(sharedData, TestContext.TestName, TestContext.CancellationTokenSource.Token, TestContext);
            string profileName = profile.GetProfileName();

            Assert.AreEqual((uint)0, AccessRasApi.GetVPNIPv6Metric(profileName));

            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
        }

        [TestMethod]
        public void GetNoUpdateDoesntUpdateMetric()
        {
            VPNProfileCreator profile = HelperFunctions.BasicUserProfile(sharedData, TestContext.TestName, TestContext.CancellationTokenSource.Token, TestContext);
            string profileName = profile.GetProfileName();

            Assert.IsFalse(AccessRasApi.SetVPNMetric(profileName, AccessRasApi.GetVPNIPv4Metric(profileName)));

            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
        }

        public void DisableVPNMetric()
        {
            VPNProfileCreator profile = HelperFunctions.BasicUserProfile(sharedData, TestContext.TestName, TestContext.CancellationTokenSource.Token, TestContext);
            string profileName = profile.GetProfileName();

            Assert.AreNotEqual<uint>(AccessRasApi.GetVPNIPv4Metric(profileName), 100);
            Assert.IsTrue(AccessRasApi.SetVPNMetric(profileName, 100));
            Assert.AreEqual<uint>(AccessRasApi.GetVPNIPv4Metric(profileName), 100);

            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);

            Assert.IsTrue(AccessRasApi.SetVPNMetric(profileName, null));
            Assert.IsNull(AccessRasApi.GetVPNIPv4Metric(profileName));

            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
        }

        [TestMethod]
        public void GetBasicNetworkOutageTime()
        {
            VPNProfileCreator profile = HelperFunctions.BasicUserProfile(sharedData, TestContext.TestName, TestContext.CancellationTokenSource.Token, TestContext);
            string profileName = profile.GetProfileName();


            Assert.AreEqual((uint) 1800, AccessRasApi.GetNetworkOutageTime(profileName)); //Default is 30 minutes

            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
        }

        [TestMethod]
        public void GetNoUpdateDoesntUpdateNetworkOutageTime()
        {
            VPNProfileCreator profile = HelperFunctions.BasicUserProfile(sharedData, TestContext.TestName, TestContext.CancellationTokenSource.Token, TestContext);
            string profileName = profile.GetProfileName();

            Assert.IsFalse(AccessRasApi.SetNetworkOutageTime(profileName, AccessRasApi.GetNetworkOutageTime(profileName)));

            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
        }

        [DataTestMethod]
        [DataRow((uint)0)]
        [DataRow((uint)1)]
        [DataRow((uint)15)]
        [DataRow((uint)100)]
        [DataRow((uint)1000)]
        [DataRow((uint)1600)]
        [DataRow((uint)9999)]
        [DataRow((uint)10000)]
        [DataRow((uint)300)]
        [DataRow((uint)600)]
        [DataRow((uint)1200)]
        //[DataRow((uint)1800)] //Default Option so will fail update checks
        [DataRow((uint)3600)]
        [DataRow((uint)7200)]
        [DataRow((uint)14400)]
        [DataRow((uint)28800)]
        [DataRow((uint)4294967295)]
        public void UpdateNetworkOutageTime(uint updatedTime)
        {
            VPNProfileCreator profile = HelperFunctions.BasicUserProfile(sharedData, TestContext.TestName, TestContext.CancellationTokenSource.Token, TestContext);
            string profileName = profile.GetProfileName();

            Assert.AreNotEqual(updatedTime, AccessRasApi.GetNetworkOutageTime(profileName));
            Assert.IsTrue(AccessRasApi.SetNetworkOutageTime(profileName, updatedTime));
            Assert.AreEqual(updatedTime, AccessRasApi.GetNetworkOutageTime(profileName));

            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
        }

        public void DisableNetworkOutageTime()
        {
            VPNProfileCreator profile = HelperFunctions.BasicUserProfile(sharedData, TestContext.TestName, TestContext.CancellationTokenSource.Token, TestContext);
            string profileName = profile.GetProfileName();

            Assert.AreNotEqual<uint>(AccessRasApi.GetNetworkOutageTime(profileName), 300);
            Assert.IsTrue(AccessRasApi.SetNetworkOutageTime(profileName, 300));
            Assert.AreEqual<uint>(AccessRasApi.GetNetworkOutageTime(profileName), 300);
            Assert.IsTrue(AccessRasApi.SetNetworkOutageTime(profileName, 0));
            Assert.IsNull(AccessRasApi.GetNetworkOutageTime(profileName));

            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
        }
    }
}