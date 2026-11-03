using DPCLibrary.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace DPCLibraryTests
{
    [TestClass]
    [TestCategory("Basic")]
    public class INIParserTests
    {
        [TestMethod]
        public void ReadsSectionsValuesAndRepeatedKeys()
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".pbk");
            File.WriteAllText(path, "'comment\r\n[First]\r\nKey=one\r\nKey=two\r\nFlag\r\n[Empty]\r\n[Second]\r\nValue=a=b\r\n");

            try
            {
                INIParser parser = new INIParser(path);

                CollectionAssert.AreEquivalent(new[] { "First", "Second" }, parser.GetSections().ToArray());
                Assert.AreEqual("onetwo", parser.GetSection("First")["Key"]);
                Assert.AreEqual(string.Empty, parser.GetSection("First")["Flag"]);
                Assert.AreEqual("a=b", parser.GetSection("Second")["Value"]);
                Assert.IsNull(parser.GetSection("Missing"));
                Assert.AreEqual(File.ReadAllText(path), parser.OpenFile());
            }
            finally
            {
                File.Delete(path);
            }
        }

        [TestMethod]
        public void MissingFileProducesNoSectionsOrContents()
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".pbk");

            INIParser parser = new INIParser(path);

            Assert.AreEqual(0, parser.GetSections().Count);
            Assert.AreEqual(string.Empty, parser.OpenFile());
            Assert.IsNull(parser.GetSection("Missing"));
        }

        [TestMethod]
        public void FormatsPBKBytesInLittleEndianOrder()
        {
            Assert.AreEqual("563412", INIParser.FormatPBKByteAsString("123456"));
            Assert.AreEqual((uint)1, INIParser.FormatPBKByteAsUInt("01000000"));
            Assert.IsTrue(INIParser.FormatPBKByteAsBool("01000000"));
            Assert.IsFalse(INIParser.FormatPBKByteAsBool("00000000"));
        }

        [TestMethod]
        public void DecodesUTF16LittleEndianHexString()
        {
            Assert.AreEqual("AB", INIParser.DecodeHexString("41004200"));
        }
    }
}
