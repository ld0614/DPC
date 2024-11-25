using DPCLibrary.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace DPCLibraryTests
{
    /// <summary>
    /// Summary description for MiniDumpTests
    /// </summary>
    [TestClass]
    [TestCategory("Basic")]
    public class MiniDumpNamingTests
    {
        /// <summary>
        ///  Gets or sets the test context which provides
        ///  information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext { get; set; }

        [TestMethod]
        [DataRow(1)]
        [DataRow(10)]
        [DataRow(100)]
        public void MiniDumpNames(int dumpCount)
        {
            MiniDumpNaming naming = new MiniDumpNaming();
            string filename;
            string firstFileName = "";
            for (int i = 0; i < dumpCount; i++)
            {
                filename = naming.GetMiniDumpName();
                if (i == 0)
                {
                    firstFileName = filename;
                }
                //Check for correct reset
                if (i == naming.MaxMiniDumpCount)
                {
                    Assert.AreEqual(filename, firstFileName);
                }
                TestContext.WriteLine(filename);
                Assert.IsTrue(Path.IsPathRooted(filename));
                Assert.IsTrue(Path.HasExtension(filename));
            }
        }
    }
}
