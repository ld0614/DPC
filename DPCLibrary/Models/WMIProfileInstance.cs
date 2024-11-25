using DPCLibrary.Utils;
using Microsoft.Management.Infrastructure;

namespace DPCLibrary.Models
{
    public class WMIProfileInstance
    {
        public string ProfileName { get; set; }
        public string SID { get; set; }
        public CimInstance Instance { get; set; }
        public string ProfileData { get; set; }

        public WMIProfileInstance(string sid, CimInstance instance)
        {
            ProfileName = GetProfileName(instance);
            SID = sid;
            Instance = instance;
            ProfileData = GetProfileData(instance);
        }

        public WMIProfileInstance(string sid, string profileName)
        {
            ProfileName = profileName;
            SID = sid;
            Instance = null;
            ProfileData = null; //Unable to get instance
        }

        public RASPhonePBKProfile GetPBKProfile()
        {
            return new RASPhonePBKProfile(SID, ProfileName);
        }

        private static string GetProfileName(CimInstance instance)
        {
            return AccessWMI.Unsanitize((string)instance.CimInstanceProperties["InstanceID"].Value);
        }
        private static string GetProfileData(CimInstance instance)
        {
            return AccessWMI.Unsanitize((string)instance.CimInstanceProperties["ProfileXML"].Value);
        }
    }
}
