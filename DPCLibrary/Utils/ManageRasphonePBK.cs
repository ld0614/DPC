using DPCLibrary.Enums;
using DPCLibrary.Exceptions;
using DPCLibrary.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DPCLibrary.Utils
{
    public static class ManageRasphonePBK
    {
        public static List<ProfileInfo> ListProfiles(string profileName)
        {
            List<ProfileInfo> profileList = ListAllProfiles(false);
            List<ProfileInfo> namedProfiles = profileList.Where(p => p.ProfileName == profileName).ToList();
            return namedProfiles;
        }

        public static List<ProfileInfo> ListHiddenProfiles(string profileName)
        {
            List<ProfileInfo> profileList = ListAllProfiles(true);
            List<ProfileInfo> namedProfiles = profileList.Where(p => p.ProfileName == profileName && p.Hidden == true).ToList();
            return namedProfiles;
        }

        //Same as ListProfiles but only include ones installed in the SYSTEM Context to avoid a user profile of exactly the same
        ///CSP breaking assumptions about DPC profiles being installed at the SYSTEM level
        public static List<ProfileInfo> ListSystemProfiles(string profileName)
        {
            //Uses CurrentUserSID to avoid Breaking unit tests but will only return with system profiles when running under its primary service
            List<ProfileInfo> namedProfiles = ListProfiles(profileName).Where(p => p.Sid == DeviceInfo.CurrentUserSID()).ToList();
            return namedProfiles;
        }

        public static ProfileInfo ListProfiles(string profileName, string sid)
        {
            List<ProfileInfo> namedProfiles = ListProfiles(profileName).Where(p => p.Sid == sid).ToList();
            //In theory there should only be a single profile with the same name per user/sid
            if (namedProfiles.Count > 0)
            {
                return namedProfiles[0];
            }
            return null;
        }

        public static List<ProfileInfo> ListAllProfiles(bool includeHiddenProfiles)
        {
            List<ProfileInfo> profiles = new List<ProfileInfo>();
            IList<UserInfo> userList = ListUserDirectories();
            foreach (UserInfo user in userList)
            {
                profiles.AddRange(ListProfilesForUser(user, includeHiddenProfiles));
            }
            return profiles;
        }

        public static IList<ProfileInfo> ListProfilesForUser(UserInfo user, bool includeHiddenProfiles)
        {
            IList<ProfileInfo> profileList = new List<ProfileInfo>();
            IList<string> PBKPaths = ListPBKFiles(user);
            //If no PBK Paths are identified there can't be any profiles managed by the account therefore there are no Profile Info objects to produce
            foreach (string PBKPath in PBKPaths)
            {
                IList<string> profileNameList = AccessRasApi.ListProfilesFromDirectory(PBKPath);
                foreach (string profile in profileNameList)
                {
                    profileList.Add(new ProfileInfo()
                    {
                        ProfileName = profile,
                        PBKPath = PBKPath,
                        Sid = user.Sid,
                        Hidden = false
                    });
                }
            }

            if (includeHiddenProfiles)
            {
                IList<string> HiddenPBKPaths = ListHiddenPBKFiles(user);
                //If no PBK Paths are identified there can't be any profiles managed by the account therefore there are no Profile Info objects to produce
                foreach (string PBKPath in HiddenPBKPaths)
                {
                    IList<string> profileNameList = AccessRasApi.ListProfilesFromDirectory(PBKPath);
                    foreach (string profile in profileNameList)
                    {
                        profileList.Add(new ProfileInfo()
                        {
                            ProfileName = profile,
                            PBKPath = PBKPath,
                            Sid = user.Sid,
                            Hidden = true
                        });
                    }
                }
            }

            return profileList;
        }

        public static IList<string> ListPBKFiles(UserInfo user)
        {
            if (string.IsNullOrWhiteSpace(user.ProfilePath))
            {
                return new List<string>();
            }
            string rootOffset = @"AppData\Roaming\Microsoft\Network\Connections\Pbk";
            if (user.Sid == DeviceInfo.SYSTEMSID)
            {
                rootOffset = @"Microsoft\Network\Connections\Pbk";
            }

            string fullPath = Path.Combine(user.ProfilePath, rootOffset);

            if (!Directory.Exists(fullPath))
            {
                //If the directory doesn't exist at all return back an empty list rather than crashing
                return new List<string>();
            }

            return Directory.GetFiles(fullPath, "*.pbk", SearchOption.TopDirectoryOnly); //Don't search recursive such as _hiddenPbk
        }

        public static IList<string> ListHiddenPBKFiles(UserInfo user)
        {
            if (string.IsNullOrWhiteSpace(user.ProfilePath))
            {
                return new List<string>();
            }
            string rootOffset = @"AppData\Roaming\Microsoft\Network\Connections\Pbk\_hiddenPbk";
            if (user.Sid == DeviceInfo.SYSTEMSID)
            {
                return new List<string>(); //The system account never has a need for a hiddenPbk
            }

            string fullPath = Path.Combine(user.ProfilePath, rootOffset);

            if (!Directory.Exists(fullPath))
            {
                //If the directory doesn't exist at all return back an empty list rather than crashing
                return new List<string>();
            }

            return Directory.GetFiles(fullPath, "*.pbk", SearchOption.TopDirectoryOnly); //Don't search recursive
        }

        public static string GetPrimaryPBKFile(UserInfo user)
        {
            string rootOffset = @"AppData\Roaming\Microsoft\Network\Connections\Pbk";
            if (user == null || user.Sid == DeviceInfo.SYSTEMSID)
            {
                rootOffset = @"Microsoft\Network\Connections\Pbk";
            }

            string pbkPath = Path.Combine(user.ProfilePath, rootOffset, "rasphone.pbk");

            if (!File.Exists(pbkPath))
            {
                //If PBK doesn't exist return null
                return null;
            }

            return pbkPath;
        }

        public static IList<UserInfo> ListUserDirectories()
        {
            IList<UserInfo> profiles = new List<UserInfo>();
            IList<string> profileSIDs = AccessRegistry.ReadMachineSubkeys(RegistrySettings.ProfileList);
            foreach (string sid in profileSIDs)
            {
                UserInfo user = GetUserInfo(sid);
                if (user != null)
                {
                    profiles.Add(user);
                }
            }

            return profiles;
        }

        public static UserInfo GetUserInfo(string sid)
        {
            string profileFilePath = AccessRegistry.ReadMachineString(RegistrySettings.ProfileImagePath, sid, RegistrySettings.ProfileList, true);

            //System Profiles do not have the fullProfile Attribute and should be skipped to avoid lots of system duplicates
            bool fullProfile = AccessRegistry.ReadMachineBoolean(RegistrySettings.FullProfile, sid, RegistrySettings.ProfileList, false);
            if (fullProfile)
            {
                return new UserInfo()
                {
                    Sid = sid,
                    ProfilePath = profileFilePath
                };
            }
            else if (sid == DeviceInfo.SYSTEMSID)
            {
                return new UserInfo()
                {
                    Sid = sid,
                    ProfilePath = Environment.GetEnvironmentVariable("ProgramData")
                };
            }

            return null;
        }

        public static bool RemoveProfile(string profileName)
        {
            List<ProfileInfo> namedProfiles = ListProfiles(profileName);
            List<Exception> errorList = new List<Exception>();
            bool endResult = false;

            if (namedProfiles.Count <= 0)
            {
                //nothing to do
                return false;
            }

            foreach (ProfileInfo profile in namedProfiles)
            {
                RemoveProfileResult RasAPIRemoveResult = AccessRasApi.RemoveProfile(profile);
                if (RasAPIRemoveResult.Status)
                {
                    //Ignore the WMI error as RAS API Removal succeeded
                    endResult = true;
                }
                else
                {
                    errorList.Add(RasAPIRemoveResult.Error);
                }
            }

            if (!endResult)
            {
                //No Removal Succeeded
                string errorMessage = "Error Removing Profile:" + Environment.NewLine;
                foreach (Exception error in errorList)
                {
                    if (error != null)
                    {
                        errorMessage += Environment.NewLine + error.Message + Environment.NewLine + error.StackTrace + Environment.NewLine;
                    }
                }
                throw new Exception(errorMessage);
            }

            List<ProfileInfo> profileRemoveCheck = ListProfiles(profileName);
            if (profileRemoveCheck.Count > 0)
            {
                throw new Exception("Detected Profile after removal");
            }

            //Profile not detected any more and at least 1 profile was removed
            return true;
        }

        public static IList<Exception> RemoveHiddenProfile(string profileName)
        {
            List<ProfileInfo> namedProfiles = ListHiddenProfiles(profileName);
            List<Exception> errorList = new List<Exception>();

            if (namedProfiles.Count <= 0)
            {
                //nothing to do
                errorList.Add(new NoOperationException());
                return errorList;
            }

            foreach (ProfileInfo profile in namedProfiles)
            {
                RemoveProfileResult RasAPIRemoveResult = AccessFile.DeleteFile(profile.PBKPath);

                if (!RasAPIRemoveResult.Status)
                {
                    errorList.Add(RasAPIRemoveResult.Error);
                }
            }

            //If all delete operations completed successfully an empty list will be returned;
            //If any fail, the failure messages will be returned
            //If there are no deletions required, a NoOperationException will be returned
            return errorList;
        }

        public static IList<string> IdentifyCorruptPBKs()
        {
            List<string> corruptPBKs = new List<string>();
            IList<UserInfo> userList = ListUserDirectories();
            foreach (UserInfo user in userList)
            {
                IList<string> HiddenPBKPaths = ListHiddenPBKFiles(user);
                foreach (string PBKPath in HiddenPBKPaths)
                {
                    if (AccessFile.GetFileSize(PBKPath) > 0)
                    {
                        IList<string> profileNameList = AccessRasApi.ListProfilesFromDirectory(PBKPath);
                        if (profileNameList.Count == 0)
                        {
                            //No valid profiles detected but data is in file which suggests that the file has a corrupt profile in it
                            corruptPBKs.Add(PBKPath);
                        }
                    }
                }
            }
            return corruptPBKs;
        }
    }
}
