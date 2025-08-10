using DPCLibrary.Enums;
using DPCLibrary.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DPCLibrary.Models
{
    public class RASPhonePBKProfile
    {
        private Dictionary<string, string> ProfileSettings = new Dictionary<string, string>();

        public RASPhonePBKProfile(ProfileInfo profile)
        {
            LoadProfile(profile.Sid, profile.ProfileName);
        }

        public RASPhonePBKProfile(string SID, string profileName)
        {
            LoadProfile(SID, profileName);
        }

        private void LoadProfile(string SID, string profileName)
        {
            UserInfo userDetails = ManageRasphonePBK.GetUserInfo(SID);
            string pbkPath = ManageRasphonePBK.GetPrimaryPBKFile(userDetails);
            if (!string.IsNullOrWhiteSpace(pbkPath))
            {
                INIParser pbkFile = new INIParser(pbkPath);
                Dictionary<string, string> profileSettingsNullCheck = pbkFile.GetSection(profileName);
                if (profileSettingsNullCheck != null)
                {
                    ProfileSettings = profileSettingsNullCheck;
                }
            }
        }

        public bool GetAlwaysOnCapable()
        {
            if (ProfileSettings.ContainsKey("AlwaysOnCapable"))
            {
                try
                {
                    return Convert.ToBoolean(Convert.ToInt32(ProfileSettings["AlwaysOnCapable"], CultureInfo.InvariantCulture));
                }
                catch
                {
                    //Any error just assume that the tunnel is not always on
                    return false;
                }
            }
            else
            {
                //Assume not always on if setting isn't present
                return false;
            }
        }

        public bool? GetDeviceTunnelStatus()
        {
            if (ProfileSettings.ContainsKey("DeviceTunnel"))
            {
                try
                {
                    return Convert.ToBoolean(Convert.ToInt32(ProfileSettings["DeviceTunnel"],CultureInfo.InvariantCulture));
                }
                catch
                {
                    //Any error just assume that the tunnel is not a device tunnel
                    return false;
                }
            }
            else
            {
                return null;
            }
        }

        public bool GetPBKOptionsStatus(PBKOptions optionFlag)
        {
            if (ProfileSettings.ContainsKey("Options"))
            {
                try
                {
                    PBKOptions optionsFlags = (PBKOptions)Convert.ToUInt32(ProfileSettings["Options"], CultureInfo.InvariantCulture);
                    return optionsFlags.HasFlag(optionFlag);
                }
                catch
                {
                    //Any error just assume that the tunnel does not have the flag set
                    return false;
                }
            }
            else
            {
                //If not included in PBK assume not registering
                return false;
            }
        }

        public bool GetRegisterDNSStatus()
        {
            if (ProfileSettings.ContainsKey("IpDnsFlags"))
            {
                try
                {
                    return Convert.ToBoolean(Convert.ToInt32(ProfileSettings["IpDnsFlags"], CultureInfo.InvariantCulture));
                }
                catch
                {
                    //Any error just assume that the tunnel is not registering DNS
                    return false;
                }
            }
            else
            {
                //If not included in PBK assume not registering
                return false;
            }
        }

        public bool GetDisableClassBasedDefaultRouteStatus()
        {
            if (ProfileSettings.ContainsKey("DisableClassBasedDefaultRoute"))
            {
                try
                {
                    return Convert.ToBoolean(Convert.ToInt32(ProfileSettings["DisableClassBasedDefaultRoute"], CultureInfo.InvariantCulture));
                }
                catch
                {
                    //Any error just assume that the tunnel is not registering DNS
                    return false;
                }
            }
            else
            {
                //If not included in PBK assume not registering
                return false;
            }
        }

        public IList<Route> GetRouteList()
        {
            if (!ProfileSettings.ContainsKey("Routes"))
            {
                return null;
            }

            string binaryRouteList = ProfileSettings["Routes"];
            if (binaryRouteList.Length % 72 != 0)
            {
                throw new Exception("Route List Binary was the wrong size");
            }

            string[] Routes = binaryRouteList.SplitByLength(72, true).ToArray();
            List<Route> RouteList = new List<Route>();
            foreach (string routeBinary in Routes)
            {
                RouteList.Add(new Route(routeBinary));
            }

            return RouteList;
        }

        public IList<TrafficFilter> GetTrafficFilterList()
        {
            if (!ProfileSettings.ContainsKey("PerAppTrafficFilters"))
            {
                return null;
            }

            string binaryTrafficFilterList = ProfileSettings["PerAppTrafficFilters"];
            if (string.IsNullOrWhiteSpace(binaryTrafficFilterList))
            {
                return null;
            }
            string decodedData = INIParser.DecodeHexString(binaryTrafficFilterList);

            string[] splitRules = decodedData.Split(new char[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);

            List<TrafficFilter> trafficFilterList = new List<TrafficFilter>();

            foreach (string rule in splitRules)
            {
                string[] ruleAttributes = rule.Trim('\0').Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                Dictionary<string, string> ruleData = new Dictionary<string, string>();

                foreach (string attrib in ruleAttributes.Skip(1)) //Ignore first element as its the version of the rule (v2.24)
                {
                    string[] parts = attrib.Split(new char[] { '=' }, 2);
                    if (ruleData.ContainsKey(parts[0]))
                    {
                        ruleData[parts[0]] += "," + parts[1]; //Where there is more than one element (such as multiple IP address/RA4 etc) concat with comma
                    }
                    else
                    {
                        ruleData.Add(parts[0], parts[1]);
                    }
                }
                trafficFilterList.Add(new TrafficFilter(ruleData));
            }

            return trafficFilterList;
        }

        public bool GetDeviceComplianceEnabled()
        {
            if (ProfileSettings.ContainsKey("DeviceComplianceEnabled"))
            {
                try
                {
                    return Convert.ToBoolean(Convert.ToInt32(ProfileSettings["DeviceComplianceEnabled"], CultureInfo.InvariantCulture));
                }
                catch
                {
                    //Any error just assume that Device Compliance isn't enabled
                    return false;
                }
            }
            else
            {
                //Assume Device Compliance isn't enabled if setting isn't present
                return false;
            }
        }

        public bool GetDeviceComplianceSSOEnabled()
        {
            if (ProfileSettings.ContainsKey("DeviceComplianceSsoEnabled"))
            {
                try
                {
                    return Convert.ToBoolean(Convert.ToInt32(ProfileSettings["DeviceComplianceSsoEnabled"], CultureInfo.InvariantCulture));
                }
                catch
                {
                    //Any error just assume that Device Compliance SSO isn't enabled
                    return false;
                }
            }
            else
            {
                //Assume Device Compliance SSO isn't enabled if setting isn't present
                return false;
            }
        }

        public string GetDeviceComplianceEKU()
        {
            if (ProfileSettings.ContainsKey("DeviceComplianceSsoEku"))
            {
                try
                {
                    string result = ProfileSettings["DeviceComplianceSsoEku"];
                    if (string.IsNullOrWhiteSpace(result))
                    {
                        //return null rather than "" as this will then match the CSP Profile
                        return null;
                    }
                    return result;
                }
                catch
                {
                    //Any error just assume that Device Compliance SSO isn't enabled
                    return null;
                }
            }
            else
            {
                //Assume Device Compliance SSO isn't enabled if setting isn't present
                return null;
            }
        }

        public string GetDeviceComplianceIssuerHash()
        {
            if (ProfileSettings.ContainsKey("DeviceComplianceSsoIssuer"))
            {
                try
                {
                    string result = ProfileSettings["DeviceComplianceSsoIssuer"];
                    if (string.IsNullOrWhiteSpace(result))
                    {
                        //return null rather than "" as this will then match the CSP Profile
                        return null;
                    }
                    return result;
                }
                catch
                {
                    //Any error just assume that Device Compliance SSO isn't enabled
                    return null;
                }
            }
            else
            {
                //Assume Device Compliance SSO isn't enabled if setting isn't present
                return null;
            }
        }
    }
}
