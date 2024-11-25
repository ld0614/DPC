using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DPCLibrary.Utils
{
    public class INIParser
    {
        private string Path;
        private Dictionary<string, Dictionary<string, string>> Sections;
        public INIParser(string filePath)
        {
            Path = filePath;
            ProcessFile();
        }

        public string OpenFile()
        {
            string data = "";
            if (File.Exists(Path))
            {
                using (TextReader reader = new StreamReader(Path))
                {
                    data = reader.ReadToEnd();
                }
            }
            return data;
        }

        private void ProcessFile()
        {
            Sections = new Dictionary<string, Dictionary<string, string>>();
            if (File.Exists(Path))
            {
                string sectionName = "";
                Dictionary<string, string> data = new Dictionary<string, string>();
                using (TextReader reader = new StreamReader(Path))
                {
                    string line;
                    do
                    {
                        line = reader.ReadLine();
                        if (line != null)
                        {
                            if (line.StartsWith("[") && line.EndsWith("]"))
                            {
                                AddSection(sectionName, data);
                                sectionName = line.TrimStart('[').TrimEnd(']');
                                data = new Dictionary<string, string>();
                            }
                            else
                            {
                                //Ignore Comments
                                if (!line.StartsWith("'") && !string.IsNullOrWhiteSpace(line))
                                {
                                    string[] parts = line.Split(new char[] { '=' }, 2);
                                    if (parts.Count() < 2)
                                    {
                                        parts = new string[2] { parts[0], "" };
                                    }
                                    if (data.ContainsKey(parts[0]))
                                    {
                                        data[parts[0]] += parts[1];
                                    }
                                    else
                                    {
                                        data.Add(parts[0], parts[1]);
                                    }
                                }
                            }
                        }
                    } while (line != null);

                    AddSection(sectionName, data);
                }
            }
        }

        private void AddSection(string sectionName, Dictionary<string, string> sectionData)
        {
            if (!string.IsNullOrWhiteSpace(sectionName) && sectionData.Count > 0)
            {
                Sections.Add(sectionName, sectionData);
            }
        }

        public IList<string> GetSections()
        {
            return Sections.Keys.ToList();
        }

        public Dictionary<string, string> GetSection(string name)
        {
            if (Sections.ContainsKey(name))
            {
                return Sections[name];
            }

            return null;
        }

        public static bool FormatPBKByteAsBool(string hexString)
        {
            return Convert.ToBoolean(Convert.ToUInt32(FormatPBKByteAsString(hexString), 16));
        }

        public static uint FormatPBKByteAsUInt(string hexString)
        {
            return Convert.ToUInt32(FormatPBKByteAsString(hexString), 16);
        }

        public static string FormatPBKByteAsString(string hexString)
        {
            string formattedNumber = "";
            IEnumerable<string> policyListIE = hexString.SplitByLength(2, false);
            foreach (string number in policyListIE)
            {
                formattedNumber = formattedNumber.Insert(0, number);
            }
            return formattedNumber;
        }

        public static string DecodeHexString(string hexString)
        {
            IEnumerable<string> bytelist = hexString.SplitByLength(2);
            byte[] bytes = new byte[bytelist.Count()];
            for (int i = 0; i < bytelist.Count(); i++)
            {
                bytes[i] = Convert.ToByte(bytelist.ElementAt(i), 16);
            }
            return Encoding.Unicode.GetString(bytes).Trim('\0');
        }
    }
}
