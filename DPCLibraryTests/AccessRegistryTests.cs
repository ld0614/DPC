using DPCLibrary.Enums;
using DPCLibrary.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DPCLibraryTests
{
    [TestClass]
    public class AccessRegistryTests
    {
        [TestInitialize]
        public void PreTestInitialize()
        {
            DeleteRegLocation(RegistrySettings.PolicyPath);
            DeleteRegLocation(RegistrySettings.ManualPath);
        }

        [TestCleanup]
        public void PostTestCleanup()
        {
            DeleteRegLocation(RegistrySettings.PolicyPath);
            DeleteRegLocation(RegistrySettings.ManualPath);
        }

        private RegistryKey GetHKLM()
        {
            PrivateType privateTypeObject = new PrivateType(typeof(AccessRegistry));
            object hklmObj = privateTypeObject.InvokeStatic("GetHKLM");

            Assert.IsNotNull(hklmObj);
            RegistryKey hklm = (RegistryKey)hklmObj;
            Assert.IsNotNull(hklm);
            return hklm;
        }

        private void DeleteRegLocation(string location)
        {
            RegistryKey hklm = GetHKLM();
            if (hklm.OpenSubKey(location) != null)
            {
                hklm.DeleteSubKeyTree(location, false);
            }
            hklm.Close();
            Assert.IsNull(GetHKLM().OpenSubKey(location));
        }

        private static bool DictionaryEqual(
            Dictionary<string, string> oldDict,
            Dictionary<string, string> newDict)
        {
            // Simple check, are the counts the same?
            if (!oldDict.Count.Equals(newDict.Count)) return false;

            // Verify the keys
            if (!oldDict.Keys.SequenceEqual(newDict.Keys)) return false;

            // Verify the values for each key
            foreach (string key in oldDict.Keys)
                if (!oldDict[key].SequenceEqual(newDict[key]))
                    return false;

            return true;
        }

        [TestMethod]
        public void RegistryLocations()
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(RegistrySettings.PolicyPath));
            Assert.IsFalse(string.IsNullOrWhiteSpace(RegistrySettings.ManualPath));

            //Breaking Change if these values change
            Assert.AreEqual(RegistrySettings.PolicyPath, "SOFTWARE\\Policies\\DPC\\DPCClient");
            Assert.AreEqual(RegistrySettings.ManualPath, "SOFTWARE\\DPC\\DPCClient");
        }

        [TestMethod]
        public void TestHKLM()
        {
            RegistryKey hklm = GetHKLM();

            Assert.AreEqual(RegistryView.Registry64, hklm.View);
            Assert.IsNotNull(hklm.GetValueNames());
        }

        [TestMethod]
        public void LoadRegistryKeyValueFromManual()
        {
            string valueName = "Value";
            string valueData = "ManualPath";

            AccessRegistry.SaveMachineData(valueName, valueData, RegistrySettings.ManualPath, null);

            string retValue = AccessRegistry.ReadMachineString(valueName);
            Assert.IsNotNull(retValue);
            Assert.AreEqual(valueData, retValue);
        }

        [TestMethod]
        public void LoadRegistryKeyValueFromPolicyWithOffset()
        {
            string valueName = "Value";
            string valueData = "PolicyPath";
            string offset = "OffsetKey";

            AccessRegistry.SaveMachineData(valueName, valueData, RegistrySettings.PolicyPath, offset);
            AccessRegistry.SaveMachineData(valueName, "ManualPath", RegistrySettings.ManualPath, offset);

            string retValue = AccessRegistry.ReadMachineString(valueName, offset);
            Assert.IsNotNull(retValue);
            Assert.AreEqual(valueData, retValue);
        }

        [TestMethod]
        public void LoadRegistryKeyValueMissingValue()
        {
            string valueName = "Val1";

            string retValue = AccessRegistry.ReadMachineString(valueName);

            Assert.IsNull(retValue);
        }

        [TestMethod]
        public void LoadRegistryKeyValueFromPolicy()
        {
            string valueName = "Value";
            string valueData = "PolicyPath";

            AccessRegistry.SaveMachineData(valueName, valueData, RegistrySettings.PolicyPath,null);
            AccessRegistry.SaveMachineData(valueName, "ManualPath", RegistrySettings.ManualPath, null);

            string retValue = AccessRegistry.ReadMachineString(valueName);
            Assert.IsNotNull(retValue);
            Assert.AreEqual(valueData, retValue);
        }

        [TestMethod]
        public void SaveStringInternal()
        {
            AccessRegistry.SaveMachineData("Value1", "Data1");
            Assert.AreEqual(AccessRegistry.ReadMachineString("Value1", RegistrySettings.InternalStateOffset), "Data1");
        }

        [TestMethod]
        public void ValidateHashtableRegistry()
        {
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "Value1", "Data1" },
                { "Value2", "Data2" },
                { "Value3", "Data3" },
                { "Value4", "Data4" }
            };

            AccessRegistry.SaveMachineData("Hash", data);

            Dictionary<string, string> returnData = AccessRegistry.ReadMachineHashtable("Hash", RegistrySettings.InternalStateOffset);

            Assert.IsTrue(DictionaryEqual(data, returnData));
        }

        [TestMethod]
        public void ValidateHashtableOverwriteRegistry()
        {
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "Value1", "Data1" },
                { "Value2", "Data2" },
                { "Value3", "Data3" },
                { "Value4", "Data4" }
            };

            AccessRegistry.SaveMachineData("Hash", data);

            Dictionary<string, string> returnData = AccessRegistry.ReadMachineHashtable("Hash", RegistrySettings.InternalStateOffset);

            Assert.IsTrue(DictionaryEqual(data, returnData));

            Dictionary<string, string> data2 = new Dictionary<string, string>
            {
                { "Value1", "Data1" }
            };

            AccessRegistry.SaveMachineData("Hash", data2);

            Dictionary<string, string> returnData2 = AccessRegistry.ReadMachineHashtable("Hash", RegistrySettings.InternalStateOffset);

            Assert.IsTrue(DictionaryEqual(data2, returnData2));
        }

        [TestMethod]
        public void ValidateListRegistry()
        {
            IList<string> data = new List<string>
            {
                { "Data1" },
                { "Data2" },
                { "Data3" },
                { "Data4" }
            };

            AccessRegistry.SaveMachineData("List", data);

            IList<string> returnData = AccessRegistry.ReadMachineList("List", RegistrySettings.InternalStateOffset);

            Assert.AreEqual(data.Count, returnData.Count);

            foreach (string item in data)
            {
                Assert.IsTrue(returnData.Contains(item));
            }
        }

        [TestMethod]
        public void ValidateListOverwriteRegistry()
        {
            IList<string> data = new List<string>
            {
                { "Data1" },
                { "Data2" },
                { "Data3" },
                { "Data4" }
            };

            AccessRegistry.SaveMachineData("List", data);

            IList<string> returnData = AccessRegistry.ReadMachineList("List", RegistrySettings.InternalStateOffset);

            Assert.AreEqual(data.Count, returnData.Count);

            foreach (string item in data)
            {
                Assert.IsTrue(returnData.Contains(item));
            }

            List<string> data2 = new List<string>
            {
                { "Data1" },
            };

            AccessRegistry.SaveMachineData("List", data2);

            IList<string> returnData2 = AccessRegistry.ReadMachineList("List", RegistrySettings.InternalStateOffset);

            Assert.AreEqual(data2.Count, returnData2.Count);

            foreach (string item in data2)
            {
                Assert.IsTrue(returnData2.Contains(item));
            }
        }

        //Check its possible to create a new registry key
        [TestMethod]
        public void ValidateCreateKey()
        {
            string customPath = RegistrySettings.ManualPath + "\\Path1\\Path2\\Path3";
            Assert.IsNull(AccessRegistry.ReadMachineBoolean("Value", null, customPath));
            AccessRegistry.SaveMachineData("Value", true, customPath, null);
            Assert.IsTrue(AccessRegistry.ReadMachineBoolean("Value", null, customPath));
        }

        //Check that when there is no value in the registry the default value will be returned
        [TestMethod]
        public void ValidateDefaultBool()
        {
            Assert.IsNull(AccessRegistry.ReadMachineBoolean("Value"));
            Assert.IsTrue(AccessRegistry.ReadMachineBoolean("Value", true));
            Assert.IsFalse(AccessRegistry.ReadMachineBoolean("Value", false));
        }

        //Check that having the location exist doesn't cause an issue when no value exists
        [TestMethod]
        public void ValidateSubkeyDoesntConflictBool()
        {
            AccessRegistry.SaveMachineData("Value", true);
            AccessRegistry.SaveMachineData("Value2", new List<string>() { "List1", "List2" });

            Assert.IsNull(AccessRegistry.ReadMachineBoolean("Value2", RegistrySettings.InternalStateOffset));
            Assert.IsFalse(AccessRegistry.ReadMachineBoolean("Value2", false, RegistrySettings.InternalStateOffset));
        }

        //Check that having the location exist doesn't cause an issue when no value exists
        [TestMethod]
        public void ValidateLocationExistDefaultBool()
        {
            AccessRegistry.SaveMachineData("Value", true);

            Assert.IsNull(AccessRegistry.ReadMachineBoolean("Value2", RegistrySettings.InternalStateOffset));
            Assert.IsFalse(AccessRegistry.ReadMachineBoolean("Value2", false, RegistrySettings.InternalStateOffset));
        }

        //Check that when a default value is specified that this is overridden by the actual value in the registry if it exists
        [TestMethod]
        public void ValidateValidDefaultBool()
        {
            AccessRegistry.SaveMachineData("Value", true);

            Assert.IsNotNull(AccessRegistry.ReadMachineBoolean("Value", RegistrySettings.InternalStateOffset));
            Assert.IsTrue(AccessRegistry.ReadMachineBoolean("Value", false, RegistrySettings.InternalStateOffset));
        }

        [TestMethod]
        public void ValidateBool()
        {
            Assert.IsNull(AccessRegistry.ReadMachineBoolean("Value"));

            AccessRegistry.SaveMachineData("Value", true);

            bool? result = AccessRegistry.ReadMachineBoolean("Value", RegistrySettings.InternalStateOffset);

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result);

            AccessRegistry.SaveMachineData("Value", false);

            result = AccessRegistry.ReadMachineBoolean("Value", RegistrySettings.InternalStateOffset);

            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result);
        }

        [TestMethod]
        public void ValidateSingleString()
        {
            AccessRegistry.SaveMachineData("Value1", "This is a test string");
            Assert.AreEqual("This is a test string", AccessRegistry.ReadMachineString("Value1", RegistrySettings.InternalStateOffset));
        }

        [TestMethod]
        public void ValidateMultiString()
        {
            AccessRegistry.SaveMachineData("Value1", new string[] { "This is a test string", "This is another String" });
            Assert.AreEqual("This is a test string\nThis is another String", AccessRegistry.ReadMachineString("Value1", RegistrySettings.InternalStateOffset));
        }

        [TestMethod]
        public void ValidateInt()
        {
            AccessRegistry.SaveMachineData("Value1", 65);
            Assert.AreEqual((uint)65, AccessRegistry.ReadMachineUInt32("Value1", RegistrySettings.InternalStateOffset));
        }

        [TestMethod]
        public void ValidateMaxInt()
        {
            AccessRegistry.SaveMachineData("Value1", 4294967295);
            Assert.AreEqual(4294967295, AccessRegistry.ReadMachineUInt32("Value1", RegistrySettings.InternalStateOffset));
        }

        [TestMethod]
        public void ValidateGuid()
        {
            Guid guid = Guid.NewGuid();

            AccessRegistry.SaveMachineData("Value1", guid);
            Assert.AreEqual(guid, AccessRegistry.ReadMachineGuid("Value1", RegistrySettings.InternalStateOffset));
        }

        [TestMethod]
        public void ValidateInvalidGuid()
        {
            AccessRegistry.SaveMachineData("Value1", "This won't work");
            Assert.IsNull(AccessRegistry.ReadMachineGuid("Value1", RegistrySettings.InternalStateOffset));
            Assert.IsNotNull(AccessRegistry.ReadMachineString("Value1", RegistrySettings.InternalStateOffset));
        }

        [TestMethod]
        public void ValidateLongThrowsException()
        {
            AccessRegistry.SaveMachineData("Value1", 4294967296);
            Assert.ThrowsException<OverflowException>(() => AccessRegistry.ReadMachineUInt32("Value1", RegistrySettings.InternalStateOffset));
        }

        [TestMethod]
        public void ValidateLongBool()
        {
            AccessRegistry.SaveMachineData("Value1", (long)1);
            Assert.IsNotNull(AccessRegistry.ReadMachineBoolean("Value1", RegistrySettings.InternalStateOffset));
            Assert.IsTrue((bool)AccessRegistry.ReadMachineBoolean("Value1", RegistrySettings.InternalStateOffset));
        }
    }
}