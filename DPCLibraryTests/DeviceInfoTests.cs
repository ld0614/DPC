using System;
using DPCLibrary.Enums;
using DPCLibrary.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DPCLibraryTests
{
    [TestClass]
    public class DeviceInfoTests
    {
        /// <summary>
        ///  Gets or sets the test context which provides
        ///  information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext { get; set; }

        [TestMethod]
        [TestCategory("Windows10")]
        public void OSMajorVersionWin10()
        {
            Assert.AreEqual(MajorOSVersion.Windows10, DeviceInfo.GetOSVersion().MajorOSVersion);
        }

        [TestMethod]
        [TestCategory("Windows11")]
        public void OSMajorVersionWin11()
        {
            Assert.AreEqual(MajorOSVersion.Windows11, DeviceInfo.GetOSVersion().MajorOSVersion);
        }

        [TestMethod]
        public void OSFeatureRelease()
        {
            string OSVersion = DeviceInfo.GetOSVersion().OSSubVersion;
            Assert.IsNotNull(OSVersion);
            TestContext.WriteLine("Version: " + OSVersion);
        }

        [TestMethod]
        public void VersionisOfTypeVersion()
        {
            Assert.IsInstanceOfType(DeviceInfo.GetOSVersion().Version, typeof(Version));
        }

        //Test should only work on devices later than 2004

        [TestMethod]
        public void CheckVersion2004Gate()
        {
            Assert.IsTrue(DeviceInfo.GetOSVersion().IsGreaterThanWin10_2004);
        }

        [TestMethod]
        public void GetFullBuildVersion()
        {
            Assert.IsInstanceOfType(DeviceInfo.GetOSVersion().Version, typeof(Version));
        }
    }
}
