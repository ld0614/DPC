using DPCLibrary.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace DPCLibraryTests
{
    /// <summary>
    /// Summary description for MiniDumpTests
    /// </summary>
    [TestClass]
    [TestCategory("Basic")]
    public class MiniDumpTests
    {
        /// <summary>
        ///  Gets or sets the test context which provides
        ///  information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext { get; set; }

        [TestMethod]
        [DataRow(1)]
        [DataRow(10)]
        public void MiniDumps(int dumpCount)
        {
            MiniDumpNaming naming = new MiniDumpNaming();
            MiniDump.ResetNaming();
            string filename;
            for (int i = 0; i < dumpCount; i++)
            {
                filename = naming.GetMiniDumpName();
                if (File.Exists(filename))
                {
                    File.Delete(filename);
                }
                Assert.IsFalse(File.Exists(filename)); //Check file is deleted to ensure the new one is valid
                Assert.IsTrue(MiniDump.Write());
                Assert.IsTrue(File.Exists(filename));
            }
        }

        [TestMethod]
        [DataRow(1)]
        [DataRow(10)]
        public void MiniDumpsWithOverwrite(int dumpCount)
        {
            MiniDumpNaming naming = new MiniDumpNaming();
            MiniDump.ResetNaming();
            string filename;
            DateTime oldWriteTime = new DateTime();
            for (int i = 0; i < dumpCount; i++)
            {
                filename = naming.GetMiniDumpName();
                if (i > naming.MaxMiniDumpCount)
                {
                    Assert.IsTrue(File.Exists(filename));
                    oldWriteTime = new FileInfo(filename).LastWriteTimeUtc;
                }
                Assert.IsTrue(MiniDump.Write());
                Assert.IsTrue(File.Exists(filename));

                if (i > naming.MaxMiniDumpCount)
                {
                    FileInfo newFileInfo = new FileInfo(filename);
                    Assert.IsTrue(oldWriteTime < newFileInfo.LastWriteTimeUtc);
                }
            }
        }
    }
}
