using DPCLibrary.Enums;
using DPCLibrary.Exceptions;
using DPCLibrary.Models;
using DPCLibrary.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace DPCService.Utils
{
    public static class ProfileAction
    {
        public static void RemoveProfileIfExists(ManagedProfile profile, bool eventOnRemove)
        {
            try
            {
                if (ManageRasphonePBK.RemoveProfile(profile.ProfileName))
                {
                    Cleanup(profile.ProfileName, false, profile.ProfileType);
                    if (eventOnRemove)
                    {
                        DPCServiceEvents.Log.ProfileProfileRemoved(profile.ProfileName);
                    }
                    DPCServiceEvents.Log.ProfileDebugProfileRemoved(profile.ProfileName);
                }
                else
                {
                    DPCServiceEvents.Log.ProfileDebugProfileRemoveNotRequired(profile.ProfileName);
                }
            }
            catch (Exception e)
            {
                DPCServiceEvents.Log.IssueDeletingProfile(profile.ProfileName, e.Message, e.StackTrace);
            }
        }

        public static void Cleanup(string profileName, bool showEvents, ProfileType profileType)
        {
            //Root path is HKLM:\SOFTWARE\Microsoft\EnterpriseResourceManager\Tracked
            //There is then 1 or more random GUIDs (Typically 1)
            //Under the GUID there is a device key + user SIDs
            //Under the device or user key there are 1 or more inner keys (typically default)
            //Under the inner key there are 1 or more values with PathX (X being a number) and the VPN name such as ./device/Vendor/MSFT/VPNv2/AOVPN
            //There is also a count value for tracking the number of entries
            //When there are no more entries the device or user key is removed
            //./device/Vendor/MSFT/VPNv2/AOVPN may vary but the last part is the profile name

            IList<string> subPaths = AccessRegistry.ReadMachineSubkeys(RegistrySettings.EnterpriseResourceManagerPath);

            foreach (string path in subPaths)
            {
                IList<string> userPaths = AccessRegistry.ReadMachineSubkeys(RegistrySettings.EnterpriseResourceManagerPath, path);
                foreach (string userPath in userPaths)
                {
                    IList<string> innerPaths = AccessRegistry.ReadMachineSubkeys(RegistrySettings.EnterpriseResourceManagerPath + "\\" + path, userPath);
                    foreach (string innerPath in innerPaths)
                    {
                        //Looking at HKLM:\SOFTWARE\Microsoft\EnterpriseResourceManager\Tracked\PATH\USERPATH\default
                        Dictionary<string, string> vpnProfiles = AccessRegistry.ReadMachineHashtable(innerPath, userPath, RegistrySettings.EnterpriseResourceManagerPath + "\\" + path);
                        int vpnCount = vpnProfiles.Count;
                        foreach (KeyValuePair<string, string> vpnProfile in vpnProfiles)
                        {
                            if (AccessWMI.Unsanitize(vpnProfile.Value.Split('/').Last()) == profileName)
                            {
                                if (showEvents) DPCServiceEvents.Log.CleanUpEnterpriseResourceManager(profileName);
                                AccessRegistry.RemoveRegistryKeyValue(RegistrySettings.EnterpriseResourceManagerPath + "\\" + path + "\\" + userPath + "\\" + innerPath, vpnProfile.Key);
                                vpnCount--;
                            }
                        }

                        if (vpnCount > 0)
                        {
                            if (showEvents) DPCServiceEvents.Log.UpdateEnterpriseResourceManagerCount(vpnCount);
                            AccessRegistry.UpdateMachineData(RegistrySettings.EnterpriseResourceManagerPath + "\\" + path + "\\" + userPath + "\\" + innerPath, "count", vpnCount);
                        }
                        else
                        {
                            if (showEvents) DPCServiceEvents.Log.RemoveEnterpriseResourceManagerKey(userPath);
                            AccessRegistry.RemoveRegistryKey(RegistrySettings.EnterpriseResourceManagerPath + "\\" + path, userPath);
                        }
                    }
                }
            }

            //Root path is HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\NetworkList\Profiles\
            //There is a random GUID for each network adapter
            //Each network adapter should have a property named ProfileName which has the profile name (with spaces intact)
            IList<string> networkAdapters = AccessRegistry.ReadMachineSubkeys(RegistrySettings.NetworkListPath);
            foreach (string networkAdapter in networkAdapters)
            {
                Dictionary<string, string> adapterDetails = AccessRegistry.ReadMachineHashtable(networkAdapter, null, RegistrySettings.NetworkListPath);
                if (adapterDetails.ContainsKey("ProfileName") && adapterDetails["ProfileName"] == profileName)
                {
                    if (showEvents) DPCServiceEvents.Log.CleanUpNetworkProfile(profileName);
                    AccessRegistry.RemoveRegistryKey(RegistrySettings.NetworkListPath, networkAdapter);
                }
            }

            //Root path is HKLM:\System\CurrentControlSet\Services\RasMan\Config\
            //There is a registry value called AutoTriggerDisabledProfilesList
            //This contains a list (new line separated) of all profiles which have had the connect automatically tickbox unticked
            IList<string> profileList = AccessRegistry.ReadMachineStringArray(RegistrySettings.AutoTriggerDisabled, null, RegistrySettings.RasManConfig);
            if (profileList != null && profileList.Contains(profileName))
            {
                //Remove value and write back
                if (showEvents) DPCServiceEvents.Log.CleanUpAutoTriggerDisabledProfilesList(profileName);
                profileList.Remove(profileName);
                AccessRegistry.UpdateMachineData(RegistrySettings.RasManConfig, RegistrySettings.AutoTriggerDisabled, profileList);
            }

            //Cleanup any Managed Profiles
            string managedDeviceTunnelProfileName = AccessRegistry.ReadMachineString(AccessRegistry.GetProfileNameRegistryName(ProfileType.Machine), RegistrySettings.InternalStateOffset);
            if (managedDeviceTunnelProfileName == profileName)
            {
                AccessRegistry.RemoveRegistryKeyValue(RegistrySettings.InternalStateFullPath, AccessRegistry.GetProfileNameRegistryName(ProfileType.Machine));
            }

            string managedUserTunnelProfileName = AccessRegistry.ReadMachineString(AccessRegistry.GetProfileNameRegistryName(ProfileType.User), RegistrySettings.InternalStateOffset);
            if (managedUserTunnelProfileName == profileName)
            {
                AccessRegistry.RemoveRegistryKeyValue(RegistrySettings.InternalStateFullPath, AccessRegistry.GetProfileNameRegistryName(ProfileType.User));
            }

            string managedUserBackupTunnelProfileName = AccessRegistry.ReadMachineString(AccessRegistry.GetProfileNameRegistryName(ProfileType.UserBackup), RegistrySettings.InternalStateOffset);
            if (managedUserBackupTunnelProfileName == profileName)
            {
                AccessRegistry.RemoveRegistryKeyValue(RegistrySettings.InternalStateFullPath, AccessRegistry.GetProfileNameRegistryName(ProfileType.UserBackup));
            }

            //Remove Device Tunnel Registry Key as this blocks new device tunnels from being installed in Windows 11
            if (profileType == ProfileType.Machine)
            {
                AccessRegistry.RemoveRegistryKey(RegistrySettings.RasMan, RegistrySettings.DeviceTunnel);
;            }
        }

        public static void CreateProfile(ManagedProfile profile, CancellationToken cancelToken)
        {
            //Check to see if Profile is already accurate
            if (!string.IsNullOrWhiteSpace(profile.ProfileXML) && VPNProfile.CompareToInstalledProfile(profile.ProfileName, profile.ProfileXML, cancelToken)) //We don't need to check for accuracy if the profile is null as this is code for removal
            {
                //requested profile is already up to date, skip Profile rebuild but continue to process RAS Entry changes if required
                DPCServiceEvents.Log.ProfileAlreadyUpToDate(profile.ProfileName);
            }
            else
            {
                Dictionary<string, string> preUpdateResults = VPNProfile.CompareToInstalledProfileWithResults(profile.ProfileName, profile.ProfileXML, cancelToken);
                foreach (KeyValuePair<string, string> result in preUpdateResults)
                {
                    DPCServiceEvents.Log.ProfileDebugUpdateProfileDetail(profile.ProfileName, result.Key, result.Value);
                }

                //Profile not up to date, remove and re-add
                DPCServiceEvents.Log.ProfileDebugRemoveProfile(profile.ProfileName);
                //Remove existing profile if it exists
                RemoveProfileIfExists(profile, string.IsNullOrWhiteSpace(profile.ProfileXML)); //Write event if the profile is scheduled for deletion not just update

                //Run cleanup in case a previous profile was removed poorly
                Cleanup(profile.ProfileName, true, profile.ProfileType);

                //if value is null the profile should be removed only
                if (!string.IsNullOrWhiteSpace(profile.ProfileXML))
                {
                    //Profile has data so attempt to add it
                    DPCServiceEvents.Log.ProfileDebugAddProfile(profile.ProfileName);

                    //Actually request the profile creation
                    HandleProfileCreate(profile, cancelToken);

                    Thread.Sleep(1000); //Sometimes the compare will fail as the WMI/PS_Connection information hasn't quite had time to update yet

                    //Check that the profile was received correctly
                    if (!VPNProfile.CompareToInstalledProfile(profile.ProfileName, profile.ProfileXML, cancelToken))
                    {
                        //Warn that the profile has not been created accurately but do not remove
                        DPCServiceEvents.Log.WarningProfilesDoNotMatch(profile.ProfileName);
                        Dictionary<string, string> evalResults = VPNProfile.CompareToInstalledProfileWithResults(profile.ProfileName, profile.ProfileXML, cancelToken);
                        foreach (KeyValuePair<string, string> result in evalResults)
                        {
                            DPCServiceEvents.Log.WarningProfilesDoNotMatchDetail(profile.ProfileName, result.Key, result.Value);
                        }
                    }
                }
            }
            //Track in registry to enable easier reloading
            AccessRegistry.SaveMachineData(AccessRegistry.GetProfileNameRegistryName(profile.ProfileType), profile.ProfileName);
        }

        public static void HandleProfileCreate(ManagedProfile profile, CancellationToken cancelToken)
        {
            AccessWMI.NewProfile(profile.ProfileName, profile.ProfileXML, cancelToken);
        }

        public static void ManageRASEntryUpdates(ManagedProfile profile)
        {
            //Update VPN Metric
            try
            {
                if (AccessRasApi.SetVPNMetric(profile.ProfileName, profile.Metric))
                {
                    DPCServiceEvents.Log.ProfileMetricUpdated(profile.ProfileName);
                }
                else
                {
                    DPCServiceEvents.Log.ProfileMetricNotUpdated(profile.ProfileName);
                }
            }
            catch (Exception e)
            {
                DPCServiceEvents.Log.IssueUpdatingMetric(profile.ProfileName, e.Message, e.StackTrace);
            }

            //Update VPN Strategy
            try
            {
                if (AccessRasApi.SetVPNStrategy(profile.ProfileName, profile.VPNStrategy))
                {
                    DPCServiceEvents.Log.ProfileVPNStrategyUpdated(profile.ProfileName);
                }
                else
                {
                    DPCServiceEvents.Log.ProfileVPNStrategyNotUpdated(profile.ProfileName);
                }
            }
            catch (Exception e)
            {
                DPCServiceEvents.Log.IssueUpdatingVPNStrategy(profile.ProfileName, e.Message, e.StackTrace);
            }

            //Update Ras Credentials Flag
            try
            {
                if (AccessRasApi.SetDisableRasCredFlag(profile.ProfileName, profile.DisableRasCredentials))
                {
                    DPCServiceEvents.Log.ProfileUseRasCredentialsStatusUpdated(profile.ProfileName);
                }
                else
                {
                    DPCServiceEvents.Log.ProfileUseRasCredentialsStatusNotUpdated(profile.ProfileName);
                }
            }
            catch (Exception e)
            {
                DPCServiceEvents.Log.IssueUpdatingUseRasCredentialsStatus(profile.ProfileName, e.Message, e.StackTrace);
            }

            //Update Network Outage Time
            try
            {
                if (AccessRasApi.SetNetworkOutageTime(profile.ProfileName, profile.NetworkOutageTime))
                {
                    DPCServiceEvents.Log.ProfileNetworkOutageUpdated(profile.ProfileName);
                }
                else
                {
                    DPCServiceEvents.Log.ProfileNetworkOutageTimeNotUpdated(profile.ProfileName);
                }
            }
            catch (Exception e)
            {
                DPCServiceEvents.Log.IssueUpdatingNetworkOutageTime(profile.ProfileName, e.Message, e.StackTrace);
            }
        }

        public static void ManageNetIOUpdates(ManagedProfile profile)
        {
            //Update Profile MTU
            try
            {
                uint? oldMTUIPv4 = AccessWMI.GetInterfaceMTU(profile.ProfileName, IPAddressFamily.IPv4);
                uint updateMTU = profile.MTU;
                if (updateMTU < 576)
                {
                    updateMTU = 576; //IPv4 standard has a minimum MTU of 576
                }
                if (oldMTUIPv4 == null)
                {
                    DPCServiceEvents.Log.ProfileMTUIsNull(profile.ProfileName, "IPv4");
                }
                else if (oldMTUIPv4 != profile.MTU)
                {
                    AccessNetSh.SetPersistentMTU(profile.ProfileName, IPAddressFamily.IPv4, updateMTU);
                    DPCServiceEvents.Log.ProfileMTUUpdated(profile.ProfileName, "IPv4", (uint)oldMTUIPv4, updateMTU);
                }
                else
                {
                    DPCServiceEvents.Log.ProfileMTUNotUpdated(profile.ProfileName, "IPv4");
                }
            }
            catch (Exception e)
            {
                DPCServiceEvents.Log.IssueUpdatingProfileMTU(profile.ProfileName, "IPv4", e.Message, e.StackTrace);
            }

            try
            {
                uint? oldMTUIPv6 = AccessWMI.GetInterfaceMTU(profile.ProfileName, IPAddressFamily.IPv6);
                uint updateMTU = profile.MTU;
                if (updateMTU < 1280)
                {
                    updateMTU = 1280; //IPv6 standard has a minimum MTU of 1280 compared to the IPv4 minimum of 576
                }
                if (oldMTUIPv6 == null)
                {
                    DPCServiceEvents.Log.ProfileMTUIsNull(profile.ProfileName, "IPv6");
                }
                else if (oldMTUIPv6 != updateMTU)
                {
                    AccessNetSh.SetPersistentMTU(profile.ProfileName, IPAddressFamily.IPv6, updateMTU);
                    DPCServiceEvents.Log.ProfileMTUUpdated(profile.ProfileName, "IPv6", (uint)oldMTUIPv6, updateMTU);
                }
                else
                {
                    DPCServiceEvents.Log.ProfileMTUNotUpdated(profile.ProfileName, "IPv6");
                }
            }
            catch (Exception e)
            {
                DPCServiceEvents.Log.IssueUpdatingProfileMTU(profile.ProfileName, "IPv6", e.Message, e.StackTrace);
            }
        }

        public static void ManageWMIEntryUpdates(ManagedProfile profile, CancellationToken cancelToken)
        {
            if (profile.ProfileType == ProfileType.Machine)
            {
                //Update Machine EKU Filter
                try
                {
                    if (AccessWMI.SetMachineCertificateEKUFilter(profile.ProfileName, profile.MachineEKU, cancelToken))
                    {
                        DPCServiceEvents.Log.ProfileMachineEKUUpdated(profile.ProfileName);
                    }
                    else
                    {
                        DPCServiceEvents.Log.ProfileMachineEKUNotUpdated(profile.ProfileName);
                    }
                }
                catch (Exception e)
                {
                    DPCServiceEvents.Log.IssueUpdatingMachineEKU(profile.ProfileName, e.Message, e.StackTrace);
                }
            }

            //Update Proxy Exclusions List and Bypass for Local Exceptions
            try
            {
                if (AccessWMI.SetProxyExcludeExceptions(profile.ProfileName, profile.ProxyExcludeList, profile.ProxyBypassForLocal, cancelToken))
                {
                    DPCServiceEvents.Log.ProfileProxyExclusionsUpdated(profile.ProfileName);
                }
                else
                {
                    DPCServiceEvents.Log.ProfileProxyExclusionsNotUpdated(profile.ProfileName);
                }
            }
            catch (Exception e)
            {
                DPCServiceEvents.Log.IssueUpdatingProxyExclusions(profile.ProfileName, e.Message, e.StackTrace);
            }
        }

        public static bool ManageAutoTriggerSettings(ManagedProfile profile, CancellationToken cancelToken)
        {
            if (!AccessRegistry.ReadMachineBoolean(RegistrySettings.ClearDisableProfileList, false))
            {
                //If the setting to enable automatic starting is disabled then don't attempt to re-enable the profile
                return false;
            }
            bool updateResult = false;
            ProfileInfo PBKProfile = ManageRasphonePBK.ListProfiles(profile.ProfileName).FirstOrDefault();
            if (PBKProfile == null)
            {
                DPCServiceEvents.Log.UnableToLocatePBKProfile(profile.ProfileName);
                return false;
            }

            try
            {
                if (profile.ProfileType == ProfileType.User)
                {
                    //When running tests locally the SID may be the user running the tests, if this is the case don't try to update to the Machine Account
                    if (AccessRegistry.ReadMachineString(RegistrySettings.UserSID, RegistrySettings.Config, RegistrySettings.RasMan) != DeviceInfo.CurrentUserSID())
                    {
                        updateResult |= AccessRegistry.SaveMachineData(RegistrySettings.UserSID, "S-1-1-0", RegistrySettings.RasMan, RegistrySettings.Config);
                    }
                    updateResult |= AccessRegistry.SaveMachineData(RegistrySettings.AutoTriggerProfileName, profile.ProfileName, RegistrySettings.RasMan, RegistrySettings.Config);
                    updateResult |= AccessRegistry.SaveMachineData(RegistrySettings.AutoTriggerPBKPath, PBKProfile.PBKPath, RegistrySettings.RasMan, RegistrySettings.Config);
                    Guid profileGuid = AccessWMI.GetWMIVPNGuid(profile.ProfileName, cancelToken);
                    updateResult |= AccessRegistry.SaveMachineDataAsBinary(RegistrySettings.AutoTriggerProfileGUID, profileGuid, RegistrySettings.RasMan, RegistrySettings.Config);
                }
                else if (profile.ProfileType == ProfileType.Machine)
                {
                    updateResult |= AccessRegistry.SaveMachineData(RegistrySettings.UserSID, "S-1-5-80", RegistrySettings.RasMan, RegistrySettings.DeviceTunnel);
                    updateResult |= AccessRegistry.SaveMachineData(RegistrySettings.AutoTriggerProfileName, profile.ProfileName, RegistrySettings.RasMan, RegistrySettings.DeviceTunnel);
                    updateResult |= AccessRegistry.SaveMachineData(RegistrySettings.AutoTriggerPBKPath, PBKProfile.PBKPath, RegistrySettings.RasMan, RegistrySettings.DeviceTunnel);
                    Guid profileGuid = AccessWMI.GetWMIVPNGuid(profile.ProfileName, cancelToken);
                    updateResult |= AccessRegistry.SaveMachineDataAsBinary(RegistrySettings.AutoTriggerProfileGUID, profileGuid, RegistrySettings.RasMan, RegistrySettings.DeviceTunnel);
                }
                //Do Not set the backup tunnel to auto trigger but do remove it from the disabled triggers list if required
                updateResult |= AccessRegistry.ClearRegistryKeyValue(RegistrySettings.RasManConfig, RegistrySettings.AutoTriggerDisabled);
            }
            catch (Exception e)
            {
                DPCServiceEvents.Log.IssueUpdatingAutoTriggerRegistry(profile.ProfileName, e.Message, e.StackTrace);
            }
            return updateResult;
        }

        public static ManagedProfile GetProfile(string profileName, ProfileType profileType, CancellationToken cancelToken)
        {

            ManagedProfile newProfile = new ManagedProfile
            {
                ProfileName = profileName,
                ProfileType = profileType,
                ProfileObj = new WMIProfile(ManageRasphonePBK.ListProfiles(profileName, DeviceInfo.CurrentUserSID()), cancelToken), //returns null if not found
            };

            //If unable to find profile, skip trying to get win32 details as it creates unnecessary errors
            if (!VPNProfile.IsDefaultProfile(newProfile.ProfileObj))
            {
                if (profileType == ProfileType.Machine && !newProfile.ProfileObj.DeviceTunnel)
                {
                    //Machine retrieved User Tunnel
                    newProfile.ProfileType = ProfileType.User;
                }

                try
                {
                    newProfile.VPNStrategy = AccessRasApi.GetVPNStrategy(profileName);
                }
                catch (Exception e)
                {
                    DPCServiceEvents.Log.ErrorGettingVPNStrategy(profileName, e.Message, e.StackTrace);
                    newProfile.VPNStrategy = VPNStrategy.Ikev2Only;
                }

                try
                {
                    newProfile.Metric = AccessRasApi.GetVPNIPv4Metric(profileName);
                    if (AccessRasApi.GetVPNIPv6Metric(profileName) != newProfile.Metric)
                    {
                        DPCServiceEvents.Log.WarningIPv4AndIPv6MetricsDoNotMatch(profileName);
                    }
                }
                catch (Exception e)
                {
                    DPCServiceEvents.Log.ErrorGettingVPNMetric(profileName, e.Message, e.StackTrace);
                }

                try
                {
                    newProfile.DisableRasCredentials = AccessRasApi.GetDisableRasCredStatus(profileName);
                }
                catch (Exception e)
                {
                    DPCServiceEvents.Log.ErrorGettingDisableRasCredentials(profileName, e.Message, e.StackTrace);
                }

                try
                {
                    newProfile.NetworkOutageTime = AccessRasApi.GetNetworkOutageTime(profileName);
                }
                catch (Exception e)
                {
                    DPCServiceEvents.Log.ErrorGettingNetworkOutageTime(profileName, e.Message, e.StackTrace);
                }

                try
                {
                    uint? mtu = AccessWMI.GetInterfaceMTU(profileName, IPAddressFamily.IPv4);
                    if (mtu == null)
                    {
                        newProfile.MTU = 1400; //default MTU size
                    }
                    else
                    {
                        newProfile.MTU = (uint)mtu;
                    }

                }
                catch (Exception e)
                {
                    DPCServiceEvents.Log.ErrorGettingNetworkOutageTime(profileName, e.Message, e.StackTrace);
                }

                try
                {
                    newProfile.ProxyExcludeList = AccessWMI.GetProxyExcludeList(profileName, cancelToken);
                }
                catch (Exception e)
                {
                    DPCServiceEvents.Log.ErrorGettingProxyExclusions(profileName, e.Message, e.StackTrace);
                }
            }

            return newProfile;
        }

        public static void RemoveHiddenProfile(ManagedProfile profile)
        {
            IList<Exception> errorList = ManageRasphonePBK.RemoveHiddenProfile(profile.ProfileName);
            if (errorList.Count <= 0)
            {
                DPCServiceEvents.Log.ProfileHiddenProfileRemoved(profile.ProfileName);
            }
            else if (errorList[0] is NoOperationException)
            {
                DPCServiceEvents.Log.ProfileDebugProfileRemoveNotRequired(profile.ProfileName);
            }
            else
            {
                foreach (Exception e in errorList)
                {
                    if (e is FileDeleteException)
                    {
                        DPCServiceEvents.Log.IssueDeletingHiddenPbk(e.Message, e.InnerException.Message, e.InnerException.StackTrace);
                    }
                    else
                    {
                        DPCServiceEvents.Log.IssueDeletingHiddenPbk(profile.ProfileName, e.Message, e.StackTrace);
                    }
                }
            }
        }
    }
}
