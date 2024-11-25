using DPCLibrary.Exceptions;
using DPCLibrary.Utils;
using System.Threading;

namespace DPCLibrary.Models
{
    public class ProfileInfo
    {
        public string ProfileName { get; set; }
        public string PBKPath { get; set; }
        public string Sid { get; set; }
        public bool Hidden { get; set; }

        public string GetVpnProfileFromWMI(CancellationToken cancelToken)
        {
            if (Sid != DeviceInfo.CurrentUserSID())
            {
                throw new InvalidProfileException("Unable to Retrieve WMI Profile for non-SYSTEM Profile");
            }

            return AccessWMI.GetWMIVPNConfig(ProfileName, cancelToken);
        }
    }
}
