using DPCLibrary.Enums;
using DPCLibrary.Models;
using DPCLibrary.Utils;
using DPCService.Models;
using DPCService.Utils;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer; //Timer is also available through System.Threading which is needed for the Cancellation token by System.Timers is a better timing class

namespace DPCService.Core
{
    internal class ProfileManager
    {
        private readonly SharedData SharedData;
        private CancellationTokenSource ProfileCancelToken = new CancellationTokenSource();
        private readonly CancellationToken RootToken;
        private static readonly Dictionary<string, Task> ProfileList = new Dictionary<string, Task>();
        private Timer UpdateTimer = null;

        // This is the synchronization point that prevents events
        // from running concurrently, and prevents the main thread
        // from executing code after the Stop method until any
        // event handlers are done executing.
        private static int UpdateMonitorSyncPoint = 0;
        private static int RemoveProfilesSyncPoint = 0;
        private static int DeviceTunnelUISyncPoint = 0;

        public ProfileManager(SharedData sharedData, CancellationToken token)
        {
            SharedData = sharedData;
            token.Register(ShutdownProfileMonitors);
            RootToken = token;
        }

        public void ManagerStartup()
        {
            try
            {
                if (RootToken.IsCancellationRequested)
                {
                    //Program is trying to shutdown, it should not be creating new threads
                    return;
                }

                if (ProfileCancelToken.IsCancellationRequested)
                {
                    ProfileCancelToken.Dispose();
                    ProfileCancelToken = new CancellationTokenSource();
                }

                if (AccessRegistry.ReadMachineBoolean(RegistrySettings.UserTunnel, false))
                {
                    DPCServiceEvents.Log.StartProfileMonitor("User");
                    ProfileList.Add(RegistrySettings.UserTunnel, new TaskFactory().StartNew(() => new ProfileMonitor(SharedData, ProfileType.User, ProfileCancelToken.Token)));
                }
                else
                {
                    //If the profile isn't being activated, check for existing profile and remove
                    string oldProfileName = AccessRegistry.ReadMachineString(AccessRegistry.GetProfileNameRegistryName(ProfileType.User), RegistrySettings.InternalStateOffset);
                    if (oldProfileName != null)
                    {
                        DPCServiceEvents.Log.ProfileNoLongerNeeded(oldProfileName);
                        SharedData.RemoveProfile(oldProfileName);
                    }
                }

                if (AccessRegistry.ReadMachineBoolean(RegistrySettings.UserBackupTunnel, false))
                {
                    DPCServiceEvents.Log.StartProfileMonitor("UserBackup");
                    ProfileList.Add(RegistrySettings.UserBackupTunnel, new TaskFactory().StartNew(() => new ProfileMonitor(SharedData, ProfileType.UserBackup, ProfileCancelToken.Token)));
                }
                else
                {
                    //If the profile isn't being activated, check for existing profile and remove
                    string oldProfileName = AccessRegistry.ReadMachineString(AccessRegistry.GetProfileNameRegistryName(ProfileType.UserBackup), RegistrySettings.InternalStateOffset);
                    if (oldProfileName != null)
                    {
                        DPCServiceEvents.Log.ProfileNoLongerNeeded(oldProfileName);
                        SharedData.RemoveProfile(oldProfileName);
                    }
                }

                if (AccessRegistry.ReadMachineBoolean(RegistrySettings.MachineTunnel, false))
                {
                    DPCServiceEvents.Log.StartProfileMonitor("Machine");
                    ProfileList.Add(RegistrySettings.MachineTunnel, new TaskFactory().StartNew(() => new ProfileMonitor(SharedData, ProfileType.Machine, ProfileCancelToken.Token)));
                }
                else
                {
                    //If the profile isn't being activated, check for existing profile and remove
                    string oldProfileName = AccessRegistry.ReadMachineString(AccessRegistry.GetProfileNameRegistryName(ProfileType.Machine), RegistrySettings.InternalStateOffset);
                    if (oldProfileName != null)
                    {
                        DPCServiceEvents.Log.ProfileNoLongerNeeded(oldProfileName);
                        SharedData.RemoveProfile(oldProfileName);
                    }
                }

                bool removeExistingProfiles = AccessRegistry.ReadMachineBoolean(RegistrySettings.RemoveAllProfiles, false);

                bool updateMTU = AccessRegistry.ReadMachineUInt32(RegistrySettings.MTU, 1400) != 1400; //Check to see if the MTU setting has been configured to be non-default

                if (UpdateTimer == null)
                {
                    UpdateTimer = new Timer();
                }

                //Setup regular check for updated Monitor
                UpdateTimer.Elapsed += new ElapsedEventHandler(UpdateManagerStatus);
                //Setup regular check to see if Device Tunnel UI needs to be enabled/disabled
                UpdateTimer.Elapsed += new ElapsedEventHandler(ShowDeviceTunnelUI);

                if (removeExistingProfiles)
                {
                    DPCServiceEvents.Log.StartingUnmanagedProfileRemovalService();
                    UpdateTimer.Elapsed += new ElapsedEventHandler(ClearUnmanagedProfiles);
                }
                else if (updateMTU)
                {
                    //Update MTU is enabled but removing Existing Profiles has been disabled. This raises the potential for other VPN connections to be impacted by the MTU change
                    DPCServiceEvents.Log.ExistingProfileMTUImpact();
                }

                UpdateTimer.AutoReset = true;
                UpdateTimer.Interval = SharedData.GetUpdateTime();
                UpdateTimer.Start(); //start the clock

                //Attempt to process any profile changes such as the remove of old profiles
                SharedData.HandleProfileUpdates();

                //Run initial clear of profiles if enabled at startup
                if (removeExistingProfiles)
                {
                    ClearUnmanagedProfiles(null, null);
                }

                //Always run these as part of startup/reset
                DisableVPNStrategyUpdate();
                ShowDeviceTunnelUI(null, null);
            }
            catch (Exception e)
            {
                DPCServiceEvents.Log.MonitorStartFailed(e.Message, e.StackTrace);
            }
        }

        private void UpdateManagerStatus(object sender, ElapsedEventArgs args)
        {
            DPCServiceEvents.Log.TraceMethodStart("UpdateManagerStatus", "ProfileManager");
            //Skip execution if another instance of this method is already running
            if (Interlocked.CompareExchange(ref UpdateMonitorSyncPoint, 1, 0) == 0)
            {
                try
                {
                    DPCServiceEvents.Log.ProfileManagerCheckForUpdates();
                    bool updateProfiles = false;
                    bool newUserMonitor = AccessRegistry.ReadMachineBoolean(RegistrySettings.UserTunnel, false);
                    bool newUserBackupMonitor = AccessRegistry.ReadMachineBoolean(RegistrySettings.UserBackupTunnel, false);
                    bool newDeviceMonitor = AccessRegistry.ReadMachineBoolean(RegistrySettings.MachineTunnel, false);
                    if (newUserMonitor != ProfileList.ContainsKey(RegistrySettings.UserTunnel))
                    {
                        DPCServiceEvents.Log.MonitorProfileChangeRequested(RegistrySettings.UserTunnel, ProfileList.ContainsKey(RegistrySettings.UserTunnel), newUserMonitor);
                        updateProfiles = true;
                    }

                    if (newUserBackupMonitor != ProfileList.ContainsKey(RegistrySettings.UserBackupTunnel))
                    {
                        DPCServiceEvents.Log.MonitorProfileChangeRequested(RegistrySettings.UserBackupTunnel, ProfileList.ContainsKey(RegistrySettings.UserBackupTunnel), newUserBackupMonitor);
                        updateProfiles = true;
                    }

                    if (newDeviceMonitor != ProfileList.ContainsKey(RegistrySettings.MachineTunnel))
                    {
                        DPCServiceEvents.Log.MonitorProfileChangeRequested(RegistrySettings.MachineTunnel, ProfileList.ContainsKey(RegistrySettings.MachineTunnel), newDeviceMonitor);
                        updateProfiles = true;
                    }

                    bool restart = false;
                    if (updateProfiles)
                    {
                        DPCServiceEvents.Log.ProfileMonitorChanged();
                        restart = true;
                    }
                    else
                    {
                        //Check on the status of the service
                        foreach (KeyValuePair<string, Task> t in ProfileList)
                        {
                            if (t.Value.Status == TaskStatus.Faulted)
                            {
                                DPCServiceEvents.Log.MonitorProfileFailed(t.Key, t.Value.Exception.Message, t.Value.Exception.StackTrace);
                                restart = true;
                            }
                            else if (t.Value.Status == TaskStatus.RanToCompletion)
                            {
                                DPCServiceEvents.Log.MonitorProfileCompleted(t.Key);
                                restart = true;
                            }
                        }
                    }

                    if (restart)
                    {
                        UpdateTimer.Stop();
                        // Release control of SyncPoint early otherwise deadlock will occur in TokenCancelled.
                        UpdateMonitorSyncPoint = 0;
                        ShutdownProfileMonitors(); //Cancel existing profiles
                        ManagerStartup(); //Restart profile updates
                    }
                }
                catch (Exception e)
                {
                    DPCServiceEvents.Log.UpdateMonitorStatusFailed(e.Message, e.StackTrace);
                }
                finally
                {
                    // Release control of SyncPoint
                    UpdateMonitorSyncPoint = 0;
                }
            }
            else
            {
                DPCServiceEvents.Log.ProfileManagerUpdateSkipped();
            }
            DPCServiceEvents.Log.TraceMethodStop("UpdateManagerStatus", "ProfileManager");
        }

        private static void DisableVPNStrategyUpdate()
        {
            try
            {
                DPCServiceEvents.Log.ProfileManagerDisableVPNStrategyOverwrite();
                AccessRegistry.SaveMachineData(RegistrySettings.VPNStrategyUsageDisabled, 1, RegistrySettings.RasManParameters, null);
            }
            catch (Exception e)
            {
                DPCServiceEvents.Log.UpdateToDisableVPNStrategyOverwriteFailed(e.Message, e.StackTrace);
            }
        }

        private void ShowDeviceTunnelUI(object sender, ElapsedEventArgs args)
        {
            DPCServiceEvents.Log.TraceMethodStart("ShowDeviceTunnelUI", "ProfileManager");
            //Skip execution if another instance of this method is already running
            if (Interlocked.CompareExchange(ref DeviceTunnelUISyncPoint, 1, 0) == 0)
            {
                try
                {
                    bool showDeviceTunnelUI = AccessRegistry.ReadMachineBoolean(RegistrySettings.ShowDeviceTunnelUI, false);
                    bool currentDeviceTunnelUIState = AccessRegistry.ReadMachineBoolean(RegistrySettings.ShowDeviceTunnelUI, RegistrySettings.VPNUI, false);
                    bool deviceTunnelEnabledStatus = AccessRegistry.ReadMachineBoolean(RegistrySettings.MachineTunnel, false);

                    //Only modify the device tunnel UI state if it is different AND DPC is currently the owner of the device tunnel
                    //This avoids 'install first' issues where this is previously set manually but a config has not yet been applied
                    //to DPC which then resets the key back to false
                    if (currentDeviceTunnelUIState != showDeviceTunnelUI && deviceTunnelEnabledStatus)
                    {
                        AccessRegistry.SaveMachineData(RegistrySettings.ShowDeviceTunnelUI, showDeviceTunnelUI, RegistrySettings.VPNUI, null);
                        DPCServiceEvents.Log.DeviceTunnelUIUpdated(showDeviceTunnelUI);
                    }
                    else
                    {
                        DPCServiceEvents.Log.DeviceTunnelUIAlreadyCorrect(showDeviceTunnelUI);
                    }
                }
                catch (Exception e)
                {
                    DPCServiceEvents.Log.UnableToUpdateDeviceTunnelUI(e.Message, e.StackTrace);
                }
                finally
                {
                    // Release control of SyncPoint.
                    DeviceTunnelUISyncPoint = 0;
                }
            }
            else
            {
                DPCServiceEvents.Log.ProfileManagerDeviceTunnelUIUpdateSkipped();
            }
            DPCServiceEvents.Log.TraceMethodStop("ShowDeviceTunnelUI", "ProfileManager");
        }

        private void ClearUnmanagedProfiles(object sender, ElapsedEventArgs args)
        {
            DPCServiceEvents.Log.TraceMethodStart("ClearUnmanagedProfiles", "ProfileManager");
            //Skip execution if another instance of this method is already running
            if (Interlocked.CompareExchange(ref RemoveProfilesSyncPoint, 1, 0) == 0)
            {
                try
                {
                    DPCServiceEvents.Log.RemovingAllUnmanagedProfiles();
                    IList<string> managedProfileList = SharedData.GetManagedProfileList();

                    List<ProfileInfo> allProfileList = ManageRasphonePBK.ListAllProfiles(false);

                    if (managedProfileList.Count < 1 && allProfileList.Count > 0)
                    {
                        DPCServiceEvents.Log.ProfileRemovalBlocked(allProfileList.Count);
                        return;
                    }

                    //Remove profiles managed by this solution and are currently managed by the system
                    foreach (string profile in managedProfileList)
                    {
                        allProfileList.RemoveAll(p => p.ProfileName == profile && p.Sid == DeviceInfo.SYSTEMSID);
                    }

                    if (allProfileList.Count < 1)
                    {
                        DPCServiceEvents.Log.NoProfilesToRemove();
                    }
                    else
                    {
                        foreach (ProfileInfo profile in allProfileList)
                        {
                            DPCServiceEvents.Log.RemovingProfile(profile.ProfileName);
                            SharedData.RemoveProfile(profile.ProfileName);
                        }
                    }
                }
                catch (Exception e)
                {
                    DPCServiceEvents.Log.UpdateToRemoveProfile(e.Message, e.StackTrace);
                }
                finally
                {
                    // Release control of SyncPoint.
                    RemoveProfilesSyncPoint = 0;
                }
            }
            else
            {
                DPCServiceEvents.Log.ProfileManagerRemoveSkipped();
            }
            DPCServiceEvents.Log.TraceMethodStop("ClearSystemProfiles", "ProfileManager");
        }

        private void ShutdownProfileMonitors()
        {
            try
            {
                DPCServiceEvents.Log.StoppingProfileMonitor();
                if (UpdateTimer != null)
                {
                    UpdateTimer.Stop();

                    // Ensure that if an event is currently executing,
                    // no further processing is done on this thread until
                    // the event handler is finished. This is accomplished
                    // by using CompareExchange to place -1 in syncPoint,
                    // but only if syncPoint is currently zero (specified
                    // by the third parameter of CompareExchange).
                    // CompareExchange returns the original value that was
                    // in syncPoint. If it was not zero, then there's an
                    // event handler running, and it is necessary to try
                    // again.
                    while (Interlocked.CompareExchange(ref UpdateMonitorSyncPoint, -1, 0) != 0)
                    {
                        DPCServiceEvents.Log.ProfileManagerWaitForProfileUpdate();
                        Thread.Sleep(10);
                    }

                    while (Interlocked.CompareExchange(ref RemoveProfilesSyncPoint, -1, 0) != 0)
                    {
                        DPCServiceEvents.Log.ProfileManagerWaitForProfileRemovalWMI();
                        Thread.Sleep(10);
                    }

                    UpdateTimer.Close();
                    UpdateTimer.Dispose();
                    UpdateTimer = null;
                }
                //Cancel all subThreads
                ProfileCancelToken.Cancel();

                //Wait for all child threads to stop
                foreach (KeyValuePair<string, Task> t in ProfileList)
                {
                    try
                    {
                        t.Value.Wait();
                    }
                    catch (Exception e)
                    {
                        DPCServiceEvents.Log.ProfileCreationFailed(t.Key, e.Message, e.StackTrace);
                    }
                }
                ProfileList.Clear();
                DPCServiceEvents.Log.VPNProfileMonitorStopped();
            }
            catch (Exception e)
            {
                DPCServiceEvents.Log.MonitorStopFailed(e.Message, e.StackTrace);
            }
            finally
            {
                // Release control of SyncPoints.
                UpdateMonitorSyncPoint = 0;
                RemoveProfilesSyncPoint = 0;
                DeviceTunnelUISyncPoint = 0;
            }
        }
    }
}