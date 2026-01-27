using DPCLibrary.Enums;
using DPCLibrary.Models;
using DPCLibrary.Utils;
using DPCService.Models;
using DPCService.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PEFile;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace ServiceIntegrationTests
{
    [TestClass]
    [TestCategory("IntergrationWithoutAdmin")]
    public class SharedDataTests
    {
        /// <summary>
        ///  Gets or sets the test context which provides
        ///  information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext { get; set; }

        //Maintain a single, consistent view of the OS State across Tests to help with clearing profiles
        private SharedData SharedDataForTestCleanup;

        [TestInitialize]
        public void PreTestInitialize()
        {
            SharedDataForTestCleanup = new SharedData(60, true, false, TestContext.CancellationTokenSource.Token);
            HelperFunctions.ClearETWEvents();
            HelperFunctions.ClearProfiles(SharedDataForTestCleanup);
            HelperFunctions.ClearRegistry();
        }

        [TestCleanup]
        public void PostTestCleanup()
        {
            HelperFunctions.ClearProfiles(SharedDataForTestCleanup);
            HelperFunctions.CheckNoErrors(TestContext);
        }

        private SharedData CreateBasicSharedData(bool updateOnConnected)
        {
            SharedData sharedData = new SharedData(60, updateOnConnected, false, TestContext.CancellationTokenSource.Token);
            Assert.AreEqual(sharedData.PendingUpdates(), 0); //No updates based on nothing to do
            Assert.AreEqual(sharedData.GetManagedProfileList().Count, 0); //No profiles being tracked
            return sharedData;
        }

        private ManagedProfile CreateBasicUserProfileUpdate(string profileName)
        {
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
            TestContext.WriteLine(profile.GetValidationFailures());
            TestContext.WriteLine(profile.GetValidationWarnings());
            Assert.IsFalse(profile.ValidateFailed());
            Assert.IsFalse(profile.ValidateWarnings());

            return profile.GetProfileUpdate();
        }

        [TestMethod]
        public void InitialBasicSetup()
        {
            SharedData sharedData = CreateBasicSharedData(true);
            Assert.AreEqual(sharedData.PendingUpdates(), 0); //No updates based on nothing to do
            Assert.AreEqual(sharedData.GetManagedProfileList().Count, 0); //No profiles being tracked
        }

        [TestMethod]
        public void InitialSetupManagedNamesExistWithoutProfile()
        {
            AccessRegistry.SaveMachineData(AccessRegistry.GetProfileNameRegistryName(ProfileType.Machine), "Machine Profile");
            AccessRegistry.SaveMachineData(AccessRegistry.GetProfileNameRegistryName(ProfileType.User), "User Profile");
            AccessRegistry.SaveMachineData(AccessRegistry.GetProfileNameRegistryName(ProfileType.UserBackup), "User Backup Profile");

            SharedData sharedData = new SharedData(60, false, false, TestContext.CancellationTokenSource.Token);
            Assert.AreEqual(sharedData.PendingUpdates(), 0); //No updates based on nothing to do
            Assert.AreEqual(sharedData.GetManagedProfileList().Count, 0); //Both profiles can't be found so they are not classed as monitored
        }

        [TestMethod]
        [TestCategory("MachineTunnel")]
        public void InitialSetupManagedNamesExistWithProfile()
        {
            AccessRegistry.SaveMachineData(AccessRegistry.GetProfileNameRegistryName(ProfileType.Machine), "Machine Profile");
            AccessRegistry.SaveMachineData(AccessRegistry.GetProfileNameRegistryName(ProfileType.User), "User Profile");
            AccessRegistry.SaveMachineData(AccessRegistry.GetProfileNameRegistryName(ProfileType.UserBackup), "User Backup Profile");

            //Create Machine Profile outside of DPC Context
            ManagedProfile machineProfile = new ManagedProfile()
            {
                ProfileName = "Machine Profile",
                ProfileXML = "<VPNProfile><AlwaysOn>true</AlwaysOn><DeviceTunnel>true</DeviceTunnel><DnsSuffix>example.local</DnsSuffix><TrustedNetworkDetection>example.local</TrustedNetworkDetection><NativeProfile><Servers>aovpn.test.com;aovpn.test.com</Servers><RoutingPolicyType>SplitTunnel</RoutingPolicyType><NativeProtocolType>Ikev2</NativeProtocolType><Authentication><MachineMethod>Certificate</MachineMethod></Authentication><CryptographySuite><AuthenticationTransformConstants>SHA256128</AuthenticationTransformConstants><CipherTransformConstants>AES128</CipherTransformConstants><PfsGroup>PFS2048</PfsGroup><DHGroup>Group14</DHGroup><IntegrityCheckMethod>SHA256</IntegrityCheckMethod><EncryptionMethod>AES128</EncryptionMethod></CryptographySuite><DisableClassBasedDefaultRoute>true</DisableClassBasedDefaultRoute></NativeProfile><Route><Address>10.0.0.0</Address><PrefixSize>8</PrefixSize><Metric>1</Metric></Route><RegisterDNS>true</RegisterDNS></VPNProfile>"
            };
            ProfileAction.HandleProfileCreate(machineProfile, TestContext.CancellationTokenSource.Token);

            AccessWMI.NewProfile("User Profile", "<VPNProfile><RememberCredentials>true</RememberCredentials><AlwaysOn>true</AlwaysOn><DnsSuffix>example.local</DnsSuffix><TrustedNetworkDetection>example.local</TrustedNetworkDetection><NativeProfile><Servers>aovpn.test.com;aovpn.test.com</Servers><RoutingPolicyType>SplitTunnel</RoutingPolicyType><NativeProtocolType>Ikev2</NativeProtocolType><Authentication><UserMethod>Eap</UserMethod><MachineMethod>Eap</MachineMethod><Eap><Configuration><EapHostConfig xmlns=\"http://www.microsoft.com/provisioning/EapHostConfig\"><EapMethod><Type xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">25</Type><VendorId xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">0</VendorId><VendorType xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">0</VendorType><AuthorId xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">0</AuthorId></EapMethod><Config xmlns=\"http://www.microsoft.com/provisioning/EapHostConfig\"><Eap xmlns=\"http://www.microsoft.com/provisioning/BaseEapConnectionPropertiesV1\"><Type>25</Type><EapType xmlns=\"http://www.microsoft.com/provisioning/MsPeapConnectionPropertiesV1\"><ServerValidation><DisableUserPromptForServerValidation>true</DisableUserPromptForServerValidation><ServerNames>AONPS-01.test.local</ServerNames><TrustedRootCA>05 49 d9 e2 d6 8c 0e 18 48 9f ad 29 8c 03 62 62 1d 33 42 28 </TrustedRootCA></ServerValidation><FastReconnect>true</FastReconnect><InnerEapOptional>false</InnerEapOptional><Eap xmlns=\"http://www.microsoft.com/provisioning/BaseEapConnectionPropertiesV1\"><Type>13</Type><EapType xmlns=\"http://www.microsoft.com/provisioning/EapTlsConnectionPropertiesV1\"><CredentialsSource><CertificateStore><SimpleCertSelection>true</SimpleCertSelection></CertificateStore></CredentialsSource><ServerValidation><DisableUserPromptForServerValidation>true</DisableUserPromptForServerValidation><ServerNames>AONPS-01.test.local</ServerNames><TrustedRootCA>05 49 d9 e2 d6 8c 0e 18 48 9f ad 29 8c 03 62 62 1d 33 42 28 </TrustedRootCA></ServerValidation><DifferentUsername>false</DifferentUsername><PerformServerValidation xmlns=\"http://www.microsoft.com/provisioning/EapTlsConnectionPropertiesV2\">true</PerformServerValidation><AcceptServerName xmlns=\"http://www.microsoft.com/provisioning/EapTlsConnectionPropertiesV2\">true</AcceptServerName><TLSExtensions xmlns=\"http://www.microsoft.com/provisioning/EapTlsConnectionPropertiesV2\"><FilteringInfo xmlns=\"http://www.microsoft.com/provisioning/EapTlsConnectionPropertiesV3\"><CAHashList Enabled=\"true\"><IssuerHash>5e e1 d7 2e ac 4a d3 23 57 c3 3e ff 1f 8c 7a 25 3c 1e 74 7a </IssuerHash></CAHashList></FilteringInfo></TLSExtensions></EapType></Eap><EnableQuarantineChecks>false</EnableQuarantineChecks><RequireCryptoBinding>false</RequireCryptoBinding><PeapExtensions><PerformServerValidation xmlns=\"http://www.microsoft.com/provisioning/MsPeapConnectionPropertiesV2\">true</PerformServerValidation><AcceptServerName xmlns=\"http://www.microsoft.com/provisioning/MsPeapConnectionPropertiesV2\">true</AcceptServerName></PeapExtensions></EapType></Eap></Config></EapHostConfig></Configuration></Eap></Authentication><CryptographySuite><AuthenticationTransformConstants>SHA256128</AuthenticationTransformConstants><CipherTransformConstants>AES128</CipherTransformConstants><PfsGroup>PFS2048</PfsGroup><DHGroup>Group14</DHGroup><IntegrityCheckMethod>SHA256</IntegrityCheckMethod><EncryptionMethod>AES128</EncryptionMethod></CryptographySuite><DisableClassBasedDefaultRoute>true</DisableClassBasedDefaultRoute></NativeProfile><Route><Address>10.0.0.0</Address><PrefixSize>8</PrefixSize><Metric>1</Metric></Route></VPNProfile>", TestContext.CancellationTokenSource.Token);

            SharedData sharedData = new SharedData(60, false, false, TestContext.CancellationTokenSource.Token);
            Assert.AreEqual(sharedData.PendingUpdates(), 0); //No updates based on nothing to do
            Assert.AreEqual(2, sharedData.GetManagedProfileList().Count); //Both profiles are now classed as monitored
        }

        [TestMethod]
        public void CheckUpdateTimeIsRandom()
        {
            int checkCount = 20;

            SharedData sharedData = new SharedData(60, false, false, TestContext.CancellationTokenSource.Token);
            Assert.AreEqual(sharedData.PendingUpdates(), 0); //No updates based on nothing to do
            Assert.AreEqual(sharedData.GetManagedProfileList().Count, 0); //No profiles being tracked

            List<int> uniqueList = new List<int>();

            for (int i = 0; i< checkCount; i++)
            {
                int newAttempt = sharedData.GetUpdateTime();
                TestContext.WriteLine("Attempt " + i + " was " + newAttempt);
                Assert.IsFalse(uniqueList.Contains(newAttempt));
                uniqueList.Add(newAttempt);
            }
        }

        [TestMethod]
        public void GetAvailableUpdatesWithoutInfo()
        {
            SharedData sharedData = CreateBasicSharedData(true);
            PrivateObject obj = new PrivateObject(sharedData);
            List<ManagedProfile> updateList = (List<ManagedProfile>)obj.Invoke("GetAvailableUpdates");
            Assert.AreEqual(updateList.Count, 0);
        }

        [TestMethod]
        public void GetAvailableUpdatesWith1UpdateNoConnections()
        {
            SharedData sharedData = CreateBasicSharedData(true);
            PrivateObject obj = new PrivateObject(sharedData);
            sharedData.AddProfileUpdate(CreateBasicUserProfileUpdate("Test Profile 1"));

            //Check that the profile is suitable to be processed as it is not being blocked
            List<ManagedProfile> updateList = (List<ManagedProfile>)obj.Invoke("GetAvailableUpdates");
            Assert.AreEqual(1, updateList.Count);
        }

        //When updates are allowed with unmanaged connections already connected, the profile update (to
        //an unrelated profile) should still be allowed
        [TestMethod]
        public void GetAvailableUpdatesWith1UpdateWithConnections()
        {
            SharedData sharedData = CreateBasicSharedData(true);
            PrivateObject obj = new PrivateObject(sharedData);

            //As this test runs without a valid VPN configured we have to fake an active connection
            Type type = typeof(SharedData);
            FieldInfo field = type.GetField("ConnectedVPNList", BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(sharedData, new List<string>() {
                "Profile 1"
            });

            sharedData.AddProfileUpdate(CreateBasicUserProfileUpdate("Test Profile 1"));

            //Check that the profile is suitable to be processed as it is not being blocked
            IList<ManagedProfile> updateList = (List<ManagedProfile>)obj.Invoke("GetAvailableUpdates");
            Assert.AreEqual(1, updateList.Count);
        }

        //When updates are allowed with unmanaged connections already connected, the profile update (to
        //an unrelated profile) should still be allowed
        [TestMethod]
        public void GetAvailableUpdatesWith2UpdateWithConnections()
        {
            SharedData sharedData = CreateBasicSharedData(true);
            PrivateObject obj = new PrivateObject(sharedData);

            //As this test runs without a valid VPN configured we have to fake an active connection
            Type type = typeof(SharedData);
            FieldInfo field = type.GetField("ConnectedVPNList", BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(sharedData, new List<string>() {
                "Profile 1",
                "Profile 2"
            });

            sharedData.AddProfileUpdate(CreateBasicUserProfileUpdate("Test Profile 1"));
            sharedData.AddProfileUpdate(CreateBasicUserProfileUpdate("Test Profile 2"));

            //Check that the profile is suitable to be processed as it is not being blocked
            IList<ManagedProfile> updateList = (List<ManagedProfile>)obj.Invoke("GetAvailableUpdates");
            Assert.AreEqual(2, updateList.Count);
        }

        //Remove actions should always be processed first to minimise routing table disruption
        [TestMethod]
        public void GetAvailableUpdatesWithProfileBeingRemovedAndReAdded()
        {
            SharedData sharedData = CreateBasicSharedData(true);
            PrivateObject obj = new PrivateObject(sharedData);

            //As this test runs without a valid VPN configured we have to fake an active connection
            Type type = typeof(SharedData);
            FieldInfo field = type.GetField("ConnectedVPNList", BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(sharedData, new List<string>() {
                "Profile 1",
                "Profile 2"
            });

            sharedData.AddProfileUpdate(CreateBasicUserProfileUpdate("Test Profile 1"));
            sharedData.RemoveProfile("Test Profile 2");
            IList<ManagedProfile> updateList = (List<ManagedProfile>)obj.Invoke("GetAvailableUpdates");
            Assert.AreEqual(2, updateList.Count);
            Assert.AreEqual(null, updateList[0].ProfileXML); //Check that the first element is the removal even though it was called second
        }

        //When 1 profile is blocked because it is already connected a second profile should
        //be able to still update
        [TestMethod]
        public void GetAvailableUpdatesWith2UpdateWith1ProfileAlreadyConnected()
        {
            SharedData sharedData = CreateBasicSharedData(true);
            PrivateObject obj = new PrivateObject(sharedData);
            //As this test runs without a valid VPN configured we have to fake an active connection
            Type type = typeof(SharedData);
            FieldInfo field = type.GetField("ConnectedVPNList", BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(sharedData, new List<string>() {
                "Test Profile 1"
            });

            sharedData.AddProfileUpdate(CreateBasicUserProfileUpdate("Test Profile 1"));
            sharedData.AddProfileUpdate(CreateBasicUserProfileUpdate("Test Profile 2"));
            IList<ManagedProfile> updateList = (List<ManagedProfile>)obj.Invoke("GetAvailableUpdates");
            Assert.AreEqual(1, updateList.Count);
        }

        //Even when unmanaged connections are not in use the update should not happen if the profile name
        //is already connected
        [TestMethod]
        public void GetAvailableUpdatesWith1UpdateWithProfileAlreadyConnected()
        {
            SharedData sharedData = CreateBasicSharedData(true);
            PrivateObject obj = new PrivateObject(sharedData);
            //As this test runs without a valid VPN configured we have to fake an active connection
            Type type = typeof(SharedData);
            FieldInfo field = type.GetField("ConnectedVPNList", BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(sharedData, new List<string>() {
                "Test Profile 1"
            });

            sharedData.AddProfileUpdate(CreateBasicUserProfileUpdate("Test Profile 1"));
            IList<ManagedProfile> updateList = (List<ManagedProfile>)obj.Invoke("GetAvailableUpdates");
            Assert.AreEqual(0, updateList.Count);
        }

        //When updates are not allowed with unmanaged connections already connected, the profile update
        //should not allow any connection updates
        [TestMethod]
        public void GetAvailableUpdatesWith1UpdateWithConnectionsBlocked()
        {
            SharedData sharedData = CreateBasicSharedData(false);
            PrivateObject obj = new PrivateObject(sharedData);

            //As this test runs without a valid VPN configured we have to fake an active connection
            Type type = typeof(SharedData);
            FieldInfo field = type.GetField("ConnectedVPNList", BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(sharedData, new List<string>() {
                "Profile 1"
            });

            sharedData.AddProfileUpdate(CreateBasicUserProfileUpdate("Test Profile 1"));
            IList<ManagedProfile> updateList = (List<ManagedProfile>)obj.Invoke("GetAvailableUpdates");
            Assert.AreEqual(0, updateList.Count);
        }

        [TestMethod]
        public void InitialBasicSetupWithExistingConnection()
        {
            string profileName = TestContext.TestName;
            string originalProfile = "<VPNProfile><RememberCredentials>true</RememberCredentials><AlwaysOn>true</AlwaysOn><DnsSuffix>test.local</DnsSuffix><TrustedNetworkDetection>test.local</TrustedNetworkDetection><NativeProfile><Servers>aovpn.example.com;aovpn.example.com</Servers><RoutingPolicyType>SplitTunnel</RoutingPolicyType><NativeProtocolType>Ikev2</NativeProtocolType><Authentication><UserMethod>Eap</UserMethod><MachineMethod>Eap</MachineMethod><Eap><Configuration><EapHostConfig xmlns=\"http://www.microsoft.com/provisioning/EapHostConfig\"><EapMethod><Type xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">25</Type><VendorId xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">0</VendorId><VendorType xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">0</VendorType><AuthorId xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">0</AuthorId></EapMethod><Config xmlns=\"http://www.microsoft.com/provisioning/EapHostConfig\"><Eap xmlns=\"http://www.microsoft.com/provisioning/BaseEapConnectionPropertiesV1\"><Type>25</Type><EapType xmlns=\"http://www.microsoft.com/provisioning/MsPeapConnectionPropertiesV1\"><ServerValidation><DisableUserPromptForServerValidation>true</DisableUserPromptForServerValidation><ServerNames>AONPS-01.test.local</ServerNames><TrustedRootCA>05 49 d9 e2 d6 8c 0e 18 48 9f ad 29 8c 03 62 62 1d 33 42 28 </TrustedRootCA></ServerValidation><FastReconnect>true</FastReconnect><InnerEapOptional>false</InnerEapOptional><Eap xmlns=\"http://www.microsoft.com/provisioning/BaseEapConnectionPropertiesV1\"><Type>13</Type><EapType xmlns=\"http://www.microsoft.com/provisioning/EapTlsConnectionPropertiesV1\"><CredentialsSource><CertificateStore><SimpleCertSelection>true</SimpleCertSelection></CertificateStore></CredentialsSource><ServerValidation><DisableUserPromptForServerValidation>true</DisableUserPromptForServerValidation><ServerNames>AONPS-01.test.local</ServerNames><TrustedRootCA>05 49 d9 e2 d6 8c 0e 18 48 9f ad 29 8c 03 62 62 1d 33 42 28 </TrustedRootCA></ServerValidation><DifferentUsername>false</DifferentUsername><PerformServerValidation xmlns=\"http://www.microsoft.com/provisioning/EapTlsConnectionPropertiesV2\">true</PerformServerValidation><AcceptServerName xmlns=\"http://www.microsoft.com/provisioning/EapTlsConnectionPropertiesV2\">true</AcceptServerName><TLSExtensions xmlns=\"http://www.microsoft.com/provisioning/EapTlsConnectionPropertiesV2\"><FilteringInfo xmlns=\"http://www.microsoft.com/provisioning/EapTlsConnectionPropertiesV3\"><CAHashList Enabled=\"true\"><IssuerHash>5e e1 d7 2e ac 4a d3 23 57 c3 3e ff 1f 8c 7a 25 3c 1e 74 7a </IssuerHash></CAHashList></FilteringInfo></TLSExtensions></EapType></Eap><EnableQuarantineChecks>false</EnableQuarantineChecks><RequireCryptoBinding>false</RequireCryptoBinding><PeapExtensions><PerformServerValidation xmlns=\"http://www.microsoft.com/provisioning/MsPeapConnectionPropertiesV2\">true</PerformServerValidation><AcceptServerName xmlns=\"http://www.microsoft.com/provisioning/MsPeapConnectionPropertiesV2\">true</AcceptServerName></PeapExtensions></EapType></Eap></Config></EapHostConfig></Configuration></Eap></Authentication><CryptographySuite><AuthenticationTransformConstants>SHA256128</AuthenticationTransformConstants><CipherTransformConstants>AES128</CipherTransformConstants><PfsGroup>PFS2048</PfsGroup><DHGroup>Group14</DHGroup><IntegrityCheckMethod>SHA256</IntegrityCheckMethod><EncryptionMethod>AES128</EncryptionMethod></CryptographySuite><DisableClassBasedDefaultRoute>true</DisableClassBasedDefaultRoute></NativeProfile><Route><Address>10.0.0.0</Address><PrefixSize>8</PrefixSize><Metric>1</Metric></Route></VPNProfile>";
            ProfileType profileType = ProfileType.User;

            //Create Profile outside of test context
            AccessWMI.NewProfile(profileName, originalProfile, TestContext.CancellationTokenSource.Token);
            AccessRegistry.SaveMachineData(AccessRegistry.GetProfileNameRegistryName(profileType), profileName); //Save to Registry to enable the profile to be tracked on startu

            SharedData sharedData = new SharedData(60, false, false, TestContext.CancellationTokenSource.Token); //don't update if an existing connection is live

            //As this test runs without a valid VPN configured we have to fake an active connection
            Type type = typeof(SharedData);
            FieldInfo field = type.GetField("ConnectedVPNList", BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(sharedData, new List<string>() {
                "Profile 1"
            });

            Assert.AreEqual(0, sharedData.PendingUpdates()); //No updates based on nothing to do
            Assert.AreEqual(1, sharedData.GetManagedProfileList().Count); //Profile is being tracked

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
            Assert.IsFalse(profile.ValidateWarnings());

            sharedData.AddProfileUpdate(profile.GetProfileUpdate());

            Assert.AreEqual(1, sharedData.PendingUpdates()); //Update shouldn't have processed
            Assert.AreEqual(1, sharedData.GetManagedProfileList().Count); //Profile is being tracked

            //Check update was blocked so profile still shows original
            Assert.IsTrue(VPNProfile.CompareToInstalledProfile(profileName, originalProfile, TestContext.CancellationTokenSource.Token));
        }
    }
}
