using DPCLibrary.Enums;
using DPCLibrary.Models;
using DPCLibrary.Utils;
using DPCService.Models;
using DPCService.Utils;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer; //Timer is also available through System.Threading which is needed for the Cancellation token by System.Timers is a better timing class

namespace DPCService.Core
{
    public class ProfileMonitor
    {
        private readonly SharedData SharedData;
        private readonly ProfileType ProfileType;
        private readonly VPNProfileCreator profile;
        private readonly Timer UpdateTimer = new Timer();
        private string ProfileName;
        private string LogProfileName; //Profile name to use when logging, needed for when there isn't a valid profile name set or the profile name is in the middle of changing
        private string OldProfileName;
        private CancellationToken CancelToken;
        private Task GPUpdateNotification;

        // This is the synchronization point that prevents events
        // from running concurrently, and prevents the main thread
        // from executing code after the Stop method until any
        // event handlers are done executing.

        //make static to limit one profile update at a time
        private int SyncPoint = 0;

        public ProfileMonitor(SharedData sharedData, ProfileType profileType, CancellationToken token)
        {
            try
            {
                SharedData = sharedData;
                ProfileType = profileType;
                CancelToken = token;

                LogProfileName = DefaultProfileName();

                profile = new VPNProfileCreator(ProfileType, false);

                token.Register(TokenCancelled);

                //Load current profile name from registry to handle dropout scenarios
                ProfileName = AccessRegistry.ReadMachineString(AccessRegistry.GetProfileNameRegistryName(ProfileType), RegistrySettings.InternalStateOffset);
                if (!string.IsNullOrWhiteSpace(ProfileName))
                {
                    LogProfileName = ProfileName;
                }

                //Setup regular check for updated Monitor
                UpdateTimer.Elapsed += new ElapsedEventHandler(CheckProfile);
                UpdateTimer.AutoReset = true;
                UpdateTimer.Interval = SharedData.GetUpdateTime();

                CheckProfile(null, null); //Force an initial start
                RegisterForGPUpdateNotification();
                UpdateTimer.Start();

                //Wait for cancellation token to complete before returning thread, this ensures that errors are captured in the thread collection logic
                token.WaitHandle.WaitOne();
            }
            catch (Exception e)
            {
                DPCServiceEvents.Log.MonitorProfileStartupFailed(e.Message, e.StackTrace);
            }
        }

        private void CheckProfile(object sender, ElapsedEventArgs args)
        {
            DPCServiceEvents.Log.TraceMethodStart("CheckProfile", LogProfileName);
            //Skip execution if another instance of this method is already running
            if (Interlocked.CompareExchange(ref SyncPoint, 1, 0) == 0)
            {
                DPCServiceEvents.Log.ProfileUpdateStartedDebug(LogProfileName);
                DPCServiceEvents.Log.ProfileUpdateStarted(LogProfileName);
                try
                {
                    profile.LoadFromRegistry(); //Reload settings from registry to check for any Group Policy Updates
                    profile.Generate();
                    string savePath = null;
                    try
                    {
                        LoadRegistryVariable(ref savePath, RegistrySettings.SavePath);
                        if (!string.IsNullOrWhiteSpace(savePath))
                        {
                            profile.SaveProfile(savePath);
                        }
                    }
                    catch (Exception e)
                    {
                        DPCServiceEvents.Log.UpdateToSaveProfileToDisk(LogProfileName, e.Message, savePath);
                    }

                    bool newName = UpdateProfileName();

                    bool genFailed = profile.ValidateFailed();

                    if (genFailed)
                    {
                        DPCServiceEvents.Log.ProfileGenerationErrors(LogProfileName, profile.GetValidationFailures());
                    }

                    if (profile.ValidateWarnings())
                    {
                        DPCServiceEvents.Log.ProfileGenerationWarnings(LogProfileName, profile.GetValidationWarnings());
                    }

                    if (!genFailed)
                    {
                        //Build Succeeded and new profile name is selected so schedule old profile removal unless there was no profile previously
                        ManagedProfile newProfile = profile.GetProfileUpdate();
                        if (newName && !string.IsNullOrWhiteSpace(OldProfileName))
                        {
                            //Delete old profile so new profile can take its place
                            SharedData.RemoveProfile(OldProfileName);
                            newProfile.OldProfileName = OldProfileName; //Track old profile name so that we can ensure that the old profile is deleted before adding the new one in
                        }

                        SharedData.AddProfileUpdate(newProfile); //Duplication handled in Add method
                        DPCServiceEvents.Log.ProfileUpdateGenerated(LogProfileName);
                    }

                    //Attempt to process profile updates
                    SharedData.HandleProfileUpdates();

                    string debugPath = null;
                    try
                    {
                        LoadRegistryVariable(ref debugPath, RegistrySettings.DebugPath);
                        if (!string.IsNullOrWhiteSpace(debugPath))
                        {
                            if (savePath == debugPath)
                            {
                                DPCServiceEvents.Log.ProfileSavePathIdentical(LogProfileName);
                            }
                            SaveWMIProfile(debugPath, ProfileName, LogProfileName, CancelToken);
                        }
                    }
                    catch (Exception e)
                    {
                        DPCServiceEvents.Log.UpdateToSaveActualProfileToDiskException(LogProfileName, e.Message, debugPath, e.StackTrace);
                    }
                }
                catch (Exception e)
                {
                    if (SharedData.DumpOnException)
                    {
                        MiniDump.Write();
                    }

                    DPCServiceEvents.Log.ProfileCreationFailed(ProfileName, e.Message, e.StackTrace);
                }
                finally
                {
                    // Release control of SyncPoint.
                    SyncPoint = 0;
                }
            }
            else
            {
                DPCServiceEvents.Log.ProfileUpdateSkipped(LogProfileName);
            }
            DPCServiceEvents.Log.TraceMethodStop("CheckProfile", LogProfileName);
        }

        private void ProcessGPUpdateNotification()
        {
            DPCServiceEvents.Log.GroupPolicyUpdated();
            CheckProfile(null, null); //Force an initial start
            //Reset the timer
            UpdateTimer.Stop();
            UpdateTimer.Start();
        }

        private void RegisterForGPUpdateNotification()
        {
            DPCServiceEvents.Log.StartGPUpdateMonitoring(LogProfileName);
            try
            {
                GPUpdateNotification = new TaskFactory().StartNew(() => AccessUserEnv.StartGPUpdateNotification(CancelToken, ProcessGPUpdateNotification));
            }
            catch (Exception e)
            {
                DPCServiceEvents.Log.MonitorGPUpdateErrorOnStartup(e.Message, e.StackTrace);
            }
        }

        public static string SaveWMIProfile(string debugPath, string profileName, string logProfileName, CancellationToken cancelToken)
        {
            string installedProfileExport;
            if (DeviceInfo.GetOSVersion().WMIWorking)
            {
                try
                {
                    //There may be times where even when WMI is working this call may fail so fall back to RAS approach
                    //Where there is an issue
                    installedProfileExport = AccessWMI.GetProfileData(profileName, cancelToken);
                }
                catch
                {
                    ProfileInfo profileInfo = ManageRasphonePBK.ListProfiles(profileName, DeviceInfo.CurrentUserSID());
                    if (profileInfo != null)
                    {
                        installedProfileExport = new WMIProfile(profileInfo, cancelToken).ToString();
                    }
                    else
                    {
                        installedProfileExport = "";
                    }
                }
            }
            else
            {
                ProfileInfo profileInfo = ManageRasphonePBK.ListProfiles(profileName, DeviceInfo.CurrentUserSID());
                if (profileInfo != null)
                {
                    installedProfileExport = new WMIProfile(profileInfo, cancelToken).ToString();
                }
                else
                {
                    installedProfileExport = "";
                }
            }

            if (!string.IsNullOrWhiteSpace(installedProfileExport))
            {
                return VPNProfileCreator.SaveProfile(profileName, installedProfileExport, debugPath);
            }
            else
            {
                DPCServiceEvents.Log.UpdateToSaveActualProfileToDisk(logProfileName, "Profile has not been installed yet", debugPath);
            }
            return null;
        }

        private void TokenCancelled()
        {
            try
            {
                DPCServiceEvents.Log.ProfileUpdateShutdownRequested(LogProfileName);
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
                while (Interlocked.CompareExchange(ref SyncPoint, -1, 0) != 0)
                {
                    DPCServiceEvents.Log.ProfileShutdownRequested(LogProfileName);
                    Thread.Sleep(10);
                }
                GPUpdateNotification.Wait();
                DPCServiceEvents.Log.ProfileUpdateShutdownComplete(LogProfileName);
            }
            catch (Exception e)
            {
                DPCServiceEvents.Log.MonitorProfileShutdownFailed(e.Message, e.StackTrace);
            }
            finally
            {
                SyncPoint = 0;
            }
        }

        private bool UpdateProfileName()
        {
            string newProfileName = profile.GetProfileName();

            if (newProfileName == null)
            {
                LogProfileName = DefaultProfileName();
                return false;
            }

            if (ProfileName != newProfileName)
            {
                LogProfileName = newProfileName;
                OldProfileName = ProfileName;
                ProfileName = newProfileName;
                return true;
            }
            else
            {
                OldProfileName = null; //OldProfile should have been handled by now
                return false;
            }
        }

        private string DefaultProfileName()
        {
            switch (ProfileType)
            {
                case ProfileType.Machine:
                    return "Machine Profile";

                case ProfileType.User:
                    return "User Profile";

                case ProfileType.UserBackup:
                    return "Backup User Profile";

                default:
                    return "Unknown Profile";
            }
        }

        private void LoadRegistryVariable(ref string var, string registryValue)
        {
            var = null;

            try
            {
                var = AccessRegistry.ReadMachineString(registryValue, RegistrySettings.GetProfileOffset(ProfileType));
            }
            catch (Exception e)
            {
                DPCServiceEvents.Log.UnableToReadRegistry(registryValue, e.Message);
            }
        }
    }
}