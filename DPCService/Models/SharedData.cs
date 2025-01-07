using DPCLibrary.Enums;
using DPCLibrary.Models;
using DPCLibrary.Utils;
using DPCService.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DPCService.Models
{
    public class SharedData
    {
        private readonly object ManagedProfileLock = new object();
        private readonly object ProfileUpdateLock = new object();
        private readonly object ConnectedListLock = new object();

        private IList<string> ConnectedVPNList = new List<string>();
        private static readonly Random Rand = new Random();
        private readonly List<ManagedProfile> ManagedProfileList = new List<ManagedProfile>();
        private readonly List<ManagedProfile> ProfileUpdateList = new List<ManagedProfile>();
        private readonly int UpdateTime;
        private bool UpdateOnUnmanagedConnection;
        private CancellationToken CancelToken;
        private bool rasManRestartNeeded = false;

        public bool DumpOnException { get; }

        public int PendingUpdates()
        {
            lock (ProfileUpdateLock)
            {
                return ProfileUpdateList.Count;
            }
        }

        public SharedData(int updateTime, bool updateOnUnmanagedConnection, bool dumpOnException, CancellationToken RootCancelToken)
        {
            UpdateTime = updateTime;
            UpdateOnUnmanagedConnection = updateOnUnmanagedConnection;
            DumpOnException = dumpOnException;
            CancelToken = RootCancelToken;

            lock(ConnectedVPNList)
            {
                //Get initial connected list
                ConnectedVPNList = AccessRasApi.ListConnectedProfiles();
            }

            try
            {
                lock (ManagedProfileLock)
                {
                    AddExistingTunnelOnStartup(ProfileType.Machine);
                    AddExistingTunnelOnStartup(ProfileType.User);
                    AddExistingTunnelOnStartup(ProfileType.UserBackup);
                    DPCServiceEvents.Log.FoundExistingProfiles(ManagedProfileList.Count);
                }
            }
            catch (Exception ex)
            {
                DPCServiceEvents.Log.ProfileLoadOnStartUpError(ex.Message, ex.StackTrace);
            }
        }

        public void UpdateOnUnmanagedConnectionValue(bool newValue)
        {
            UpdateOnUnmanagedConnection = newValue;
        }

        public void RequestRasManRestart()
        {
            DPCServiceEvents.Log.RestartRasManServiceRequested();
            rasManRestartNeeded = true;
        }

        private void AddExistingTunnelOnStartup(ProfileType profileType)
        {
            lock (ManagedProfileLock)
            {
                string existingTunnelProfileName = AccessRegistry.ReadMachineString(AccessRegistry.GetProfileNameRegistryName(profileType), RegistrySettings.InternalStateOffset);
                //Where a tunnel name has been defined and the name is not already in the list
                if (!string.IsNullOrWhiteSpace(existingTunnelProfileName) && ManagedProfileList.Where(p => p.ProfileName == existingTunnelProfileName).Count() == 0)
                {
                    //If a profile has been deleted outside of DPC a profile may not be found on startup
                    ManagedProfile profile = ProfileAction.GetProfile(existingTunnelProfileName, profileType, CancelToken);
                    if (profile.ProfileObj != null && string.IsNullOrWhiteSpace(profile.ProfileObj.LoadError))
                    {
                        //Profile found and no issues loading the details
                        profile.ProfileDeployed = true; //Profile is already installed so must have been deployed
                        ManagedProfileList.Add(profile);
                        HandleConnectedProfileUpdate(); //Handle config changed but profile has already started before DPC startup
                    }
                    else
                    {
                        //Clean up Monitoring Registry key if the profile is unavailable for management
                        AccessRegistry.RemoveRegistryKeyValue(RegistrySettings.InternalStateFullPath, AccessRegistry.GetProfileNameRegistryName(profileType));
                    }
                }
            }
        }

        public IList<string> GetManagedProfileList()
        {
            lock (ManagedProfileLock)
            {
                return ManagedProfileList.Select(mp => mp.ProfileName).ToList();
            }
        }

        public int GetUpdateTime()
        {
            //Profile a randomized offset to minimize the events simultaneously occurring as most events are initialized on startup and therefore would continue to process on top of each other
            //Randomise between 1 second and 60 seconds
            return UpdateTime + Rand.Next(1000, 60000);
        }

        public void RemoveProfile(string profileName)
        {
            DPCServiceEvents.Log.ProfileRemoveScheduled(profileName);
            AddProfileUpdate(new ManagedProfile {
                ProfileName = profileName,
                ProfileXML = null
            });
        }

        public void AddProfileUpdate(ManagedProfile profile)
        {
            if (profile == null)
            {
                return;
            }

            try
            {
                profile.RemoveWhiteSpace();
            }
            catch (Exception e)
            {
                DPCServiceEvents.Log.UnableToRemoveWhiteSpace(e.Message, e.StackTrace);
            }

            lock (ManagedProfileLock)
            {
                //Check profile is identical to existing profile, its not about to be deleted and the profile still exists on the Host
                if (ManagedProfileList.Contains(profile) && //Profile is already classed as Managed
                    profile.ProfileXML != null && //Profile is not scheduled for deletion
                    ManageRasphonePBK.ListSystemProfiles(profile.ProfileName).Count > 0) //Profile is already added and specifically added to the SYSTEM Profile to avoid existing user profiles classing as existing
                {
                    //All fields match
                    DPCServiceEvents.Log.ProfileAlreadyUpToDate(profile.ProfileName);
                    return;
                }
            }

            //If any changes are needed schedule update, if successful the profile will be added as managed after update
            lock (ProfileUpdateLock)
            {
                //Check to see if there are existing profiles containing a previous profile
                ManagedProfile previousUpdate = ProfileUpdateList.Where(p =>
                    p.ProfileName == profile.ProfileName && //Profile Name Matches
                    !p.ProfileDeployed && //AND Profile hasn't previously been successfully deployed
                    !string.IsNullOrWhiteSpace(p.OldProfileName) //AND old profile name has been populated to show it needs to wait on an existing profile before being deployed
                ).FirstOrDefault(); //Return null if there is no eligible previous update

                //Update the new profile update with the existing requirement to wait for a profile to be deleted
                if (previousUpdate != null)
                {
                    profile.OldProfileName = previousUpdate.OldProfileName;
                }

                if (!string.IsNullOrWhiteSpace(profile.OldProfileName))
                {
                    ManagedProfile existingRemoveRequest = ProfileUpdateList.Where(p =>
                        p.ProfileName == profile.OldProfileName && //Profile Name Matches
                        !p.ProfileDeployed && //AND Profile hasn't previously been successfully deployed
                        !string.IsNullOrWhiteSpace(p.OldProfileName) && //AND old profile name has been populated to show it needs to wait on an existing profile before being deployed
                        string.IsNullOrWhiteSpace(p.ProfileXML) //AND ManagedProfile is a delete request
                    ).FirstOrDefault(); //Return null if there is no eligible previous update
                    if (existingRemoveRequest != null)
                    {
                        //Delete request with an old profile set means that the previous create profile request never happened
                        //therefore there is a chain of profiles waiting on a delete request.

                        //We can simply bypass the middle profile being created by setting the new create profile to be
                        //dependent on the original profile and then delete the request to delete a profile that never
                        //existed
                        profile.OldProfileName = existingRemoveRequest.OldProfileName;
                        ProfileUpdateList.RemoveAll(p => p.ProfileName == existingRemoveRequest.ProfileName);
                    }
                }

                //After handling the tracking, push the latest update to pending update
                ProfileUpdateList.RemoveAll(p => p.ProfileName == profile.ProfileName);

                ProfileUpdateList.Add(profile);
                DPCServiceEvents.Log.ProfileScheduled(profile.ProfileName);
            }

            //MTU settings are connection/runtime applicable only and therefore can be updated dynamically without having to wait for the
            //Profile to be fully refreshed. This updates the existing deployed profile if it exists and needs to be updated so that runtime
            //Checks can be successfully performed/updated as required
            lock (ManagedProfileLock)
            {
                ManagedProfile managedProfile = ManagedProfileList.Where(p => p.ProfileName == profile.ProfileName && p.MTU != profile.MTU).FirstOrDefault();
                if (managedProfile != null)
                {
                    managedProfile.MTU = profile.MTU;
                }
            }
        }

        private IList<string> GetConnectedVPNList()
        {
            lock (ConnectedListLock)
            {
                return ConnectedVPNList;
            }
        }

        //Either add or remove a list of connections from the tracked connections list.
        public IList<string> UpdateConnectionList(IList<string> updateList, bool addConnections)
        {
            lock (ConnectedListLock)
            {
                IList<string> newConnections;
                if (addConnections)
                {
                    newConnections = updateList.Except(GetConnectedVPNList()).ToList();
                }
                else
                {
                    newConnections = GetConnectedVPNList().Except(updateList).ToList();
                }

                SetConnectedVPNList(updateList);
                return newConnections;
            }
        }

        public void SetConnectedVPNList(IList<string> newList)
        {
            lock (ConnectedListLock)
            {
                ConnectedVPNList = newList;
            }
        }

        private List<ManagedProfile> GetAvailableUpdates()
        {
            List<ManagedProfile> profilesToUpdate = new List<ManagedProfile>();
            IList<string> connectedList = GetConnectedVPNList(); //Get local version to avoid things changing or deadlocking
            lock (ProfileUpdateLock)
            {
                //if there are no updates send nothing back
                if (ProfileUpdateList.Count == 0)
                {
                    return ProfileUpdateList;
                }

                //if updates are allowed or no existing connection are active
                if (connectedList.Count == 0)
                {
                    profilesToUpdate = ProfileUpdateList;
                }
                else if (UpdateOnUnmanagedConnection || (!UpdateOnUnmanagedConnection && connectedList.All(cp => GetManagedProfileList().Contains(cp))))
                {
                    //Either we enable profiles to be updated while unmanaged connections are also updated
                    //or all updates are to managed connections

                    //Get all profiles which are not connected and do not have a dependency on an old Profile name
                    //This ensures that the old profile is deleted first before a new profile is added
                    profilesToUpdate = ProfileUpdateList.Where(p => !connectedList.Contains(p.ProfileName) && string.IsNullOrWhiteSpace(p.OldProfileName)).ToList();

                    profilesToUpdate.AddRange(ProfileUpdateList.Where(
                        p => !connectedList.Contains(p.ProfileName) && //Profile Name is not currently connected
                        !string.IsNullOrWhiteSpace(p.OldProfileName) && //AND the Profile previously had a different name (Profiles without a previous name are already handled above)
                        !connectedList.Contains(p.OldProfileName) //AND the previous Profile is not currently connected
                    ).ToList());
                }
                //else do not update any profiles

                //Process removals first
                return profilesToUpdate.OrderBy(p => p.ProfileXML).ToList();
            }
        }

        public void HandleProfileUpdates()
        {
            List<ManagedProfile> profileUpdates = new List<ManagedProfile>(); //Create local context of Profile Update List to allow us not to keep 2 locks open at the same time (possible deadlock)
            lock (ProfileUpdateLock)
            {
                profileUpdates = GetAvailableUpdates();
                foreach (ManagedProfile profile in profileUpdates)
                {
                    try
                    {
                        ProfileAction.CreateProfile(profile, CancelToken);

                        //Profile update handled, remove from pending list
                        profile.ProfileDeployed = true;
                        ProfileUpdateList.RemoveAll(p => p.ProfileName == profile.ProfileName);
                        DPCServiceEvents.Log.ProfileCompletedProfileModification(profile.ProfileName);
                    }
                    catch (Exception e)
                    {
                        if (DumpOnException)
                        {
                            MiniDump.Write();
                        }

                        //Some types of failure start the tattoo process before failing so need cleaning up
                        ProfileAction.Cleanup(profile.ProfileName, false, profile.ProfileType);

                        DPCServiceEvents.Log.AddProfileFailed(profile.ProfileName, profile.ProfileType, e.Message, e.StackTrace);
                    }
                }
            }

            lock (ManagedProfileLock)
            {
                foreach (ManagedProfile profile in profileUpdates)
                {
                    //Remove tacking of old profile
                    ManagedProfileList.RemoveAll(p => p.ProfileName == profile.ProfileName);
                    //Keep track in memory that we have added a profile
                    if (!string.IsNullOrWhiteSpace(profile.ProfileXML))
                    {
                        ManagedProfileList.Add(profile);
                    }
                }

                //Loop through all managed profiles to update pbk settings, changes don't save if nothing to do
                //Some settings might be changed by windows, so we want to loop through all known tunnels and set them back to
                //What we were expecting
                foreach (ManagedProfile profile in ManagedProfileList)
                {
                    //Skip trying to update invalid profiles
                    if (profile.ProfileDeployed)
                    {
                        ProfileAction.ManageRASEntryUpdates(profile);
                        ProfileAction.ManageWMIEntryUpdates(profile, CancelToken);
                        rasManRestartNeeded |= ProfileAction.ManageAutoTriggerSettings(profile, CancelToken);
                        HandleConnectedProfileUpdate();
                        ProfileAction.RemoveHiddenProfile(profile);
                    }
                }
            }

            //Track rasMan restarts over more than 1 refresh so that if the tunnels to shut rasMan can be restarted at that point
            if (rasManRestartNeeded)
            {
                ManagedProfile deviceTunnelProfile = ManagedProfileList.Where(p => p.ProfileType == ProfileType.Machine).FirstOrDefault();
                if (ConnectedVPNList.Count == 0 || (ConnectedVPNList.Count == 1 && deviceTunnelProfile != null && ConnectedVPNList.Contains(deviceTunnelProfile.ProfileName)))
                {
                    DPCServiceEvents.Log.RestartingRasManService();

                    //Start the restart in a different thread to avoid locking up the main DPC threads waiting for RasMan to restart
                    new TaskFactory().StartNew(() => AccessServices.RestartService("RasMan")).ContinueWith(
                        //If there is an error with the service then log the error however as this is completely async code we need to define the error handling as part of the task creation
                        t => {
                            //Task wraps the actual exceptions in an aggregate exception so we look through and raise a event for each actual exception
                            foreach (Exception e in t.Exception.InnerExceptions)
                            {
                                DPCServiceEvents.Log.RestartingRasManServiceFailed(e.Message, e.StackTrace);
                            }
                        }, CancelToken, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default
                    );
                    //always mark the restart as having happened to avoid repeated restart attempts when it is failing
                    rasManRestartNeeded = false; //Mark the restart as having happened
                }
                else
                {
                    DPCServiceEvents.Log.RasManRestartNeeded();
                }
            }
        }

        public void HandleConnectedProfileUpdate()
        {
            lock (ManagedProfileLock)
            {
                //Loop through all managed profiles to update MTU/network interface settings
                //This has to be done after connection to make sure that the network interface exists
                foreach (ManagedProfile profile in ManagedProfileList)
                {
                    //Skip trying to update invalid profiles
                    if (profile.ProfileDeployed && ConnectedVPNList.Contains(profile.ProfileName))
                    {
                        ProfileAction.ManageNetIOUpdates(profile);
                    }
                }
            }
        }
    }
}