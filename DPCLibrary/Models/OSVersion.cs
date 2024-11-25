using DPCLibrary.Enums;
using DPCLibrary.Utils;
using System;

namespace DPCLibrary.Models
{
    public class OSVersion
    {
        public MajorOSVersion MajorOSVersion { get; }
        public string OSSubVersion { get; }
        public Version Version { get; }

        public bool IsGreaterThanWin10_2004 { get; }
        public bool IsGreaterThanWin10_1703 { get; }
        public bool IsGreaterThanWin11_22H2 { get; }
        public bool WMIWorking { get; }

        public OSVersion(Version envVersion)
        {
            if (envVersion == null)
            {
                throw new ArgumentNullException(nameof(envVersion));
            }

            if (envVersion.Major < 10)
            {
                MajorOSVersion = MajorOSVersion.PreWindows10;
            }
            else if (envVersion.Major == 10 && envVersion.Build < 22000)
            {
                MajorOSVersion = MajorOSVersion.Windows10;
            }
            else if (envVersion.Major == 10 && envVersion.Build >= 22000)
            {
                MajorOSVersion = MajorOSVersion.Windows11;
            }
            else
            {
                MajorOSVersion = MajorOSVersion.Unknown;
            }

            OSSubVersion = AccessRegistry.ReadMachineString(RegistrySettings.DisplayVersion, null, RegistrySettings.OSVersion, true);
            int? OSPatchVersionNull = AccessRegistry.ReadMachineInt32(RegistrySettings.PatchVersion, null, RegistrySettings.OSVersion);
            int OSPatchVersion = 0;
            if (OSPatchVersionNull.HasValue)
            {
                OSPatchVersion = OSPatchVersionNull.Value;
            }

            //Greater than Windows 10 2004
            if (envVersion.Major > 10 || (envVersion.Major == 10 && envVersion.Build >= 19041))
            {
                IsGreaterThanWin10_2004 = true;
            }

            //Greater than Windows 10 1703
            if (envVersion.Major > 10 || (envVersion.Major == 10 && envVersion.Build >= 15063))
            {
                IsGreaterThanWin10_1703 = true;
            }

            //Greater than Windows 11 September 2022 Quality Update as this makes WMI work on Win11
            if ((envVersion.Major < 10) || //Pre Windows 10
                (envVersion.Major == 10 && envVersion.Build < 20000) || //Windows 10
                (envVersion.Major == 10 && envVersion.Build >= 22000 && envVersion.Build < 22621 && OSPatchVersion >= 1344) || //Working Windows 11 21H2 after specific patch
                (envVersion.Major == 10 && envVersion.Build == 22621 && OSPatchVersion >= 1778) || //Working Windows 11 22H2 after specific patch
                (envVersion.Major == 10 && envVersion.Build > 22621)) //Working Windows 11 22H2 after specific patch
            {
                WMIWorking = true;
            }

            //Greater than Windows 11 22H2
            if (envVersion.Major > 10 || (envVersion.Major == 10 && envVersion.Build >= 22621))
            {
                IsGreaterThanWin11_22H2 = true;
            }

            Version = new Version(envVersion.Major, envVersion.Minor, envVersion.Build, OSPatchVersion);
        }

        public override string ToString()
        {
            return MajorOSVersion + " - " + Version.ToString();
        }
    }
}
