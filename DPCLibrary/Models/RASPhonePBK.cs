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
        private static readonly char[] NullTerminatorSeparator = { '\0' };
        private static readonly char[] RuleAttributeSeparator = { '|' };
        private static readonly char[] KeyValueSeparator = { '=' };
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
            if (ProfileSettings.TryGetValue("AlwaysOnCapable", out string alwaysOnCapable))
            {
                try
                {
                    return Convert.ToBoolean(Convert.ToInt32(alwaysOnCapable, CultureInfo.InvariantCulture));
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
            if (ProfileSettings.TryGetValue("DeviceTunnel", out string deviceTunnel))
            {
                try
                {
                    return Convert.ToBoolean(Convert.ToInt32(deviceTunnel, CultureInfo.InvariantCulture));
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
            if (ProfileSettings.TryGetValue("Options", out string options))
            {
                try
                {
                    PBKOptions optionsFlags = (PBKOptions)Convert.ToUInt32(options, CultureInfo.InvariantCulture);
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
            if (ProfileSettings.TryGetValue("IpDnsFlags", out string ipDnsFlags))
            {
                try
                {
                    return Convert.ToBoolean(Convert.ToInt32(ipDnsFlags, CultureInfo.InvariantCulture));
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
            if (ProfileSettings.TryGetValue("DisableClassBasedDefaultRoute", out string disableClassBasedDefaultRoute))
            {
                try
                {
                    return Convert.ToBoolean(Convert.ToInt32(disableClassBasedDefaultRoute, CultureInfo.InvariantCulture));
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
            if (!ProfileSettings.TryGetValue("Routes", out string binaryRouteList))
            {
                return null;
            }

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
            if (!ProfileSettings.TryGetValue("PerAppTrafficFilters", out string binaryTrafficFilterList))
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(binaryTrafficFilterList))
            {
                return null;
            }
            string decodedData = INIParser.DecodeHexString(binaryTrafficFilterList);

            string[] splitRules = decodedData.Split(NullTerminatorSeparator, StringSplitOptions.RemoveEmptyEntries);

            List<TrafficFilter> trafficFilterList = new List<TrafficFilter>();

            foreach (string rule in splitRules)
            {
                string[] ruleAttributes = rule.Trim('\0').Split(RuleAttributeSeparator, StringSplitOptions.RemoveEmptyEntries);
                Dictionary<string, string> ruleData = new Dictionary<string, string>();

                foreach (string attrib in ruleAttributes.Skip(1)) //Ignore first element as its the version of the rule (v2.24)
                {
                    string[] parts = attrib.Split(KeyValueSeparator, 2);
                    if (ruleData.TryGetValue(parts[0], out string existingValue))
                    {
                        ruleData[parts[0]] = existingValue + "," + parts[1]; //Where there is more than one element (such as multiple IP address/RA4 etc) concat with comma
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
            if (ProfileSettings.TryGetValue("DeviceComplianceEnabled", out string deviceComplianceEnabled))
            {
                try
                {
                    return Convert.ToBoolean(Convert.ToInt32(deviceComplianceEnabled, CultureInfo.InvariantCulture));
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
            if (ProfileSettings.TryGetValue("DeviceComplianceSsoEnabled", out string deviceComplianceSsoEnabled))
            {
                try
                {
                    return Convert.ToBoolean(Convert.ToInt32(deviceComplianceSsoEnabled, CultureInfo.InvariantCulture));
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
            if (ProfileSettings.TryGetValue("DeviceComplianceSsoEku", out string deviceComplianceSsoEku))
            {
                try
                {
                    string result = deviceComplianceSsoEku;
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
            if (ProfileSettings.TryGetValue("DeviceComplianceSsoIssuer", out string deviceComplianceSsoIssuer))
            {
                try
                {
                    string result = deviceComplianceSsoIssuer;
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
