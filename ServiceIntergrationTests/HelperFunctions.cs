using DPCLibrary.Enums;
using DPCLibrary.Models;
using DPCLibrary.Utils;
using DPCService.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Session;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;
using System.Runtime.InteropServices;

namespace ServiceIntegrationTests
{
    public static class HelperFunctions
    {
        //Needs to be set to the initial device profile name installed on the machine if running on Windows11+
        public const string DefaultConnectionURL = "aovpndpcunittest.systemcenter.ninja"; //Point to somewhere we control

        private static readonly string ETWSessionName = "DPCServiceIntergrationTests";
        internal static TraceEventSession ETWSession = new TraceEventSession(ETWSessionName);
        private static List<TraceEvent> ETWEvents = new List<TraceEvent>();

        private static readonly object ModifyETWEventListLock = new object();

        public static void ClearRegistry()
        {
            AccessRegistry.ClearRegistryKey(RegistrySettings.ManualPath);
            AccessRegistry.ClearRegistryKey(RegistrySettings.PolicyPath);
        }

        public static bool ClearProfiles(SharedData sharedData)
        {
            int timeoutInSeconds = 120; //2 mins
            bool skipProfileDeleteErrors = false;
            DateTime startTime = DateTime.UtcNow;
            IList<ProfileInfo> profileList = ManageRasphonePBK.ListAllProfiles(false);
            while (profileList.Count > 0 && (DateTime.UtcNow - startTime).TotalSeconds < timeoutInSeconds)
            {
                foreach (ProfileInfo profile in profileList)
                {
                    sharedData.RemoveProfile(profile.ProfileName);
                }

                sharedData.HandleProfileUpdates();
                //Assert that all updates have happened
                Assert.AreEqual(0, sharedData.PendingUpdates());

                profileList = ManageRasphonePBK.ListAllProfiles(false);
                if (profileList.Count > 0)
                {
                    skipProfileDeleteErrors = true;
                    Thread.Sleep(5000); //Try Again in 5 Seconds
                }
            }

            //Check all profiles have gone
            IList<ProfileInfo> cleanProfileList = ManageRasphonePBK.ListAllProfiles(false);
            Assert.AreEqual(0, cleanProfileList.Count);
            return skipProfileDeleteErrors;
        }

        public static void StartETWMonitor(TestContext context)
        {
            // Today you have to be Admin to turn on ETW events (anyone can write ETW events).
            if (!(TraceEventSession.IsElevated() ?? false))
            {
                throw new Exception("To turn on ETW events you need to be Administrator, please run from an Admin process.");
            }

            try
            {
                //Configure Event Management
                ETWSession.Source.Dynamic.All += delegate (TraceEvent data)
                {
                    lock (ModifyETWEventListLock)
                    {
                        ETWEvents.Add(data.Clone());
                    }
                };

                ETWSession.Source.UnhandledEvents += delegate (TraceEvent data)
                {
                    // The EventSource manifest events show up as unhanded, filter them out.
                    if ((int)data.ID != 0xFFFE)
                    {
                        lock (ModifyETWEventListLock)
                        {
                            ETWEvents.Add(data.Clone());
                        }
                    }
                };

                bool restarted = ETWSession.EnableProvider("DPC-AOVPN-DPCService");
                if (restarted)      // Generally you don't bother with this warning, but for the demo we do.
                {
                    context.WriteLine("The session {0} was already active, it has been restarted.", ETWSessionName);
                }
                new TaskFactory().StartNew(() => ETWSession.Source.Process());
            }
            catch (COMException)
            {
                context.WriteLine("WARNING: ETW Events not monitored!");
            }
        }

        public static void AssertProfileMatches(string profileName, string expectedProfile, TestContext context)
        {
            Dictionary<string, string> results = VPNProfile.CompareToInstalledProfileWithResults(profileName, expectedProfile, context.CancellationTokenSource.Token);
            foreach (KeyValuePair<string, string> result in results)
            {
                context.WriteLine("Issue with " + result.Key + ": " + result.Value);
            }

            //Check update was fully accepted by WMI
            Assert.IsTrue(VPNProfile.CompareToInstalledProfile(profileName, expectedProfile, context.CancellationTokenSource.Token));
        }

        public static List<TraceEvent> GetAllETWEvents()
        {
            lock (ModifyETWEventListLock)
            {
                ETWSession.Flush(); //Events can be queued
                return new List<TraceEvent>(ETWEvents);
            }
        }

        public static List<TraceEvent> GetETWErrorEvents()
        {
            if (ETWSession.IsActive)
            {
                ETWSession.Flush(); //Events can be queuedETWSession.Flush(); //Events can be queued
                lock (ModifyETWEventListLock)
                {
                    return ETWEvents.Where(e => e.Level == TraceEventLevel.Error || e.Level == TraceEventLevel.Critical).ToList();
                }
            }
            else
            {
                throw new Exception("ETW Tracking Not Active!");
            }
        }

        public static List<TraceEvent> GetETWWarningEvents()
        {
            if (ETWSession.IsActive)
            {
                ETWSession.Flush(); //Events can be queued
                lock (ModifyETWEventListLock)
                {
                    return ETWEvents.Where(e => e.Level == TraceEventLevel.Warning).ToList();
                }
            }
            else
            {
                throw new Exception("ETW Tracking Not Active!");
            }
        }

        public static void ClearETWEvents()
        {
            if (ETWSession.IsActive)
            {
                ETWSession.Flush(); //Events can be queued
                lock (ModifyETWEventListLock)
                {
                    ETWEvents.Clear();
                }
            }
        }

        public static void StopETWMonitor()
        {
            if (ETWSession.IsActive)
            {
                //Stop the ETW Session Monitoring
                ETWSession.Dispose();
                lock (ModifyETWEventListLock)
                {
                    ETWEvents.Clear();
                }
            }
        }

        public static void ClearSpecificEventId(int eventID)
        {
            if (ETWSession.IsActive)
            {
                ETWSession.Flush(); //Events can be queued
                Thread.Sleep(4000); //Events can take up to 4 seconds to come through
                lock (ModifyETWEventListLock)
                {
                    ETWEvents = ETWEvents.Where(e => e.ID != (TraceEventID)eventID).ToList();
                }
            }
        }

        public static bool CheckProfileExists(string profileName)
        {
            return ManageRasphonePBK.ListProfiles(profileName).Count == 1;
        }

        public static VPNProfileCreator BasicUserProfile(SharedData sharedData, string testName, CancellationToken cancelToken, TestContext testContext)
        {
            string profileName = testName;
            ProfileType profileType = ProfileType.User;

            Assert.IsFalse(CheckProfileExists(profileName));

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
            profile.Generate();
            Assert.IsFalse(profile.ValidateFailed());
            Assert.IsFalse(profile.ValidateWarnings());

            sharedData.AddProfileUpdate(profile.GetProfileUpdate());

            sharedData.HandleProfileUpdates();
            Assert.IsTrue(CheckProfileExists(profileName));

            //Assert that all updates have happened
            Assert.AreEqual(0, sharedData.PendingUpdates());

            //Check update was fully accepted by WMI
            foreach (KeyValuePair<string, string> issue in VPNProfile.CompareToInstalledProfileWithResults(profileName, profile.GetProfile(), cancelToken))
            {
                testContext.WriteLine("Issue with " + issue.Key + ": " + issue.Value);
            }
            Assert.IsTrue(VPNProfile.CompareToInstalledProfile(profileName, profile.GetProfile(), cancelToken));
            return profile;
        }

        public static string BasicUserProfileCustomCrypto(SharedData sharedData, string testName, CancellationToken cancelToken)
        {
            string profileName = testName;
            ProfileType profileType = ProfileType.User;

            Assert.IsFalse(CheckProfileExists(profileName));

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
            Assert.IsTrue(CheckProfileExists(profileName));

            //Assert that all updates have happened
            Assert.AreEqual(0, sharedData.PendingUpdates());

            //Check update was fully accepted by WMI
            Assert.IsTrue(VPNProfile.CompareToInstalledProfile(profileName, profile.GetProfile(), cancelToken));
            return profileName;
        }

        public static VPNProfileCreator BasicForceTunnelUserProfile(SharedData sharedData, string testName, CancellationToken cancelToken)
        {
            string profileName = testName;
            ProfileType profileType = ProfileType.User;

            Assert.IsFalse(CheckProfileExists(profileName));

            VPNProfileCreator profile = new VPNProfileCreator(profileType, false);
            profile.LoadUserProfile(profileName,
                    TunnelType.ForceTunnel,
                    HelperFunctions.DefaultConnectionURL,
                    new List<string>() { "47beabc922eae80e78783462a79f45c254fde68b" },
                    new List<string>() { "27ac9369faf25207bb2627cefaccbe4ef9c319b8" },
                    new List<string>() { "NPS01.Test.local" }
                );
            profile.Generate();
            Assert.IsFalse(profile.ValidateFailed());
            Assert.IsFalse(profile.ValidateWarnings());

            sharedData.AddProfileUpdate(profile.GetProfileUpdate());

            sharedData.HandleProfileUpdates();
            Assert.IsTrue(CheckProfileExists(profileName));

            //Assert that all updates have happened
            Assert.AreEqual(0, sharedData.PendingUpdates());

            //Check update was fully accepted by WMI
            Assert.IsTrue(VPNProfile.CompareToInstalledProfile(profileName, profile.GetProfile(), cancelToken));
            return profile;
        }

        public static void CheckNoErrors(TestContext testContext)
        {
            ETWSession.Flush();
            Thread.Sleep(4000); //Events can take up to 4 seconds to come through
            List<TraceEvent> ErrorEvents = GetETWErrorEvents();
            foreach (TraceEvent errorEvent in ErrorEvents)
            {
                testContext.WriteLine(errorEvent.Level + ": [" + errorEvent.Channel + "] " + errorEvent.ID + ":");
                testContext.WriteLine(errorEvent.FormattedMessage);
            }
            Assert.AreEqual(0, ErrorEvents.Count);
            List<TraceEvent> WarnEvents = GetETWWarningEvents();

            foreach (TraceEvent warnEvent in WarnEvents)
            {
                testContext.WriteLine(warnEvent.Level + ": [" + warnEvent.Channel + "] " + warnEvent.ID + ":");
                testContext.WriteLine(warnEvent.FormattedMessage);
            }
            Assert.AreEqual(0, WarnEvents.Count);
            testContext.WriteLine("Total Generated Events: " + GetAllETWEvents().Count);
        }
    }
}
