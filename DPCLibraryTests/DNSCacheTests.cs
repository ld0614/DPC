using DPCLibrary.Enums;
using DPCLibrary.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DPCLibraryTests
{
    [TestClass]
    [TestCategory("Administrator")]
    [DoNotParallelize]
    public class DNSCacheTests
    {
        private readonly Dictionary<string, object> originalValues = new Dictionary<string, object>();
        private readonly Dictionary<string, RegistryValueKind> originalKinds = new Dictionary<string, RegistryValueKind>();
        private string hostname;
        private bool originalBreakNetwork;

        [TestInitialize]
        public void Initialize()
        {
            hostname = "dpc-cache-test-" + Guid.NewGuid().ToString("N") + ".invalid";
            originalBreakNetwork = (bool)typeof(HttpService).GetField("breakNetwork", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            PreserveValue(hostname);
            PreserveValue("localhost");
        }

        private void PreserveValue(string name)
        {
            using (RegistryKey hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            using (RegistryKey key = hklm.OpenSubKey(RegistrySettings.ManualPath + "\\" + RegistrySettings.DNSCacheOffset))
            {
                object value = key?.GetValue(name);
                originalValues.Add(name, value);
                if (value != null)
                {
                    originalKinds.Add(name, key.GetValueKind(name));
                }
            }
        }

        [TestCleanup]
        public void Cleanup()
        {
            SetNetworkFailure(originalBreakNetwork);
            using (RegistryKey hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            using (RegistryKey key = hklm.OpenSubKey(RegistrySettings.ManualPath + "\\" + RegistrySettings.DNSCacheOffset, true))
            {
                if (key == null) return;
                foreach (KeyValuePair<string, object> original in originalValues)
                {
                    if (original.Value == null)
                    {
                        key.DeleteValue(original.Key, false);
                    }
                    else
                    {
                        key.SetValue(original.Key, original.Value, originalKinds[original.Key]);
                    }
                }
            }
        }

        private static void SetNetworkFailure(bool fail)
        {
            typeof(HttpService).GetField("breakNetwork", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, fail);
        }

        private static Dictionary<string, string> ResolveRoutes(Dictionary<string, string> names, out string warnings)
        {
            Dictionary<string, string> routes = new Dictionary<string, string>();
            object[] arguments = { routes, names };
            warnings = (string)typeof(VPNProfileCreator).GetMethod("GetDNSRoutes", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, arguments);
            return routes;
        }

        [TestMethod]
        public void LiveLookupReplacesOnlyItsHostnameWithMultiString()
        {
            SetNetworkFailure(false);
            AccessRegistry.SaveMachineData("localhost", new[] { "192.0.2.99" }, RegistrySettings.DNSCacheOffset);
            AccessRegistry.SaveMachineData(hostname, new[] { "192.0.2.10" }, RegistrySettings.DNSCacheOffset);

            Dictionary<string, string> routes = ResolveRoutes(new Dictionary<string, string> { { "LOCALHOST", "Live comment" } }, out string warnings);

            Assert.AreEqual(string.Empty, warnings);
            Assert.IsTrue(routes.Count > 0);
            Assert.IsFalse(routes.ContainsKey("192.0.2.99"));
            CollectionAssert.AreEquivalent(routes.Keys.ToArray(), AccessRegistry.ReadMachineStringArray("localhost", RegistrySettings.DNSCacheOffset, RegistrySettings.ManualPath).ToArray());
            CollectionAssert.AreEqual(new[] { "192.0.2.10" }, AccessRegistry.ReadMachineStringArray(hostname, RegistrySettings.DNSCacheOffset, RegistrySettings.ManualPath).ToArray());
            using (RegistryKey hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            using (RegistryKey key = hklm.OpenSubKey(RegistrySettings.ManualPath + "\\" + RegistrySettings.DNSCacheOffset))
            {
                Assert.AreEqual(RegistryValueKind.MultiString, key.GetValueKind("localhost"));
            }
        }

        [TestMethod]
        public void FailedLookupUsesBothAddressFamiliesAndCurrentComment()
        {
            string[] addresses = { "192.0.2.10", "2001:db8::10" };
            AccessRegistry.SaveMachineData(hostname, addresses, RegistrySettings.DNSCacheOffset);
            SetNetworkFailure(true);

            Dictionary<string, string> routes = ResolveRoutes(new Dictionary<string, string> { { hostname.ToUpperInvariant(), "Current comment" } }, out string warnings);

            CollectionAssert.AreEquivalent(addresses, routes.Keys.ToArray());
            Assert.IsTrue(routes.Values.All(comment => comment == hostname.ToUpperInvariant() + " - Current comment"));
            StringAssert.Contains(warnings, "Using cached DNS addresses");
            CollectionAssert.AreEqual(addresses, AccessRegistry.ReadMachineStringArray(hostname, RegistrySettings.DNSCacheOffset, RegistrySettings.ManualPath).ToArray());
        }

        [TestMethod]
        public void MixedLookupsRetainLiveAndCachedRoutes()
        {
            AccessRegistry.SaveMachineData(hostname, new[] { "192.0.2.10", "2001:db8::10" }, RegistrySettings.DNSCacheOffset);
            SetNetworkFailure(false);

            Dictionary<string, string> routes = ResolveRoutes(new Dictionary<string, string> { { "localhost", "Live" }, { hostname, "Cached" } }, out string warnings);

            Assert.IsTrue(routes.ContainsKey("192.0.2.10"));
            Assert.IsTrue(routes.ContainsKey("2001:db8::10"));
            Assert.IsTrue(routes.Values.Any(comment => comment == "localhost - Live"));
            StringAssert.Contains(warnings, "Using cached DNS addresses for " + hostname);
        }

        [TestMethod]
        public void MissingCacheDoesNotPreventOtherHostnameRoutes()
        {
            AccessRegistry.SaveMachineData("localhost", new[] { "192.0.2.10" }, RegistrySettings.DNSCacheOffset);
            SetNetworkFailure(true);

            Dictionary<string, string> routes = ResolveRoutes(new Dictionary<string, string> { { hostname, "" }, { "localhost", "" } }, out string warnings);

            Assert.AreEqual(1, routes.Count);
            Assert.AreEqual("localhost", routes["192.0.2.10"]);
            StringAssert.Contains(warnings, "Unable to retrieve DNS routes for " + hostname);
            Assert.IsNull(AccessRegistry.ReadMachineStringArray(hostname, RegistrySettings.DNSCacheOffset, RegistrySettings.ManualPath));
        }

        [TestMethod]
        public void InvalidCachedAddressesAreIgnoredAndDuplicatesAreMerged()
        {
            AccessRegistry.SaveMachineData(hostname, new[] { "invalid", "192.0.2.10", "192.0.2.10", "0.0.0.0", "2001:db8::10" }, RegistrySettings.DNSCacheOffset);
            AccessRegistry.SaveMachineData("localhost", new[] { "192.0.2.10" }, RegistrySettings.DNSCacheOffset);
            SetNetworkFailure(true);

            Dictionary<string, string> routes = ResolveRoutes(new Dictionary<string, string> { { hostname, "First" }, { "localhost", "Second" } }, out string warnings);

            Assert.AreEqual(2, routes.Count);
            Assert.AreEqual(hostname + " - First + localhost - Second", routes["192.0.2.10"]);
            StringAssert.Contains(warnings, "Ignoring invalid DNS addresses for " + hostname);
        }

        [TestMethod]
        public void UnusableCacheIsNotReportedAsSuccessfulFallback()
        {
            AccessRegistry.SaveMachineData(hostname, new[] { "invalid" }, RegistrySettings.DNSCacheOffset);
            SetNetworkFailure(true);

            Dictionary<string, string> routes = ResolveRoutes(new Dictionary<string, string> { { hostname, "" } }, out string warnings);

            Assert.AreEqual(0, routes.Count);
            StringAssert.Contains(warnings, "No usable DNS addresses available");
            Assert.IsFalse(warnings.Contains("Using cached DNS addresses"));
        }

        [TestMethod]
        public void RepeatedFallbackUsesCurrentCommentsWithoutChangingCache()
        {
            AccessRegistry.SaveMachineData(hostname, new[] { "192.0.2.10" }, RegistrySettings.DNSCacheOffset);
            SetNetworkFailure(true);

            Dictionary<string, string> first = ResolveRoutes(new Dictionary<string, string> { { hostname, "Include" } }, out string firstWarnings);
            Dictionary<string, string> second = ResolveRoutes(new Dictionary<string, string> { { hostname, "Exclude" } }, out string secondWarnings);

            Assert.AreEqual(hostname + " - Include", first["192.0.2.10"]);
            Assert.AreEqual(hostname + " - Exclude", second["192.0.2.10"]);
            StringAssert.Contains(firstWarnings, "Using cached DNS addresses");
            StringAssert.Contains(secondWarnings, "Using cached DNS addresses");
            CollectionAssert.AreEqual(new[] { "192.0.2.10" }, AccessRegistry.ReadMachineStringArray(hostname, RegistrySettings.DNSCacheOffset, RegistrySettings.ManualPath).ToArray());
        }
    }

    [TestClass]
    public class DNSAddressValidationTests
    {
        private static string[] ValidateAddresses(IList<string> addresses, StringBuilder warnings)
        {
            return (string[])typeof(VPNProfileCreator).GetMethod("ValidateDNSAddresses", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { addresses, "example.invalid", warnings });
        }

        [TestMethod]
        public void ValidAddressesAreDeduplicatedWithoutWarnings()
        {
            StringBuilder warnings = new StringBuilder();

            string[] addresses = ValidateAddresses(new[] { "192.0.2.10", "2001:db8::10", "192.0.2.10" }, warnings);

            CollectionAssert.AreEqual(new[] { "192.0.2.10", "2001:db8::10" }, addresses);
            Assert.AreEqual(0, warnings.Length);
        }

        [TestMethod]
        public void InvalidAddressesAreRejectedWithWarning()
        {
            StringBuilder warnings = new StringBuilder();

            string[] addresses = ValidateAddresses(new[] { "invalid", null, "", "192.0.2.0/24", "0.0.0.0", "::", "192.0.2.10", "2001:db8::10" }, warnings);

            CollectionAssert.AreEqual(new[] { "192.0.2.10", "2001:db8::10" }, addresses);
            StringAssert.Contains(warnings.ToString(), "Ignoring invalid DNS addresses for example.invalid");
        }

        [TestMethod]
        public void EmptyAndUnusableAnswersAreFailures()
        {
            foreach (IList<string> addresses in new IList<string>[] { null, new string[0], new[] { "invalid" } })
            {
                TargetInvocationException error = Assert.Throws<TargetInvocationException>(() => ValidateAddresses(addresses, new StringBuilder()));
                Assert.IsInstanceOfType<InvalidOperationException>(error.InnerException);
                StringAssert.Contains(error.InnerException.Message, "No usable DNS addresses available for example.invalid");
            }
        }
    }
}
