using DPCLibrary.Enums;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

//Registry Key = Folder
//Registry Value = Named Element
//Registry Data = The Data stored with the Named Element

namespace DPCLibrary.Utils
{
    public static class AccessRegistry
    {
        private static RegistryKey GetHKLM()
        {
            return RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        }

        //Return the policy key if the Registry value is found, return the ManualPath if the value is found there or null if not found in either location
        private static RegistryKey LoadRegistryKeyValue(string regValue, string regKeyOffset, string regKey)
        {
            if (regKey == null)
            {
                RegistryKey result = OpenRegistryKey(RegistrySettings.PolicyPath, regValue, regKeyOffset);
                if (result != null) return result;
                return OpenRegistryKey(RegistrySettings.ManualPath, regValue, regKeyOffset);
            }
            else
            {
                return OpenRegistryKey(regKey, regValue, regKeyOffset);
            }
        }

        private static RegistryKey OpenRegistryKey(string regPath, string regValue, string regKeyOffset, bool forWrite = false)
        {
            RegistryKey HKLM = GetHKLM();

            RegistryKey key = HKLM.OpenSubKey(regPath, forWrite);
            if (key == null) return null;

            if (!string.IsNullOrWhiteSpace(regKeyOffset))
            {
                regKeyOffset = regKeyOffset.Replace('/', '\\');
                string[] offSetList = regKeyOffset.Split('\\');
                foreach (string offSet in offSetList)
                {
                    string[] currentSubKeyList = key.GetSubKeyNames();

                    if (currentSubKeyList != null && currentSubKeyList.Contains(offSet))
                    {
                        key = key.OpenSubKey(offSet, forWrite);
                    }
                    else
                    {
                        key.Close();
                        return null;
                    }
                }
                return ReturnRegistryKey(key, regValue, forWrite);
            }
            else
            {
                return ReturnRegistryKey(key, regValue, forWrite);
            }
        }

        private static RegistryKey ReturnRegistryKey(RegistryKey regKey, string regValue, bool forWrite = false)
        {
            //If the value requested is null, return the root key
            if (regValue == null)
            {
                return regKey;
            }

            //If the value requested is a value within the key return this
            string[] valueList = regKey.GetValueNames();

            if (valueList != null && valueList.Contains(regValue))
            {
                return regKey;
            }

            //If the value is not directly is the key but is a subkey, return this subkey
            string[] keyList = regKey.GetSubKeyNames();

            if (keyList != null && keyList.Contains(regValue))
            {
                return regKey.OpenSubKey(regValue, forWrite);
            }

            //Open Key No longer needed
            regKey.Close();
            //Unable to find value at all, return null to signify failure
            return null;
        }

        public static string ReadMachineString(string regValue)
        {
            return ReadMachineString(regValue, null, null, true);
        }

        public static string ReadMachineString(string regValue, string regKeyOffset)
        {
            return ReadMachineString(regValue, regKeyOffset, null, true);
        }

        public static string ReadMachineString(string regValue, string regKeyOffset, string key)
        {
            return ReadMachineString(regValue, regKeyOffset, key, true);
        }

        public static string ReadMachineString(string regValue, string regKeyOffset, string key, bool throwOnInvalidType)
        {
            RegistryKey regKey = LoadRegistryKeyValue(regValue, regKeyOffset, key);

            //LoadRegistryKeyValue returns either the value or a subkey (To handle lists).  In this case we only care about the value and want to disregard subkeys
            if (regKey == null || regKey.GetValue(regValue) == null) return null;
            RegistryValueKind valueType = regKey.GetValueKind(regValue);
            if (valueType == RegistryValueKind.MultiString)
            {
                string[] result = (string[])regKey.GetValue(regValue);
                regKey.Close();
                return string.Join("\n",result).Replace("\0","");
            }
            else if (valueType == RegistryValueKind.String)
            {
                string result = (string)regKey.GetValue(regValue);
                regKey.Close();
                return result.Replace("\0", "");
            }
            else if (valueType == RegistryValueKind.ExpandString)
            {
                //Strings which contain system variables should be expanded/converted to full names before returning
                string result = (string)regKey.GetValue(regValue);
                regKey.Close();
                if (!string.IsNullOrWhiteSpace(result))
                {
                    result = Environment.ExpandEnvironmentVariables(result);
                }
                return result.Replace("\0", "");
            }
            else
            {
                regKey.Close();
                if (throwOnInvalidType)
                {
                    throw new InvalidOperationException("Requested Value: " + regValue + " is not a string");
                }
                else
                {
                    return null; //Don't throw an error, just don't return the value
                }
            }
        }

        public static byte[] ReadMachineBinary(string regValue, string regKeyOffset, string key, bool throwOnInvalidType)
        {
            RegistryKey regKey = LoadRegistryKeyValue(regValue, regKeyOffset, key);

            //LoadRegistryKeyValue returns either the value or a subkey (To handle lists).  In this case we only care about the value and want to disregard subkeys
            if (regKey == null || regKey.GetValue(regValue) == null) return null;
            RegistryValueKind valueType = regKey.GetValueKind(regValue);
            if (valueType == RegistryValueKind.Binary)
            {
                byte[] result = (byte[])regKey.GetValue(regValue);
                regKey.Close();
                return result;
            }
            else
            {
                regKey.Close();
                if (throwOnInvalidType)
                {
                    throw new InvalidOperationException("Requested Value: " + regValue + " is not binary");
                }
                else
                {
                    return null; //Don't throw an error, just don't return the value
                }
            }
        }

        public static IList<string> ReadMachineStringArray(string value, string offset, string key)
        {
            RegistryKey regKey = LoadRegistryKeyValue(value, offset, key);

            //LoadRegistryKeyValue returns either the value or a subkey (To handle lists).  In this case we only care about the value and want to disregard subkeys
            if (regKey == null || regKey.GetValue(value) == null) return null;
            RegistryValueKind valueType = regKey.GetValueKind(value);
            if (valueType == RegistryValueKind.MultiString)
            {
                string[] result = (string[])regKey.GetValue(value);
                regKey.Close();
                result = result.Select(x => x.Replace("\0", "")).ToArray(); //Remove Null chars from strings in the array as these can cause issues and should never exist
                return result.ToList();
            }
            else if (valueType == RegistryValueKind.String)
            {
                string result = (string)regKey.GetValue(value);
                regKey.Close();
                IList<string> returnList = new List<string>
                {
                    result.Replace("\0","")
                };
                return returnList;
            }
            else
            {
                regKey.Close();
                throw new InvalidOperationException("Requested Value: " + value + " is not a string or a string array");
            }
        }

        public static bool? ReadMachineBoolean(string regValue)
        {
            return ReadMachineBoolean(regValue, null, null);
        }

        public static bool? ReadMachineBoolean(string regValue, string regOffset)
        {
            return ReadMachineBoolean(regValue, regOffset, null);
        }

        public static bool ReadMachineBoolean(string regValue, string regKey, bool defaultValue)
        {
            return ReadMachineBoolean(regValue, null, regKey, defaultValue);
        }

        public static bool ReadMachineBoolean(string regValue, string regOffset, string regKey, bool defaultValue)
        {
            bool? result = ReadMachineBoolean(regValue, regOffset, regKey);
            if (result == null)
            {
                return defaultValue;
            }
            else
            {
                return (bool)result;
            }
        }

        //Read number value and if it is 1 assume this means true, otherwise assume it means false
        public static bool? ReadMachineBoolean(string value, string offset, string key)
        {
            bool? result = null;
            RegistryKey regKey = LoadRegistryKeyValue(value, offset, key);
            //LoadRegistryKeyValue returns either the value or a subkey (To handle lists).  In this case we only care about the value and want to disregard subkeys
            if (regKey != null && regKey.GetValue(value) != null)
            {
                RegistryValueKind valueType = regKey.GetValueKind(value);
                if (valueType == RegistryValueKind.DWord)
                {
                    int boolVal = (int)regKey.GetValue(value);
                    result = (boolVal == 1);
                }
                else if (valueType == RegistryValueKind.QWord)
                {
                    long boolVal = (long)regKey.GetValue(value);
                    result = (boolVal == 1);
                }
                //Treat other types as invalid and ignore so they flag as not existing
                regKey.Close();
            }

            return result;
        }

        public static int? ReadMachineInt32(string value)
        {
            return ReadMachineInt32(value, null, null);
        }

        public static int? ReadMachineInt32(string value, string offset)
        {
            return ReadMachineInt32(value, offset, null);
        }

        public static int? ReadMachineInt32(string value, string offset, string key)
        {
            int? result = null;
            RegistryKey regKey = LoadRegistryKeyValue(value, offset, key);
            //LoadRegistryKeyValue returns either the value or a subkey (To handle lists).  In this case we only care about the value and want to disregard subkeys
            if (regKey != null && regKey.GetValue(value) != null)
            {
                RegistryValueKind valueType = regKey.GetValueKind(value);
                if (valueType == RegistryValueKind.DWord || valueType == RegistryValueKind.String)
                {
                    result = Convert.ToInt32(regKey.GetValue(value), CultureInfo.InvariantCulture);
                }
                else if (valueType == RegistryValueKind.QWord)
                {
                    long longVal = (long)regKey.GetValue(value);

                    result = Convert.ToInt32(longVal);
                }
                //Treat other types as invalid and ignore so they flag as not existing
                regKey.Close();
            }

            return result;
        }

        public static int ReadMachineInt32(string value, int defaultValue)
        {
            return ReadMachineInt32(value, defaultValue, null);
        }

        public static int ReadMachineInt32(string value, int defaultValue, string offset)
        {
            int? result = ReadMachineInt32(value, offset);
            if (result == null)
            {
                return defaultValue;
            }
            else
            {
                return (int)result;
            }
        }

        public static uint? ReadMachineUInt32(string value, string offset, string key)
        {
            uint? result = null;
            RegistryKey regKey = LoadRegistryKeyValue(value, offset, key);
            //LoadRegistryKeyValue returns either the value or a subkey (To handle lists).  In this case we only care about the value and want to disregard subkeys
            if (regKey != null && regKey.GetValue(value) != null)
            {
                RegistryValueKind valueType = regKey.GetValueKind(value);
                if (valueType == RegistryValueKind.DWord || valueType == RegistryValueKind.String)
                {
                    result = Convert.ToUInt32(regKey.GetValue(value), CultureInfo.InvariantCulture);
                }
                else if (valueType == RegistryValueKind.QWord)
                {
                    long longVal = (long)regKey.GetValue(value);
                    result = Convert.ToUInt32(longVal);
                }
                //Treat other types as invalid and ignore so they flag as not existing
                regKey.Close();
            }

            return result;
        }

        public static uint ReadMachineUInt32(string value, uint defaultValue)
        {
            return ReadMachineUInt32(value, defaultValue, null);
        }

        public static uint? ReadMachineUInt32(string value, string offset)
        {
            return ReadMachineUInt32(value, offset, null);
        }

        public static uint ReadMachineUInt32(string value, uint defaultValue, string offset)
        {
            uint? result = ReadMachineUInt32(value, offset);
            if (result == null)
            {
                return defaultValue;
            }
            else
            {
                return (uint)result;
            }
        }

        public static uint? ReadMachineUInt32(string value)
        {
            return ReadMachineUInt32(value, null, null);
        }

        public static Guid? ReadMachineGuid(string value, string offset)
        {
            string result = ReadMachineString(value, offset);
            if (Guid.TryParse(result, out Guid guid))
            {
                return guid;
            }
            else
            {
                return null;
            }
        }

        public static bool ReadMachineBoolean(string value, bool defaultValue)
        {
            return ReadMachineBoolean(value, defaultValue, null);
        }

        public static bool ReadMachineBoolean(string value, bool defaultValue, string offset)
        {
            bool? result = ReadMachineBoolean(value, offset);
            if (result == null)
            {
                return defaultValue;
            }
            else
            {
                return (bool)result;
            }
        }

        public static IList<string> ReadMachineList(string value, string offset)
        {
            Dictionary<string, string> HashtableResult = ReadMachineHashtable(value, offset);
            if (HashtableResult == null) return new List<string>();

            return HashtableResult.Values.ToList();
        }

        public static Dictionary<string, string> ReadMachineHashtable(string value, string offset)
        {
            return ReadMachineHashtable(value, offset, null);
        }

        public static Dictionary<string, string> ReadMachineHashtable(string value, string offset, string key)
        {
            Dictionary<string, string> returnTable = new Dictionary<string, string>();
            //Check for Root Registry Entry
            RegistryKey regKey = LoadRegistryKeyValue(value, offset, key);
            if (regKey == null) return returnTable; // return empty table
            //Add Child Objects into table
            foreach (string subKey in regKey.GetValueNames())
            {
                RegistryValueKind valueType = regKey.GetValueKind(subKey);
                if (valueType == RegistryValueKind.String)
                {
                    string resultValue = (string)regKey.GetValue(subKey);
                    returnTable.Add(subKey, resultValue.Replace("\0",""));
                }
                //Skip any non string values
            }
            regKey.Close();

            return returnTable;
        }

        public static IList<string> ReadMachineSubkeys(string regKeyPath)
        {
            return ReadMachineSubkeys(regKeyPath, null);
        }

        public static IList<string> ReadMachineSubkeys(string regKeyPath, string regKeyOffset)
        {
            IList<string> returnList = new List<string>();
            using (RegistryKey regKey = LoadRegistryKeyValue(null, regKeyOffset, regKeyPath))
            {
                if (regKey == null) return returnList; // return empty list
                //Add Child Objects into list
                foreach (string key in regKey.GetSubKeyNames())
                {
                    returnList.Add(key);
                }

                return returnList;
            }
        }

        public static RegistryKey OpenRegistryForWrite(string regPath, string value, string offset)
        {
            RegistryKey regKey = OpenRegistryKey(regPath, value, offset, true);
            if (regKey == null)
            {
                RegistryKey rootPath = GetHKLM().CreateSubKey(regPath);
                if (!string.IsNullOrWhiteSpace(offset))
                {
                    regKey = rootPath.CreateSubKey(offset);
                }
                else
                {
                    regKey = rootPath;
                }
            }
            return regKey;
        }

        private static RegistryKey OpenRegistryKeyForWrite(string value, string offset)
        {
            return OpenRegistryKeyForWrite(RegistrySettings.ManualPath, value, offset);
        }

        private static RegistryKey OpenRegistryKeyForWrite(string regPath, string value, string offset)
        {
            RegistryKey regKey = OpenRegistryKey(regPath, value, offset, true);
            if (regKey == null)
            {
                RegistryKey rootPath = GetHKLM().CreateSubKey(RegistrySettings.ManualPath);
                if (!string.IsNullOrWhiteSpace(offset))
                {
                    regKey = rootPath.CreateSubKey(offset);
                }
                else
                {
                    regKey = rootPath;
                }
                regKey = regKey.CreateSubKey(value);
            }
            return regKey;
        }

        public static bool ClearRegistryKeyValue(string key, string value)
        {
            bool result = false; //Did something get updated
            using (RegistryKey regKey = OpenRegistryKey(key, value, null, true))
            {
                if (regKey != null)
                {
                    object valueData = regKey.GetValue(value);
                    if (valueData != null)
                    {
                        switch (regKey.GetValueKind(value))
                        {
                            case RegistryValueKind.MultiString:
                                {
                                    string[] currentData = (string[])valueData;
                                    if (currentData.Length > 1 || //clear if more than 1 entity
                                        (currentData.Length == 1 && !string.IsNullOrWhiteSpace(currentData[0]))) //clear if there is 1 entity but there is data in that entity
                                        //skip if there are no entities or 1 entity but it is whitespace
                                    {
                                        regKey.SetValue(value, new string[] { "" }, regKey.GetValueKind(value));
                                        result = true;
                                    }
                                    break;
                                }
                            case RegistryValueKind.ExpandString: //Same as String
                            case RegistryValueKind.String:
                                {
                                    string currentData = (string)valueData;
                                    if (!string.IsNullOrWhiteSpace(currentData))
                                    {
                                        regKey.SetValue(value, "", regKey.GetValueKind(value));
                                        result = true;
                                    }
                                    break;
                                }
                            case RegistryValueKind.DWord:
                                {
                                    int currentData = (int)valueData;
                                    if (currentData != 0)
                                    {
                                        regKey.SetValue(value, 0, regKey.GetValueKind(value));
                                        result = true;
                                    }
                                    break;
                                }
                            case RegistryValueKind.QWord:
                                {
                                    long currentData = (long)valueData;
                                    if (currentData != 0)
                                    {
                                        regKey.SetValue(value, 0, regKey.GetValueKind(value));
                                        result = true;
                                    }
                                    break;
                                }
                            default: throw new NotImplementedException("ClearRegistryKeyValue not implemented for registry of Type " + regKey.GetValueKind(value));
                        }
                    }
                }
            }
            return result;
        }

        public static void RemoveRegistryKeyValue(string key, string value)
        {
            using (RegistryKey regKey = OpenRegistryKey(key, value, null, true))
            {
                regKey?.DeleteValue(value, false); //Don't throw exception if the value doesn't exist
            }
        }

        public static void ClearRegistryKey(string key)
        {
            using (RegistryKey regKey = OpenRegistryKey(key, null, null, true))
            {
                if (regKey != null)
                {
                    foreach (string subkey in regKey.GetSubKeyNames())
                    {
                        regKey.DeleteSubKeyTree(subkey, false); //Don't throw exception if the subkey doesn't exist
                        regKey.DeleteSubKey(subkey, false);
                    }
                }
            }
        }

        public static void RemoveRegistryKey(string key, string subkey)
        {
            using (RegistryKey regKey = OpenRegistryKey(key, null, null, true))
            {
                if (regKey != null)
                {
                    regKey.DeleteSubKeyTree(subkey, false); //Don't throw exception if the subkey doesn't exist
                    regKey.DeleteSubKey(subkey, false);
                }
            }
        }

        public static void SaveMachineData(string value, string data)
        {
            SaveMachineData(value, data, RegistrySettings.InternalStateOffset);
        }

        public static void SaveMachineData(string value, string[] data)
        {
            SaveMachineData(value, data, RegistrySettings.InternalStateOffset);
        }

        public static void SaveMachineData(string value, IList<string> data)
        {
            SaveMachineData(value, data, RegistrySettings.InternalStateOffset);
        }

        public static void SaveMachineData(string value, Dictionary<string, string> data)
        {
            SaveMachineData(value, data, RegistrySettings.InternalStateOffset);
        }

        public static void SaveMachineData(string value, bool data)
        {
            SaveMachineData(value, data, RegistrySettings.InternalStateOffset);
        }

        public static void SaveMachineData(string value, bool data, string offset)
        {
            SaveMachineData(value, data, RegistrySettings.ManualPath, offset);
        }

        public static void SaveMachineData(string value, bool data, string regPath, string regPathOffset)
        {
            using (RegistryKey regKey = OpenRegistryForWrite(regPath, value, regPathOffset))
            {
                if (data)
                {
                    regKey.SetValue(value, 1, RegistryValueKind.DWord);
                }
                else
                {
                    regKey.SetValue(value, 0, RegistryValueKind.DWord);
                }
            }
        }

        public static void SaveMachineData(string value, int data)
        {
            SaveMachineData(value, data, RegistrySettings.InternalStateOffset);
        }

        public static void SaveMachineData(string value, int data, string regPathOffset)
        {
            SaveMachineData(value, data, RegistrySettings.ManualPath, regPathOffset);
        }

        public static void SaveMachineData(string value, int data, string regPath, string regPathOffset)
        {
            using (RegistryKey regKey = OpenRegistryForWrite(regPath, value, regPathOffset))
            {
                regKey.SetValue(value, data, RegistryValueKind.DWord);
            }
        }

        public static void SaveMachineData(string value, uint data, string regPath, string regPathOffset)
        {
            using (RegistryKey regKey = OpenRegistryForWrite(regPath, value, regPathOffset))
            {
                regKey.SetValue(value, data, RegistryValueKind.DWord);
            }
        }

        public static void SaveMachineData(string value, Guid data)
        {
            SaveMachineData(value, data, RegistrySettings.ManualPath, RegistrySettings.InternalStateOffset);
        }

        public static void SaveMachineData(string value, Guid data, string regPath, string regPathOffset)
        {
            using (RegistryKey regKey = OpenRegistryForWrite(regPath, value, regPathOffset))
            {
                regKey.SetValue(value, data.ToString(), RegistryValueKind.String);
            }
        }

        public static bool SaveMachineDataAsBinary(string value, Guid data, string regPath, string regPathOffset)
        {
            using (RegistryKey regKey = OpenRegistryForWrite(regPath, value, regPathOffset))
            {
                byte[] currentValue = ReadMachineBinary(value, regPathOffset, regPath, false);
                if (currentValue == null || !currentValue.SequenceEqual(data.ToByteArray()))
                {
                    regKey.SetValue(value, data.ToByteArray(), RegistryValueKind.Binary);
                    return true;
                }
            }
            return false;
        }

        public static void SaveMachineData(string value, long data)
        {
            SaveMachineData(value, data, RegistrySettings.ManualPath, RegistrySettings.InternalStateOffset);
        }

        public static void SaveMachineData(string value, long data, string regPath, string regPathOffset)
        {
            using (RegistryKey regKey = OpenRegistryForWrite(regPath, value, regPathOffset))
            {
                regKey.SetValue(value, data, RegistryValueKind.QWord);
            }
        }

        public static void UpdateMachineData(string key, string value, object data)
        {
            using (RegistryKey regKey = OpenRegistryKey(key, value, null, true))
            {
                regKey?.SetValue(value, data);
            }
        }

        public static void UpdateMachineData(string key, string value, IList<string> data)
        {
            using (RegistryKey regKey = OpenRegistryKey(key, value, null, true))
            {
                regKey?.SetValue(value, data.ToArray(), RegistryValueKind.MultiString);
            }
        }

        private static void SaveMachineData(string value, string[] data, string offset)
        {
            using (RegistryKey regKey = OpenRegistryForWrite(RegistrySettings.ManualPath, value, offset))
            {
                regKey.SetValue(value, data, RegistryValueKind.MultiString);
            }
        }

        public static void SaveMachineData(string value, string data, string offset)
        {
            SaveMachineData(value, data, RegistrySettings.ManualPath, offset);
        }

        public static bool SaveMachineData(string value, string data, string regPath, string regPathOffset)
        {
            using (RegistryKey regKey = OpenRegistryForWrite(regPath, value, regPathOffset))
            {
                string currentValue = ReadMachineString(value, regPathOffset, regPath, false);
                if (currentValue == null || currentValue != data)
                {
                    regKey.SetValue(value, data, RegistryValueKind.String);
                    return true;
                }
            }
            return false; //No update made
        }

        public static void SaveMachineData(string value, IList<string> data, string offset)
        {
            Dictionary<string, string> hashTable = new Dictionary<string, string>();

            for (int i = 0;i < data.Count;i++)
            {
                hashTable.Add((i+1).ToString(CultureInfo.InvariantCulture), data[i]);
            }

            SaveMachineData(value, hashTable, offset);
        }

        public static void SaveMachineData(string value, Dictionary<string,string> data, string offset)
        {
            using (RegistryKey regKey = OpenRegistryKeyForWrite(value, offset))
            {
                //Clean up any existing values in this key
                if (regKey.ValueCount > 0)
                {
                    string[] valueList = regKey.GetValueNames();
                    foreach (string oldValue in valueList)
                    {
                        regKey.DeleteValue(oldValue, false);
                    }
                }

                foreach (KeyValuePair<string, string> dataValue in data)
                {
                    regKey.SetValue(dataValue.Key, dataValue.Value ?? "", RegistryValueKind.String);
                }
            }
        }

        //Return the internal Registry Setting name depending on the type of profile created.  Used to cache name for cleanup purposes
        public static string GetProfileNameRegistryName(ProfileType profileType)
        {
            switch (profileType)
            {
                case ProfileType.Machine: return RegistrySettings.MachineProfileName;
                case ProfileType.User: return RegistrySettings.UserProfileName;
                case ProfileType.UserBackup: return RegistrySettings.BackupUserProfileName;
                default: throw new NotImplementedException("Missing GetProfileNameRegistryName Lookup for profile type " + profileType);
            }
        }
    }
}