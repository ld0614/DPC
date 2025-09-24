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
    public class UserProfileTests
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
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void BasicUserProfile(ProfileType profileType)
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(profileType, false);
            profile.LoadUserProfile(profileName,
                    TunnelType.SplitTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    new List<string>() { "47beabc922eae80e78783462a79f45c254fde68b" },
                    new List<string>() { "27ac9369faf25207bb2627cefaccbe4ef9c319b8" },
                    new List<string>() { "NPS01.Test.local"},
                    routeList: new Dictionary<string, string>
                        {
                            { "10.0.0.0/8", "Server Network" }
                        }
                );
            profile.Generate(NetworkCapability.IPv4AndIpv6);
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
        [DataRow("")]
        [DataRow(" ")] //Non-breaking Space
        [DataRow("\"")]
        [DataRow("\'")]
        [DataRow("&")]
        [DataRow("<")]
        [DataRow(">")]
        [DataRow("¢")]
        [DataRow("£")]
        [DataRow("¥")]
        [DataRow("€")]
        [DataRow("©")]
        [DataRow("®")]
        [DataRow("™")]
        public void BasicUserProfileWithWeirdCharsInRouteName(string character)
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(ProfileType.User, false);
            profile.LoadUserProfile(profileName,
                    TunnelType.SplitTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    new List<string>() { "47beabc922eae80e78783462a79f45c254fde68b" },
                    new List<string>() { "27ac9369faf25207bb2627cefaccbe4ef9c319b8" },
                    new List<string>() { "NPS01.Test.local" },
                    routeList: new Dictionary<string, string>
                        {
                            { "10.0.0.0/8", "Server " + character + " Client Network" }
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
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void BasicUserProfileDisableNPSValidationNoNPSServers(ProfileType profileType)
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(profileType, false);
            profile.LoadUserProfile(profileName,
                    TunnelType.SplitTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    new List<string>() { "47beabc922eae80e78783462a79f45c254fde68b" },
                    new List<string>() { "27ac9369faf25207bb2627cefaccbe4ef9c319b8" },
                    routeList: new Dictionary<string, string>
                        {
                            { "10.0.0.0/8", "Server Network" }
                        },
                    disableNPSValidation: true
                );
            profile.Generate(NetworkCapability.IPv4AndIpv6);
            TestContext.WriteLine(profile.GetValidationFailures());
            TestContext.WriteLine(profile.GetValidationWarnings());
            Assert.IsFalse(profile.ValidateFailed());
            Assert.IsTrue(profile.ValidateWarnings()); //DisableNPSValidation should always cause a warning to be logged

            sharedData.AddProfileUpdate(profile.GetProfileUpdate());

            sharedData.HandleProfileUpdates();
            Assert.IsTrue(HelperFunctions.CheckProfileExists(profileName));

            //Assert that all updates have happened
            Assert.AreEqual(0, sharedData.PendingUpdates());

            //Check update was fully accepted by WMI
            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
        }

        [DataTestMethod]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void BasicUserProfileDisableNPSValidation(ProfileType profileType)
        {
            string profileName = TestContext.TestName;

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
                    disableNPSValidation: true
                );
            profile.Generate(NetworkCapability.IPv4AndIpv6);
            TestContext.WriteLine(profile.GetValidationFailures());
            TestContext.WriteLine(profile.GetValidationWarnings());
            Assert.IsFalse(profile.ValidateFailed());
            Assert.IsTrue(profile.ValidateWarnings()); //DisableNPSValidation should always cause a warning to be logged

            sharedData.AddProfileUpdate(profile.GetProfileUpdate());

            sharedData.HandleProfileUpdates();
            Assert.IsTrue(HelperFunctions.CheckProfileExists(profileName));

            //Assert that all updates have happened
            Assert.AreEqual(0, sharedData.PendingUpdates());

            //Check update was fully accepted by WMI
            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
        }

        [DataTestMethod]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void BasicUserProfileWithDeviceCompliance(ProfileType profileType)
        {
            string profileName = TestContext.TestName;

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
                    deviceComplianceEnabled: true,
                    deviceComplianceIssuerHash: "47beabc922eae80e78783462a79f45c254fde68b"
                );
            profile.Generate(NetworkCapability.IPv4AndIpv6);
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
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void BasicUserProfileWithDeviceComplianceCertificateOID(ProfileType profileType)
        {
            string profileName = TestContext.TestName;

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
                    deviceComplianceEnabled: true,
                    deviceComplianceEKUOID: "1.3.5.6.1.1000.1",
                    deviceComplianceIssuerHash: "47beabc922eae80e78783462a79f45c254fde68b"
                );
            profile.Generate(NetworkCapability.IPv4AndIpv6);
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
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void BasicUserProfileWithDeviceComplianceCertificateOIDAndCustomEKU(ProfileType profileType)
        {
            string profileName = TestContext.TestName;
            string EKUName = "Test EKU";
            string EKUOID = "1.3.4.6.1.2.3";

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
                    deviceComplianceEnabled: true,
                    deviceComplianceEKUOID: "1.3.5.6.1.1000.1",
                    deviceComplianceIssuerHash: "47beabc922eae80e78783462a79f45c254fde68b",
                    eKUMapping: true,
                    eKUName: EKUName,
                    eKUOID: EKUOID
                );
            profile.Generate(NetworkCapability.IPv4AndIpv6);
            TestContext.WriteLine(profile.GetValidationFailures());
            TestContext.WriteLine(profile.GetValidationWarnings());
            Assert.IsFalse(profile.ValidateFailed());
            Assert.IsTrue(profile.ValidateWarnings()); //EKU should conflict with Device Compliance

            sharedData.AddProfileUpdate(profile.GetProfileUpdate());

            sharedData.HandleProfileUpdates();
            Assert.IsTrue(HelperFunctions.CheckProfileExists(profileName));

            //Assert that all updates have happened
            Assert.AreEqual(0, sharedData.PendingUpdates());

            //Check update was fully accepted by WMI
            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
            VPNProfile profileDetails = new CSPProfile(profile.GetProfile(), profile.GetProfileName());
            Assert.IsFalse(profileDetails.EapConfig.Contains(EKUName)); //EKUName should have been overwritten with "AAD Conditional Access"
            Assert.IsTrue(profileDetails.EapConfig.Contains("AAD Conditional Access")); //EKUName should have been overwritten with "AAD Conditional Access"
            Assert.IsFalse(profileDetails.EapConfig.Contains(EKUOID)); //EKUOID should have been overwritten with "1.3.6.1.4.1.311.87"
            Assert.IsTrue(profileDetails.EapConfig.Contains("1.3.6.1.4.1.311.87")); //EKUOID should have been overwritten with "1.3.6.1.4.1.311.87"
        }

        [DataTestMethod]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void CompleteUserProfileWithDeviceCompliance(ProfileType profileType)
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(profileType, false);
            profile.LoadUserProfile(profileName,
                    TunnelType.ForceTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    new List<string>() { "47beabc922eae80e78783462a79f45c254fde68b" },
                    new List<string>() { "27ac9369faf25207bb2627cefaccbe4ef9c319b8", "aa6d9369faf25207bb2627cefaccbe4ef9c315cb" },
                    new List<string>() { "NPS01.Test.local", "NPS02.Test.local", "NPS03.Test.local" },
                    disableAlwaysOn: true,
                    trustedNetworkList: new List<string>() { "Test.local" },
                    deviceComplianceEnabled: true,
                    deviceComplianceIssuerHash: "47beabc922eae80e78783462a79f45c254fde68b",
                    excludeO365: true,
                    vPNStrategy: VPNStrategy.Ikev2Only,
                    customCryptography: true,
                    authenticationTransformConstants: "SHA256128",
                    cipherTransformConstants: "AES256",
                    pfsGroup: "PFS2048",
                    dHGroup: "Group14",
                    integrityCheckMethod: "SHA256",
                    encryptionMethod: "AES256",
                    registerDNS: true
                );;
            profile.Generate(NetworkCapability.IPv4AndIpv6);
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
            TestContext.WriteLine(profile.GetProfile());
        }

        //Not sure if this is a valid configuration but Windows Accepts it so DPC will too
        [DataTestMethod]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void BasicUserProfileWithWildcardDNSSuffixList(ProfileType profileType)
        {
            string profileName = TestContext.TestName;

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
                    dNSSuffixList: new List<string>() { ".Test.local" }
                );
            profile.Generate(NetworkCapability.IPv4AndIpv6);
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

        //Not sure if this is a valid configuration but Windows Accepts it so DPC will too
        [DataTestMethod]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void BasicUserProfileWithWildcardTrustedDomain(ProfileType profileType)
        {
            string profileName = TestContext.TestName;

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
                    trustedNetworkList: new List<string>() { ".Test.local" }
                );
            profile.Generate(NetworkCapability.IPv4AndIpv6);
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
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void BasicUserProfileWithMultipleTrustedDomain(ProfileType profileType)
        {
            string profileName = TestContext.TestName;

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
                    trustedNetworkList: new List<string>() { "XYZ-INTRANET", "MySite XYZ Intranet" }
                );
            profile.Generate(NetworkCapability.IPv4AndIpv6);
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
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void BasicUserProfileSSTPOnly(ProfileType profileType)
        {
            string profileName = TestContext.TestName;

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
                    vPNStrategy: VPNStrategy.SstpOnly
                );
            profile.Generate(NetworkCapability.IPv4AndIpv6);
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
            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
        }

        [DataTestMethod]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void UserWithDomainNameInfoSSTPAndRegisterDNS(ProfileType profileType)
        {
            string profileName = "Super Ninja Always On VPN User Tunnel (DPC)";

            VPNProfileCreator profile = new VPNProfileCreator(profileType, false);
            profile.LoadUserProfile(profileName,
                    TunnelType.SplitTunnel,
                    "aovpn.ninja.com",
                    new List<string>() { "87 67 7e 7d b2 cc 68 06 b6 bb 92 7b 59 3a fc 9a 97 a8  aa 5c" },
                    new List<string>() { "cf ff e6 51 3f d7 ee c4 ac 1b 1a b0 cc a2 ee 48 75 0a 8c bb" },
                    new List<string>() { "NPS01.Test.local" },
                    routeList: new Dictionary<string, string>
                        {
                            { "10.0.0.0/8", "Server Network" },
                            { "172.30.0.0/16", "Test-1 Random Networks" }
                        },
                    vPNStrategy: VPNStrategy.SstpOnly,
                    domainInformationList: new Dictionary<string, string>
                        {
                            { ".lab.ninja.local", "10.1.18.1, 10.1.18.2" }
                        },
                    registerDNS: true,
                    interfaceMetric: 1,
                    trustedNetworkList: new List<string>() { "ninja.online"}
                );
            profile.Generate(NetworkCapability.IPv4AndIpv6);
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
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void BasicUserProfileRegisterDNS(ProfileType profileType)
        {
            string profileName = TestContext.TestName;

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
                    vPNStrategy: VPNStrategy.SstpOnly,
                    registerDNS: true
                );
            profile.Generate(NetworkCapability.IPv4AndIpv6);
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
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void ForceUserSSTP(ProfileType profileType)
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(profileType, false);
            profile.LoadUserProfile(profileName,
                    TunnelType.ForceTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    new List<string>() { "47beabc922eae80e78783462a79f45c254fde68b" },
                    new List<string>() { "27ac9369faf25207bb2627cefaccbe4ef9c319b8" },
                    new List<string>() { "NPS01.Test.local" },
                    vPNStrategy: VPNStrategy.SstpFirst
                );
            profile.Generate(NetworkCapability.IPv4AndIpv6);
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
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void ForceUserOffice365(ProfileType profileType)
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(profileType, false);
            profile.LoadUserProfile(profileName,
                    TunnelType.ForceTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    new List<string>() { "47beabc922eae80e78783462a79f45c254fde68b" },
                    new List<string>() { "27ac9369faf25207bb2627cefaccbe4ef9c319b8" },
                    new List<string>() { "NPS01.Test.local" },
                    excludeO365: true
                );
            profile.Generate(NetworkCapability.IPv4AndIpv6);
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
        public void ForceUserOffice365BothTunnels()
        {
            string userProfileName = TestContext.TestName;
            string backupProfileName = TestContext.TestName + "-Backup";

            VPNProfileCreator userProfile = new VPNProfileCreator(ProfileType.User, false);
            userProfile.LoadUserProfile(userProfileName,
                    TunnelType.ForceTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    new List<string>() { "47beabc922eae80e78783462a79f45c254fde68b" },
                    new List<string>() { "27ac9369faf25207bb2627cefaccbe4ef9c319b8" },
                    new List<string>() { "NPS01.Test.local" },
                    excludeO365: true
                );
            userProfile.Generate(NetworkCapability.IPv4AndIpv6);
            TestContext.WriteLine(userProfile.GetValidationFailures());
            TestContext.WriteLine(userProfile.GetValidationWarnings());
            Assert.IsFalse(userProfile.ValidateFailed());
            Assert.IsFalse(userProfile.ValidateWarnings());

            VPNProfileCreator backupProfile = new VPNProfileCreator(ProfileType.UserBackup, false);
            backupProfile.LoadUserProfile(backupProfileName,
                    TunnelType.ForceTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    new List<string>() { "47beabc922eae80e78783462a79f45c254fde68b" },
                    new List<string>() { "27ac9369faf25207bb2627cefaccbe4ef9c319b8" },
                    new List<string>() { "NPS01.Test.local" },
                    excludeO365: true
                );
            backupProfile.Generate(NetworkCapability.IPv4AndIpv6);
            TestContext.WriteLine(backupProfile.GetValidationFailures());
            TestContext.WriteLine(backupProfile.GetValidationWarnings());
            Assert.IsFalse(backupProfile.ValidateFailed());
            Assert.IsFalse(backupProfile.ValidateWarnings());

            sharedData.AddProfileUpdate(userProfile.GetProfileUpdate());
            sharedData.AddProfileUpdate(backupProfile.GetProfileUpdate());

            sharedData.HandleProfileUpdates();
            Assert.IsTrue(HelperFunctions.CheckProfileExists(userProfileName));
            Assert.IsTrue(HelperFunctions.CheckProfileExists(backupProfileName));

            //Assert that all updates have happened
            Assert.AreEqual(0, sharedData.PendingUpdates());

            //Check update was fully accepted by WMI
            Assert.IsTrue(VPNProfile.CompareToInstalledProfile(userProfileName, userProfile.GetProfile(), TestContext.CancellationTokenSource.Token));
            Assert.IsTrue(VPNProfile.CompareToInstalledProfile(backupProfileName, backupProfile.GetProfile(), TestContext.CancellationTokenSource.Token));
        }

        [DataTestMethod]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void ForceUserSSTPWithCustomCrypto(ProfileType profileType)
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(profileType, false);
            profile.LoadUserProfile(profileName,
                    TunnelType.ForceTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    new List<string>() { "47beabc922eae80e78783462a79f45c254fde68b" },
                    new List<string>() { "27ac9369faf25207bb2627cefaccbe4ef9c319b8" },
                    new List<string>() { "NPS01.Test.local" },
                    vPNStrategy: VPNStrategy.Ikev2Only,
                    customCryptography: true,
                    authenticationTransformConstants: "SHA256128",
                    cipherTransformConstants: "AES128",
                    pfsGroup: "PFS2048",
                    dHGroup: "Group14",
                    integrityCheckMethod: "SHA256",
                    encryptionMethod: "AES128"
                );
            profile.Generate(NetworkCapability.IPv4AndIpv6);
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
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void BasicUserProfileDisableCryptoBinding(ProfileType profileType)
        {
            string profileName = TestContext.TestName;

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
                    disableCryptoBinding: true
                );
            profile.Generate(NetworkCapability.IPv4AndIpv6);
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
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void BasicUserProfileSmartCard(ProfileType profileType)
        {
            string profileName = TestContext.TestName;

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
                    enableEKUSmartCard: true
                );
            profile.Generate(NetworkCapability.IPv4AndIpv6);
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
        public void BasicUserAndBackupProfile()
        {
            //Create Primary Profile
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(ProfileType.User, false);
            profile.LoadUserProfile(profileName,
                    TunnelType.SplitTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    new List<string>() { "47beabc922eae80e78783462a79f45c254fde68b" },
                    new List<string>() { "27ac9369faf25207bb2627cefaccbe4ef9c319b8" },
                    new List<string>() { "NPS01.Test.local" },
                    routeList: new Dictionary<string, string>
                        {
                            { "10.0.0.0/8", "Server Network" }
                        }
                );
            profile.Generate(NetworkCapability.IPv4AndIpv6);

            //Create Backup Profile
            string backupProfileName = "Test Profile 2";

            VPNProfileCreator backupProfile = new VPNProfileCreator(ProfileType.UserBackup, false);
            backupProfile.LoadUserProfile(backupProfileName,
                    TunnelType.SplitTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    new List<string>() { "47beabc922eae80e78783462a79f45c254fde68b" },
                    new List<string>() { "27ac9369faf25207bb2627cefaccbe4ef9c319b8" },
                    new List<string>() { "NPS01.Test.local" },
                    routeList: new Dictionary<string, string>
                        {
                            { "10.0.0.0/8", "Server Network" }
                        }
                );

            //Generate Profiles
            profile.Generate(NetworkCapability.IPv4AndIpv6);
            backupProfile.Generate(NetworkCapability.IPv4AndIpv6);
            //Validate Primary Profile
            TestContext.WriteLine(profile.GetValidationFailures());
            TestContext.WriteLine(profile.GetValidationWarnings());
            Assert.IsFalse(profile.ValidateFailed());
            Assert.IsFalse(profile.ValidateWarnings());

            //Validate Backup Profile
            TestContext.WriteLine(backupProfile.GetValidationFailures());
            TestContext.WriteLine(backupProfile.GetValidationWarnings());
            Assert.IsFalse(backupProfile.ValidateFailed());
            Assert.IsFalse(backupProfile.ValidateWarnings());
            sharedData.AddProfileUpdate(profile.GetProfileUpdate());
            sharedData.AddProfileUpdate(backupProfile.GetProfileUpdate());

            sharedData.HandleProfileUpdates();
            Assert.IsTrue(HelperFunctions.CheckProfileExists(profileName));
            Assert.IsTrue(HelperFunctions.CheckProfileExists(backupProfileName));

            //Assert that all updates have happened
            Assert.AreEqual(0, sharedData.PendingUpdates());

            //Check update was fully accepted by WMI
            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
            Assert.IsTrue(VPNProfile.CompareToInstalledProfile(backupProfileName, backupProfile.GetProfile(), TestContext.CancellationTokenSource.Token));
        }

        [DataTestMethod]
        [TestCategory("TrafficFilters")]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void BasicUserProfileWithTrafficFilters(ProfileType profileType)
        {
            string profileName = TestContext.TestName;

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
            profile.Generate(NetworkCapability.IPv4AndIpv6);
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
        [TestCategory("TrafficFilters")]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void BasicUserProfileWithTrafficFiltersProtocolOnly(ProfileType profileType)
        {
            string profileName = TestContext.TestName;

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
                    trafficFilters: new List<TrafficFilter>() { new TrafficFilter("TF2") {
                                                                    Protocol = Protocol.TCP
                                                                }
                    }
                );
            profile.Generate(NetworkCapability.IPv4AndIpv6);
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
        [TestCategory("TrafficFilters")]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void BasicUserProfileWithTrafficFiltersPortAllowICMP(ProfileType profileType)
        {
            string profileName = TestContext.TestName;

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
                    trafficFilters: new List<TrafficFilter>() { new TrafficFilter("TF2") {
                                                                    Protocol = Protocol.ICMP
                                                                }
                    }
                );
            profile.Generate(NetworkCapability.IPv4AndIpv6);
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
        [TestCategory("TrafficFilters")]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void BasicUserProfileWithTrafficFiltersPortNoAppId(ProfileType profileType)
        {
            string profileName = TestContext.TestName;

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
                    trafficFilters: new List<TrafficFilter>() { new TrafficFilter("TF2") {
                                                                    Protocol = Protocol.TCP
                                                                }
                    }
                );
            profile.Generate(NetworkCapability.IPv4AndIpv6);
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
        [TestCategory("TrafficFilters")]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void BasicUserProfileWithTrafficFiltersForceTunnel(ProfileType profileType)
        {
            string profileName = TestContext.TestName;

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
                    trafficFilters: new List<TrafficFilter>() { new TrafficFilter("TF2") {
                                                                    AppId = "C:\\Test\\Test.exe",
                                                                    Protocol = Protocol.TCP,
                                                                    RoutingPolicyType = TunnelType.ForceTunnel
                                                                }
                    }
                );
            profile.Generate(NetworkCapability.IPv4AndIpv6);
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
        [TestCategory("TrafficFilters")]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void BasicUserProfileWithTrafficFiltersInboundRule(ProfileType profileType)
        {
            string profileName = TestContext.TestName;

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
                    trafficFilters: new List<TrafficFilter>() { new TrafficFilter("TF2") {
                                                                    AppId = "C:\\Test\\Test.exe",
                                                                    Protocol = Protocol.TCP,
                                                                    Direction = ProtocolDirection.Inbound
                                                                }
                    }
                );
            profile.Generate(NetworkCapability.IPv4AndIpv6);
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
        [TestCategory("TrafficFilters")]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void BasicUserProfileWithTrafficFiltersRemoteSplit(ProfileType profileType)
        {
            string profileName = TestContext.TestName;

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
            profile.Generate(NetworkCapability.IPv4AndIpv6);
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
        [TestCategory("TrafficFilters")]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void BasicUserProfileWithTrafficFiltersRemotePortSplit(ProfileType profileType)
        {
            string profileName = TestContext.TestName;

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
                    trafficFilters: new List<TrafficFilter>() { new TrafficFilter("TF2") {
                                                                    AppId = "C:\\Test\\Test.exe",
                                                                    Protocol = Protocol.TCP,
                                                                    RemotePorts = "80, 443",
                                                                    Direction = ProtocolDirection.Outbound,
                                                                    RoutingPolicyType = TunnelType.SplitTunnel
                                                                }
                    }
                );
            profile.Generate(NetworkCapability.IPv4AndIpv6);
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
        [TestCategory("TrafficFilters")]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void BasicUserProfileWithTrafficFiltersRemoteAddress(ProfileType profileType)
        {
            string profileName = TestContext.TestName;

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
                    trafficFilters: new List<TrafficFilter>() { new TrafficFilter("TF2") {
                                                                    AppId = "C:\\Test\\Test.exe",
                                                                    Protocol = Protocol.TCP,
                                                                    RemoteAddresses = "10.0.0.1,192.168.0.0/16",
                                                                    Direction = ProtocolDirection.Outbound,
                                                                    RoutingPolicyType = TunnelType.SplitTunnel
                                                                }
                    }
                );
            profile.Generate(NetworkCapability.IPv4AndIpv6);
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
        [TestCategory("TrafficFilters")]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void BasicUserProfileWithTrafficFiltersLocalAddress(ProfileType profileType)
        {
            string profileName = TestContext.TestName;

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
                    trafficFilters: new List<TrafficFilter>() { new TrafficFilter("TF2") {
                                                                    AppId = "C:\\Test\\Test.exe",
                                                                    Protocol = Protocol.TCP,
                                                                    LocalAddresses = "10.0.0.1, 192.168.0.0/16",
                                                                    Direction = ProtocolDirection.Outbound,
                                                                    RoutingPolicyType = TunnelType.SplitTunnel
                                                                }
                    }
                );
            profile.Generate(NetworkCapability.IPv4AndIpv6);
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
        [TestCategory("TrafficFilters")]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void BasicUserProfileWithTrafficFiltersAllOptionsSplit(ProfileType profileType)
        {
            string profileName = TestContext.TestName;

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
            profile.Generate(NetworkCapability.IPv4AndIpv6);
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
        [TestCategory("TrafficFilters")]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void BasicUserProfileWithTrafficFiltersAllOptionsSplitMixedPorts(ProfileType profileType)
        {
            string profileName = TestContext.TestName;

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
            profile.Generate(NetworkCapability.IPv4AndIpv6);
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
        [TestCategory("TrafficFilters")]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void BasicUserProfileWithTrafficFiltersLocalAndRemoteAddresses(ProfileType profileType)
        {
            string profileName = TestContext.TestName;

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
                    trafficFilters: new List<TrafficFilter>() { new TrafficFilter("TF2") {
                                                                    AppId = "C:\\Test\\Test.exe",
                                                                    Protocol = Protocol.TCP,
                                                                    RemoteAddresses = "192.168.0.0/16",
                                                                    LocalAddresses = "10.0.0.0/8"
                                                                }
                    }
                );
            profile.Generate(NetworkCapability.IPv4AndIpv6);
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
        [TestCategory("TrafficFilters")]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void BasicUserProfileWithTrafficFiltersAllOptionsForce(ProfileType profileType)
        {
            string profileName = TestContext.TestName;

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
            profile.Generate(NetworkCapability.IPv4AndIpv6);
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
        [TestCategory("TrafficFilters")]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void BasicUserProfileWithTrafficFiltersDefault(ProfileType profileType)
        {
            string profileName = TestContext.TestName;

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
                    trafficFilters: new List<TrafficFilter>() { new TrafficFilter("TF2") {
                                                                }
                    }
                );
            profile.Generate(NetworkCapability.IPv4AndIpv6);
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

        [DataTestMethod]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void BasicUserProfileWithExcludeRoutes(ProfileType profileType)
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(profileType, false);
            profile.LoadUserProfile(profileName,
                    TunnelType.SplitTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    new List<string>() { "47beabc922eae80e78783462a79f45c254fde68b" },
                    new List<string>() { "27ac9369faf25207bb2627cefaccbe4ef9c319b8" },
                    new List<string>() { "NPS01.Test.local" },
                    routeList: new Dictionary<string, string>
                        {
                            { "10.0.0.0/8", "Server Network" },
                            { "192.168.0.0/16", "Test Network" }
                        },
                    routeExcludeList: new Dictionary<string, string>
                        {
                            { "10.0.0.1", "Web Server 1" },
                            { "192.168.0.0/24", "Test  DMZNetwork" }
                        }
                );
            profile.Generate(NetworkCapability.IPv4AndIpv6);
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
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void BasicUserProfileWithRouteMetric(ProfileType profileType)
        {
            string profileName = TestContext.TestName;

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
                    routeMetric: 56
                );
            profile.Generate(NetworkCapability.IPv4AndIpv6);
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
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void BasicUserProfileWithExcludeRoutesAndRouteMetric(ProfileType profileType)
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(profileType, false);
            profile.LoadUserProfile(profileName,
                    TunnelType.SplitTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    new List<string>() { "47beabc922eae80e78783462a79f45c254fde68b" },
                    new List<string>() { "27ac9369faf25207bb2627cefaccbe4ef9c319b8" },
                    new List<string>() { "NPS01.Test.local" },
                    routeList: new Dictionary<string, string>
                        {
                            { "10.0.0.0/8", "Server Network" },
                            { "192.168.0.0/16", "Test Network" }
                        },
                    routeExcludeList: new Dictionary<string, string>
                        {
                            { "10.0.0.1", "Web Server 1" },
                            { "192.168.0.0/24", "Test  DMZNetwork" }
                        },
                    routeMetric: 99
                );
            profile.Generate(NetworkCapability.IPv4AndIpv6);
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
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void BasicUserProfileWithUpdatedRouteMetric(ProfileType profileType)
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(profileType, false);
            profile.LoadUserProfile(profileName,
                    TunnelType.SplitTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    new List<string>() { "47beabc922eae80e78783462a79f45c254fde68b" },
                    new List<string>() { "27ac9369faf25207bb2627cefaccbe4ef9c319b8" },
                    new List<string>() { "NPS01.Test.local" },
                    routeList: new Dictionary<string, string>
                        {
                            { "10.0.0.0/8", "Server Network" },
                            { "192.168.0.0/16", "Test Network" }
                        },
                    routeExcludeList: new Dictionary<string, string>
                        {
                            { "10.0.0.1", "Web Server 1" },
                            { "192.168.0.0/24", "Test  DMZNetwork" }
                        },
                    routeMetric: 99
                );
            profile.Generate(NetworkCapability.IPv4AndIpv6);
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

            profile.LoadUserProfile(profileName,
                    TunnelType.SplitTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    new List<string>() { "47beabc922eae80e78783462a79f45c254fde68b" },
                    new List<string>() { "27ac9369faf25207bb2627cefaccbe4ef9c319b8" },
                    new List<string>() { "NPS01.Test.local" },
                    routeList: new Dictionary<string, string>
                        {
                            { "10.0.0.0/8", "Server Network" },
                            { "192.168.0.0/16", "Test Network" }
                        },
                    routeExcludeList: new Dictionary<string, string>
                        {
                            { "10.0.0.1", "Web Server 1" },
                            { "192.168.0.0/24", "Test  DMZNetwork" }
                        },
                    routeMetric: 10 //UPDATED
                );
            profile.Generate(NetworkCapability.IPv4AndIpv6);
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


        public void BasicForceTunnelUserProfile()
        {
            string profileName = TestContext.TestName;
            ProfileType profileType = ProfileType.User;

            VPNProfileCreator profile = new VPNProfileCreator(profileType, false);
            profile.LoadUserProfile(profileName,
                    TunnelType.ForceTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    new List<string>() { "47beabc922eae80e78783462a79f45c254fde68b" },
                    new List<string>() { "27ac9369faf25207bb2627cefaccbe4ef9c319b8" },
                    new List<string>() { "NPS01.Test.local" }
                );
            profile.Generate(NetworkCapability.IPv4AndIpv6);
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
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void BasicForceTunnelUserProfileWithRouteList(ProfileType profileType)
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(profileType, false);
            profile.LoadUserProfile(profileName,
                    TunnelType.ForceTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    new List<string>() { "47beabc922eae80e78783462a79f45c254fde68b" },
                    new List<string>() { "27ac9369faf25207bb2627cefaccbe4ef9c319b8" },
                    new List<string>() { "NPS01.Test.local" },
                    routeList: new Dictionary<string, string>
                        {
                            { "10.0.0.0/8", "Server Network" }
                        }
                );
            profile.Generate(NetworkCapability.IPv4AndIpv6);
            TestContext.WriteLine(profile.GetValidationFailures());
            TestContext.WriteLine(profile.GetValidationWarnings());
            Assert.IsFalse(profile.ValidateFailed());
            Assert.IsFalse(profile.ValidateWarnings()); //routeList no longer warns when additional routes are added

            sharedData.AddProfileUpdate(profile.GetProfileUpdate());

            sharedData.HandleProfileUpdates();
            Assert.IsTrue(HelperFunctions.CheckProfileExists(profileName));

            //Assert that all updates have happened
            Assert.AreEqual(0, sharedData.PendingUpdates());

            //Check update was fully accepted by WMI
            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
        }

        [DataTestMethod]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void BasicForceTunnelDNSRegisteredUserProfile(ProfileType profileType)
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(profileType, false);
            profile.LoadUserProfile(profileName,
                    TunnelType.ForceTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    new List<string>() { "47beabc922eae80e78783462a79f45c254fde68b" },
                    new List<string>() { "27ac9369faf25207bb2627cefaccbe4ef9c319b8" },
                    new List<string>() { "NPS01.Test.local" },
                    registerDNS: true,
                    dnsAlreadyRegistered: false
                );
            profile.Generate(NetworkCapability.IPv4AndIpv6);
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
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void BasicForceTunnelDNSRegisteredOnBothTunnelsUserProfile(ProfileType profileType)
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(profileType, false);
            profile.LoadUserProfile(profileName,
                    TunnelType.ForceTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    new List<string>() { "47beabc922eae80e78783462a79f45c254fde68b" },
                    new List<string>() { "27ac9369faf25207bb2627cefaccbe4ef9c319b8" },
                    new List<string>() { "NPS01.Test.local" },
                    registerDNS: true,
                    dnsAlreadyRegistered: true
                );
            profile.Generate(NetworkCapability.IPv4AndIpv6);
            TestContext.WriteLine(profile.GetValidationFailures());
            TestContext.WriteLine(profile.GetValidationWarnings());
            Assert.IsFalse(profile.ValidateFailed());
            Assert.IsTrue(profile.ValidateWarnings()); //DNS Should warn as it is already enabled on the Device Tunnel

            sharedData.AddProfileUpdate(profile.GetProfileUpdate());

            sharedData.HandleProfileUpdates();
            Assert.IsTrue(HelperFunctions.CheckProfileExists(profileName));

            //Assert that all updates have happened
            Assert.AreEqual(0, sharedData.PendingUpdates());

            //Check update was fully accepted by WMI
            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
        }

        [DataTestMethod]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void BasicUserProfileWithIPv6Routes(ProfileType profileType)
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(profileType, false);
            profile.LoadUserProfile(profileName,
                    TunnelType.SplitTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    new List<string>() { "47beabc922eae80e78783462a79f45c254fde68b" },
                    new List<string>() { "27ac9369faf25207bb2627cefaccbe4ef9c319b8" },
                    new List<string>() { "NPS01.Test.local" },
                    routeList: new Dictionary<string, string>
                        {
                            { "10.0.0.0/8", "Server Network" },
                            {"2620:1ec:908::/46", "IPv6 Range" },
                            {"542:dec:295::/46", "IPv6 Range 2" }
                        }
                );
            profile.Generate(NetworkCapability.IPv4AndIpv6);
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
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void BasicUserProfileWithDefaultRoutes(ProfileType profileType)
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(profileType, false);
            profile.LoadUserProfile(profileName,
                    TunnelType.SplitTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    new List<string>() { "47beabc922eae80e78783462a79f45c254fde68b" },
                    new List<string>() { "27ac9369faf25207bb2627cefaccbe4ef9c319b8" },
                    new List<string>() { "NPS01.Test.local" },
                    routeList: new Dictionary<string, string>
                        {
                            { "0.0.0.0/0", "IPv4 Range" },
                            {"::/0", "IPv6 Range" }
                        },
                    routeExcludeList: new Dictionary<string, string>
                    {
                        {"2620:1ec:908::/46", "Office Route" },
                        {"2a01:111:f402::/48", "Office Route" }
                    }
                );
            profile.Generate(NetworkCapability.IPv4AndIpv6);
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
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void BasicUserProfileWithIPv6ExcludeRoutes(ProfileType profileType)
        {
            string profileName = TestContext.TestName;

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
                    routeExcludeList: new Dictionary<string, string>
                    {
                        {"2620:1ec:908::/46", "Office Route" },
                        {"10.1.0.0/16", "Office Route" },
                        {"2a01:111:f402::/48", "Office Route" }
                    }
                );
            profile.Generate(NetworkCapability.IPv4AndIpv6);
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
            Assert.AreEqual(0, VPNProfile.CompareToInstalledProfileWithResults(profileName, profile.GetProfile(), TestContext.CancellationTokenSource.Token).Count);
            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
        }

        [DataTestMethod]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void BasicUserWithRoutesProfile(ProfileType profileType)
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(profileType, false);
            profile.LoadUserProfile(profileName,
                    TunnelType.SplitTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    new List<string>() { "47beabc922eae80e78783462a79f45c254fde68b" },
                    new List<string>() { "27ac9369faf25207bb2627cefaccbe4ef9c319b8" },
                    new List<string>() { "NPS01.Test.local" },
                    routeList: new Dictionary<string, string>()
                    {
                        { "192.168.200.1", "" },
                        { "10.0.0.0/8", "Internal" },
                        { "20.56.241.0/24", "External Route" },
                    }
                );
            profile.Generate(NetworkCapability.IPv4AndIpv6);
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
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void BasicUserProfileWithDomainNameInfo(ProfileType profileType)
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(profileType, false);
            profile.LoadUserProfile(profileName,
                    TunnelType.SplitTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    new List<string>() { "47beabc922eae80e78783462a79f45c254fde68b" },
                    new List<string>() { "27ac9369faf25207bb2627cefaccbe4ef9c319b8" },
                    new List<string>() { "NPS01.Test.local" },
                    domainInformationList: new Dictionary<string, string>
                        {
                            { ".", "192.168.0.1, 192.168.0.2" },
                            { ".example.com", "192.168.0.1,192.168.0.2" },
                            { ".thisisatest.com", "10.0.0.15" },
                            { "www.example.com", "" },
                            { ".test.local", "" }
                        },
                    routeList: new Dictionary<string, string>
                        {
                            { "10.0.0.0/8", "Server Network" }
                        }
                );
            profile.Generate(NetworkCapability.IPv4AndIpv6);
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

        [DataTestMethod]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void BasicUserProfileWithDomainNameInfoAndTrustedNetwork(ProfileType profileType)
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(profileType, false);
            profile.LoadUserProfile(profileName,
                    TunnelType.SplitTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    new List<string>() { "47beabc922eae80e78783462a79f45c254fde68b" },
                    new List<string>() { "27ac9369faf25207bb2627cefaccbe4ef9c319b8" },
                    new List<string>() { "NPS01.Test.local" },
                    domainInformationList: new Dictionary<string, string>
                        {
                            { ".", "192.168.0.1,192.168.0.2" },
                            { ".example.com", "192.168.0.1,192.168.0.2" },
                            { ".thisisatest.com", "10.0.0.15" },
                            { "www.example.com", "" },
                            { ".test.local", "" }
                        },
                    trustedNetworkList: new List<string>() { "test.com", "example.net"},
                    routeList: new Dictionary<string, string>
                        {
                            { "10.0.0.0/8", "Server Network" }
                        }
                );
            profile.Generate(NetworkCapability.IPv4AndIpv6);
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
        [DataRow((uint)576)]
        [DataRow((uint)1000)]
        [DataRow((uint)1200)]
        [DataRow((uint)1300)]
        [DataRow((uint)1400)]
        public void BasicUserProfileWithMTU(uint MTUValue)
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(ProfileType.User, false);
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
                    mTU: MTUValue

                );
            profile.Generate(NetworkCapability.IPv4AndIpv6);
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
        [DataRow((uint)0)]
        [DataRow((uint)1)]
        [DataRow((uint)400)]
        [DataRow((uint)1500)]
        public void BasicUserProfileWithMTUWarning(uint MTUValue)
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(ProfileType.User, false);
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
                    mTU: MTUValue

                );
            profile.Generate(NetworkCapability.IPv4AndIpv6);
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

        [DataTestMethod]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void BasicUserProfileWithTrustedNetwork(ProfileType profileType)
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(profileType, false);
            profile.LoadUserProfile(profileName,
                    TunnelType.SplitTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    new List<string>() { "47beabc922eae80e78783462a79f45c254fde68b" },
                    new List<string>() { "27ac9369faf25207bb2627cefaccbe4ef9c319b8" },
                    new List<string>() { "NPS01.Test.local" },
                    trustedNetworkList: new List<string>() { "test.com", "example.net" },
                    routeList: new Dictionary<string, string>
                        {
                            { "10.0.0.0/8", "Server Network" }
                        }
                );
            profile.Generate(NetworkCapability.IPv4AndIpv6);
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
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void BasicForceUserProfileWithManualProxy(ProfileType profileType)
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(profileType, false);
            profile.LoadUserProfile(profileName,
                    TunnelType.ForceTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    new List<string>() { "47beabc922eae80e78783462a79f45c254fde68b" },
                    new List<string>() { "27ac9369faf25207bb2627cefaccbe4ef9c319b8" },
                    new List<string>() { "NPS01.Test.local" },
                    useProxy: true,
                    proxyType: ProxyType.Manual,
                    proxyValue: "http://proxy.test.local:8080"
                );
            profile.Generate(NetworkCapability.IPv4AndIpv6);
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
        [DataRow("http://proxy.test.local:8080")]
        [DataRow("https://proxy.test.local")]
        [DataRow("proxy.test.local:8080")]
        [DataRow("proxy.test.local")]
        [DataRow("http://proxy.test.local")]
        public void BasicForceUserProfileWithManualProxyAndExclusions(string proxyAddress)
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(ProfileType.User, false);
            profile.LoadUserProfile(profileName,
                    TunnelType.ForceTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    new List<string>() { "47beabc922eae80e78783462a79f45c254fde68b" },
                    new List<string>() { "27ac9369faf25207bb2627cefaccbe4ef9c319b8" },
                    new List<string>() { "NPS01.Test.local" },
                    useProxy: true,
                    proxyType: ProxyType.Manual,
                    proxyValue: proxyAddress,
                    proxyBypassForLocal: true,
                    proxyExcludeList: new List<string>() { "*.test.local", "www.test.com"}
                );
            profile.Generate(NetworkCapability.IPv4AndIpv6);
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
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void BasicForceUserProfileWithSmartCard(ProfileType profileType)
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(profileType, false);
            profile.LoadUserProfile(profileName,
                    TunnelType.ForceTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    new List<string>() { "47beabc922eae80e78783462a79f45c254fde68b" },
                    new List<string>() { "27ac9369faf25207bb2627cefaccbe4ef9c319b8" },
                    new List<string>() { "NPS01.Test.local" },
                    useProxy: true,
                    proxyType: ProxyType.Manual,
                    proxyValue: "http://proxy.test.local:8080",
                    enableEKUSmartCard: true
                );
            profile.Generate(NetworkCapability.IPv4AndIpv6);
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
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void BasicForceUserProfileWithPACProxy(ProfileType profileType)
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(profileType, false);
            profile.LoadUserProfile(profileName,
                    TunnelType.ForceTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    new List<string>() { "47beabc922eae80e78783462a79f45c254fde68b" },
                    new List<string>() { "27ac9369faf25207bb2627cefaccbe4ef9c319b8" },
                    new List<string>() { "NPS01.Test.local" },
                    useProxy:true,
                    proxyType:ProxyType.PAC,
                    proxyValue:"http://proxy.test.local/proxyfile.pac"
                );
            profile.Generate(NetworkCapability.IPv4AndIpv6);
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
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void BasicForceUserProfileWithPACProxyWithExclusion(ProfileType profileType)
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(profileType, false);
            profile.LoadUserProfile(profileName,
                    TunnelType.ForceTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    new List<string>() { "47beabc922eae80e78783462a79f45c254fde68b" },
                    new List<string>() { "27ac9369faf25207bb2627cefaccbe4ef9c319b8" },
                    new List<string>() { "NPS01.Test.local" },
                    useProxy: true,
                    proxyType: ProxyType.PAC,
                    proxyValue: "http://proxy.test.local/proxyfile.pac",
                    proxyExcludeList: new List<string>() { "exclude.me.local"}
                );
            profile.Generate(NetworkCapability.IPv4AndIpv6);
            TestContext.WriteLine(profile.GetValidationFailures());
            TestContext.WriteLine(profile.GetValidationWarnings());
            Assert.IsFalse(profile.ValidateFailed());
            Assert.IsTrue(profile.ValidateWarnings()); //Expect warning about bad excluded proxy settings

            sharedData.AddProfileUpdate(profile.GetProfileUpdate());

            sharedData.HandleProfileUpdates();
            Assert.IsTrue(HelperFunctions.CheckProfileExists(profileName));

            //Assert that all updates have happened
            Assert.AreEqual(0, sharedData.PendingUpdates());

            //Check update was fully accepted by WMI
            HelperFunctions.AssertProfileMatches(profileName, profile.GetProfile(), TestContext);
        }

        [DataTestMethod]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void BasicForceUserProfileWithPACProxyWithSilentExclusion(ProfileType profileType)
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(profileType, false);
            profile.LoadUserProfile(profileName,
                    TunnelType.ForceTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    new List<string>() { "47beabc922eae80e78783462a79f45c254fde68b" },
                    new List<string>() { "27ac9369faf25207bb2627cefaccbe4ef9c319b8" },
                    new List<string>() { "NPS01.Test.local" },
                    useProxy: true,
                    proxyType: ProxyType.PAC,
                    proxyValue: "http://proxy.test.local/proxyfile.pac",
                    proxyExcludeList: new List<string>() { "PACFILE" }
                );
            profile.Generate(NetworkCapability.IPv4AndIpv6);
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
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void BasicUserForceTunnelProfileWithDomainNameInfo(ProfileType profileType)
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(profileType, false);
            profile.LoadUserProfile(profileName,
                    TunnelType.ForceTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    new List<string>() { "47beabc922eae80e78783462a79f45c254fde68b" },
                    new List<string>() { "27ac9369faf25207bb2627cefaccbe4ef9c319b8" },
                    new List<string>() { "NPS01.Test.local" },
                    domainInformationList: new Dictionary<string, string>
                        {
                            { ".", "192.168.0.1, 192.168.0.2" },
                            { ".example.com", "192.168.0.1,192.168.0.2" },
                            { ".thisisatest.com", "10.0.0.15" },
                            { "www.example.com", "" },
                            { ".test.local", "" }
                        },
                    routeExcludeList: new Dictionary<string, string>
                        {
                            { "10.0.0.0/8", "Server Network" }
                        }
                );
            profile.Generate(NetworkCapability.IPv4AndIpv6);
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

        [DataTestMethod]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void BasicUserForceTunnelProfileWithDomainNameInfoAndTrustedNetwork(ProfileType profileType)
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(profileType, false);
            profile.LoadUserProfile(profileName,
                    TunnelType.ForceTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    new List<string>() { "47beabc922eae80e78783462a79f45c254fde68b" },
                    new List<string>() { "27ac9369faf25207bb2627cefaccbe4ef9c319b8" },
                    new List<string>() { "NPS01.Test.local" },
                    domainInformationList: new Dictionary<string, string>
                        {
                            { ".", "192.168.0.1, 192.168.0.2" },
                            { ".example.com", "192.168.0.1,192.168.0.2" },
                            { ".thisisatest.com", "10.0.0.15" },
                            { "www.example.com", "" },
                            { ".test.local", "" }
                        },
                    trustedNetworkList: new List<string>() { "test.com", "example.net" },
                    routeExcludeList: new Dictionary<string, string>
                        {
                            { "10.0.0.0/8", "Server Network" }
                        }
                );
            profile.Generate(NetworkCapability.IPv4AndIpv6);
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
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void BasicUserForceTunnelProfileWithTrustedNetwork(ProfileType profileType)
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(profileType, false);
            profile.LoadUserProfile(profileName,
                    TunnelType.ForceTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    new List<string>() { "47beabc922eae80e78783462a79f45c254fde68b" },
                    new List<string>() { "27ac9369faf25207bb2627cefaccbe4ef9c319b8" },
                    new List<string>() { "NPS01.Test.local" },
                    trustedNetworkList: new List<string>() { "test.com", "example.net" },
                    routeExcludeList: new Dictionary<string, string>
                        {
                            { "10.0.0.0/8", "Server Network" }
                        }
                );
            profile.Generate(NetworkCapability.IPv4AndIpv6);
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
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void BasicUserForceTunnelProfileWithManualProxy(ProfileType profileType)
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(profileType, false);
            profile.LoadUserProfile(profileName,
                    TunnelType.ForceTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    new List<string>() { "47beabc922eae80e78783462a79f45c254fde68b" },
                    new List<string>() { "27ac9369faf25207bb2627cefaccbe4ef9c319b8" },
                    new List<string>() { "NPS01.Test.local" },
                    useProxy: true,
                    proxyType: ProxyType.Manual,
                    proxyValue: "http://proxy.test.local:8080",
                    routeExcludeList: new Dictionary<string, string>
                        {
                            { "10.0.0.0/8", "Network" }
                        }
                );
            profile.Generate(NetworkCapability.IPv4AndIpv6);
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
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void BasicUserForceTunnelProfileWithPACProxy(ProfileType profileType)
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(profileType, false);
            profile.LoadUserProfile(profileName,
                    TunnelType.ForceTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    new List<string>() { "47beabc922eae80e78783462a79f45c254fde68b" },
                    new List<string>() { "27ac9369faf25207bb2627cefaccbe4ef9c319b8" },
                    new List<string>() { "NPS01.Test.local" },
                    useProxy: true,
                    proxyType: ProxyType.PAC,
                    proxyValue: "http://proxy.test.local/proxyfile.pac",
                    routeExcludeList: new Dictionary<string, string>
                        {
                            { "10.0.0.0/8", "Server Network" }
                        }
                );
            profile.Generate(NetworkCapability.IPv4AndIpv6);
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
        [TestCategory("OverrideProfile")]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void OverrideUserProfile(ProfileType profileType)
        {
            string profileName = TestContext.TestName;
            string overrideProfile = "<VPNProfile><AlwaysOn>true</AlwaysOn><NativeProfile><Servers>aovpn.somewhere.net</Servers><RoutingPolicyType>SplitTunnel</RoutingPolicyType><NativeProtocolType>Ikev2</NativeProtocolType><DisableClassBasedDefaultRoute>true</DisableClassBasedDefaultRoute><Authentication><UserMethod>Eap</UserMethod><Eap><Configuration><EapHostConfig xmlns=\"http://www.microsoft.com/provisioning/EapHostConfig\"><EapMethod><Type xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">25</Type><VendorId xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">0</VendorId><VendorType xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">0</VendorType><AuthorId xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">0</AuthorId></EapMethod><Config xmlns=\"http://www.microsoft.com/provisioning/EapHostConfig\"><Eap xmlns=\"http://www.microsoft.com/provisioning/BaseEapConnectionPropertiesV1\"><Type>25</Type><EapType xmlns=\"http://www.microsoft.com/provisioning/MsPeapConnectionPropertiesV1\"><ServerValidation><DisableUserPromptForServerValidation>true</DisableUserPromptForServerValidation><ServerNames>NPS-01.somewhere.local</ServerNames><TrustedRootCA>AA BB CC DD EE FF 00 11 22 33 44 55 66 77 88 99 AA BB CC DD</TrustedRootCA></ServerValidation><FastReconnect>true</FastReconnect><InnerEapOptional>false</InnerEapOptional><Eap xmlns=\"http://www.microsoft.com/provisioning/BaseEapConnectionPropertiesV1\"><Type>13</Type><EapType xmlns=\"http://www.microsoft.com/provisioning/EapTlsConnectionPropertiesV1\"><CredentialsSource><CertificateStore><SimpleCertSelection>true</SimpleCertSelection></CertificateStore></CredentialsSource><ServerValidation><DisableUserPromptForServerValidation>true</DisableUserPromptForServerValidation><ServerNames>NPS-01.somewhere.local</ServerNames><TrustedRootCA>AA BB CC DD EE FF 00 11 22 33 44 55 66 77 88 99 AA BB CC DD</TrustedRootCA></ServerValidation><DifferentUsername>false</DifferentUsername><PerformServerValidation xmlns=\"http://www.microsoft.com/provisioning/EapTlsConnectionPropertiesV2\">true</PerformServerValidation><AcceptServerName xmlns=\"http://www.microsoft.com/provisioning/EapTlsConnectionPropertiesV2\">true</AcceptServerName><TLSExtensions xmlns=\"http://www.microsoft.com/provisioning/EapTlsConnectionPropertiesV2\"><FilteringInfo xmlns=\"http://www.microsoft.com/provisioning/EapTlsConnectionPropertiesV3\"><CAHashList Enabled=\"true\"><IssuerHash>CC DD EE FF 00 11 22 33 44 55 66 77 88 99 AA BB CC DD EE FF</IssuerHash></CAHashList></FilteringInfo></TLSExtensions></EapType></Eap><EnableQuarantineChecks>false</EnableQuarantineChecks><RequireCryptoBinding>false</RequireCryptoBinding><PeapExtensions><PerformServerValidation xmlns=\"http://www.microsoft.com/provisioning/MsPeapConnectionPropertiesV2\">true</PerformServerValidation><AcceptServerName xmlns=\"http://www.microsoft.com/provisioning/MsPeapConnectionPropertiesV2\">true</AcceptServerName></PeapExtensions></EapType></Eap></Config></EapHostConfig></Configuration></Eap></Authentication></NativeProfile></VPNProfile>";

            VPNProfileCreator profile = new VPNProfileCreator(profileType, false);
            profile.LoadUserProfile(profileName,
                    TunnelType.SplitTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    new List<string>() { "47beabc922eae80e78783462a79f45c254fde68b" },
                    new List<string>() { "27ac9369faf25207bb2627cefaccbe4ef9c319b8" },
                    new List<string>() { "NPS01.Test.local" },
                    overrideProfile
                );
            profile.Generate(NetworkCapability.IPv4AndIpv6);

            VPNProfileCreator originalProfile = new VPNProfileCreator(profileType, false);
            originalProfile.LoadUserProfile(profileName,
                    TunnelType.SplitTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    new List<string>() { "47beabc922eae80e78783462a79f45c254fde68b" },
                    new List<string>() { "27ac9369faf25207bb2627cefaccbe4ef9c319b8" },
                    new List<string>() { "NPS01.Test.local" }
                );
            originalProfile.Generate(NetworkCapability.IPv4AndIpv6);

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
        [TestCategory("OverrideProfile")]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void OverrideUserProfileErrorsAreWarnings(ProfileType profileType)
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(profileType, false);
            profile.LoadUserProfile(profileName,
                    TunnelType.SplitTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    new List<string>() { "47beabc922eae80e78783462a79f45c254fde68b" },
                    new List<string>() { "27ac9369faf25207bb2627cefaccbe4ef9c319b8" },
                    new List<string>() { "NPS01.Test.local" },
                    "<VPNProfile><NativeProfile><NativeProtocolType>Automatic</NativeProtocolType><Authentication><UserMethod>Mschapv2</UserMethod></Authentication></NativeProfile></VPNProfile>",
                    vPNStrategy: VPNStrategy.SstpFirst //As NativeProtocolType is set to Automatic the default VPNStrategy is actually SSTPFirst so we need to set this directly as the DPC default is IKEv2Only
                );
            profile.Generate(NetworkCapability.IPv4AndIpv6);
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
        }

        [DataTestMethod]
        [TestCategory("OverrideProfile")]
        [DataRow(ProfileType.User)]
        [DataRow(ProfileType.UserBackup)]
        public void OverrideUserProfileErrorsAreWarningsDefaultVPNStrategy(ProfileType profileType)
        {
            string profileName = TestContext.TestName;

            VPNProfileCreator profile = new VPNProfileCreator(profileType, false);
            profile.LoadUserProfile(profileName,
                    TunnelType.SplitTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    new List<string>() { "47beabc922eae80e78783462a79f45c254fde68b" },
                    new List<string>() { "27ac9369faf25207bb2627cefaccbe4ef9c319b8" },
                    new List<string>() { "NPS01.Test.local" },
                    "<VPNProfile><NativeProfile><NativeProtocolType>Automatic</NativeProtocolType><Authentication><UserMethod>Mschapv2</UserMethod></Authentication></NativeProfile></VPNProfile>"
                ); //Don't update the DPC VPN Strategy so default it to IKEv2 Only. This will trigger an update as NativeProtocolType is set to automatic which is SSTPFirst
            profile.Generate(NetworkCapability.IPv4AndIpv6);
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

            //Clear Event ID 1160 as this will be triggered due to the bad profile being installed and therefore can't update the VPN Strategy from the originally installed strategy
            HelperFunctions.ClearSpecificEventId(1160); //Failed to Update VPN Strategy
        }

        [DataTestMethod]
        [DataRow("")]
        [DataRow(null)]
        [DataRow("%TEMP%")]
        [DataRow("bob")]
        [DataRow("C:\\Windows\\Temp")]
        [DataRow("C:\\Windows\\Temp\\profile.xml")]
        [DataRow("C:\\Windows\\Temp\\Profile2")]
        public void BasicUserDebugSave(string savePath)
        {
            string profileName = TestContext.TestName;
            ProfileType profileType = ProfileType.User;

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
                        }
                );
            profile.Generate(NetworkCapability.IPv4AndIpv6);
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
                    Assert.IsTrue(VPNProfile.CompareProfiles(new CSPProfile(profile.GetProfile(), profile.GetProfileName()), new CSPProfile(fileText, profileName)));
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


        [DataTestMethod]
        [TestCategory("WMIWorking")]
        [DataRow("")]
        [DataRow(null)]
        [DataRow("%TEMP%")]
        [DataRow("bob")]
        [DataRow("C:\\Windows\\Temp")]
        [DataRow("C:\\Windows\\Temp\\profile.xml")]
        [DataRow("C:\\Windows\\Temp\\Profile2")]
        public void BasicUserBackupDebugSaveWin10(string savePath)
        {
            string profileName = TestContext.TestName;
            ProfileType profileType = ProfileType.UserBackup;

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
                        }
                );
            profile.Generate(NetworkCapability.IPv4AndIpv6);
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
                    Assert.IsTrue(VPNProfile.CompareProfiles(new CSPProfile(profile.GetProfile(), profile.GetProfileName()), new CSPProfile(fileText, profileName)));
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
