using DPCLibrary.Models;
using System;
using System.Security.Principal;

namespace DPCLibrary.Utils
{
    public static class DeviceInfo
    {
        private static OSVersion osVersion = null;
        public const string SYSTEMSID = "S-1-5-18";
        public static string DeviceName()
        {
            return Environment.MachineName;
        }

        public static OSVersion GetOSVersion()
        {
            if (osVersion == null)
            {
                osVersion = new OSVersion(Environment.OSVersion.Version);
            }

            return osVersion;
        }

        public static string CurrentUserSID()
        {
            return WindowsIdentity.GetCurrent().User.ToString();
        }

        public static bool IsUserAdministrator()
        {
            bool isAdmin;
            try
            {
                //get the currently logged in user
                WindowsIdentity user = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(user);
                isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                isAdmin = false;
            }
            return isAdmin;
        }

        public static bool IsCurrentUserSYSTEM()
        {
            return CurrentUserSID() == SYSTEMSID;
        }
    }
}
