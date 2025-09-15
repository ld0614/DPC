using DPCLibrary.Enums;
using DPCLibrary.Exceptions;
using Microsoft.Diagnostics.Tracing;

namespace DPCService.Utils
{
    //Documented https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.tracing.eventsource?view=net-6.0
    [EventSource(Name = "DPC-AOVPN-DPCService")]
    internal sealed class DPCServiceEvents : EventSource
    {
        public static DPCServiceEvents Log = new DPCServiceEvents();

        #region 1-100 Application Startup

        [Event(1, Message = "DPC Service is starting", Level = EventLevel.Informational, Channel = EventChannel.Admin)]
        public void Startup() { WriteEvent(1); }
        [Event(2, Message = "Running in SYSATTACH mode, waiting for debug attach", Level = EventLevel.Informational, Channel = EventChannel.Admin)]
        public void SYSATTACHEnabled() { WriteEvent(2); }
        [Event(3, Message = "Running in interactive mode", Level = EventLevel.Informational, Channel = EventChannel.Admin)]
        public void InteractiveModeEnabled() { WriteEvent(3); }

        [Event(4, Message = "DPC Service Startup Complete", Level = EventLevel.Informational, Channel = EventChannel.Admin)]
        public void StartupComplete() { WriteEvent(4); }

        [Event(5, Message = "DPC Initializing", Level = EventLevel.Verbose, Channel = EventChannel.Operational)]
        public void DPCServiceInitializing() { WriteEvent(5); }

        [Event(6, Message = "DPC Initialized", Level = EventLevel.Verbose, Channel = EventChannel.Operational)]
        public void DPCServiceInitialized() { WriteEvent(6); }

        [Event(7, Message = "Product Version is {0}, DEBUG: {1}\nOS Version: {2}", Level = EventLevel.Informational, Channel = EventChannel.Admin)]
        public void ProductVersion(string message, bool debug, string OSVersion) { WriteEvent(7, message, debug, OSVersion); }

        [Event(8, Message = "Registry Entry {0} was missing, defaulting to {1}", Level = EventLevel.Verbose, Channel = EventChannel.Debug)]
        public void MissingRegistryEntry(string variableName, string defaultValue) { WriteEvent(8, variableName, defaultValue); }
        [Event(13, Message = "Registry Entry {0} was invalid, defaulting to {1}", Level = EventLevel.Verbose, Channel = EventChannel.Operational)]
        public void InvalidRegistryEntry(string variableName, string defaultValue) { WriteEvent(13, variableName, defaultValue); }
        [Event(14, Message = "Loading Service Components", Level = EventLevel.Verbose, Channel = EventChannel.Operational)]
        public void LoadingServiceComponents() { WriteEvent(14); }
        [Event(15, Message = "Startup Account is not an administrator. Service will now Stop", Level = EventLevel.Error, Channel = EventChannel.Admin)]
        public void UserNotAdmin() { WriteEvent(15); CriticalHaltDueToError(); }
        [Event(16, Message = "Running in service mode", Level = EventLevel.Informational, Channel = EventChannel.Admin)]
        public void ServiceModeEnabled() { WriteEvent(16); }
        [Event(17, Message = "Migration Block Enabled", Level = EventLevel.Informational, Channel = EventChannel.Operational)]
        public void MigrationBlockEnabled() { WriteEvent(17); }
        [Event(22, Message = "Unable to load previous profile information\nErrors: {0} \nLocation: {1}", Level = EventLevel.Error, Channel = EventChannel.Operational)]
        public void ProfileLoadOnStartUpError(string errors, string location) { WriteEvent(22, errors, location); }
        [Event(25, Message = "Found {0} Managed Profiles", Level = EventLevel.Informational, Channel = EventChannel.Operational)]
        public void FoundExistingProfiles(int number) { WriteEvent(25, number); }
        [Event(26, Message = "Starting unmanaged Profile Removal Service", Level = EventLevel.Informational, Channel = EventChannel.Admin)]
        public void StartingUnmanagedProfileRemovalService() { WriteEvent(26); }
        [Event(28, Message = "Starting GPUpdate Notification Service", Level = EventLevel.Informational, Channel = EventChannel.Admin)]
        public void StartGPUpdateMonitoring() { WriteEvent(28); }
        [Event(29, Message = "Custom MTU setting enabled while existing profiles have not been configured for removal\nIf there are any non-DPC Profiles on this system they will likely also be impacted by the MTU change", Level = EventLevel.Warning, Channel = EventChannel.Admin)]
        public void ExistingProfileMTUImpact() { WriteEvent(29); }
        [Event(30, Message = "File Logging Configured to: {0}", Level = EventLevel.Informational, Channel = EventChannel.Admin)]
        public void FileLoggingPath(string filePath) { WriteEvent(30, filePath); }
        [Event(31, Message = "File Logging Configuration Failed with error message: {0}", Level = EventLevel.Error, Channel = EventChannel.Admin)]
        public void FileLoggingConfigError(string message) { WriteEvent(31, message); }
        [Event(32, Message = "File Logging Closure Failed with error message: {0}", Level = EventLevel.Error, Channel = EventChannel.Admin)]
        public void FileLoggingDisposeError(string message) { WriteEvent(32, message); }
        #endregion 1-100 Application Startup

        #region 100-199 Application Shutdown

        [Event(100, Message = "DPC Service is stopping", Level = EventLevel.Informational, Channel = EventChannel.Admin)]
        public void Shutdown() { WriteEvent(100); }

        [Event(101, Message = "DPC Service is Stopped", Level = EventLevel.Informational, Channel = EventChannel.Admin)]
        public void ShutdownCompleted() { WriteEvent(101); }

        [Event(102, Message = "Canceling all child services", Level = EventLevel.Informational, Channel = EventChannel.Admin)]
        public void CancelChildServices() { WriteEvent(102); }

        [Event(103, Message = "All child services stopped", Level = EventLevel.Informational, Channel = EventChannel.Admin)]
        public void CancelChildServicesCompleted() { WriteEvent(103); }

        #endregion 100-199 Application Shutdown

        #region 1000-1099 VPN Monitoring

        [Event(1000, Message = "Starting VPN Monitoring Service", Level = EventLevel.Informational, Channel = EventChannel.Admin)]
        public void StartVPNMonitoringFull() { WriteEvent(1000); }
        [Event(1002, Message = "New VPN Connection Made on profile: {0}", Level = EventLevel.Informational, Channel = EventChannel.Operational)]
        public void NewVPNConnectionEvent(string profileName) { WriteEvent(1002, profileName); }

        [Event(1003, Message = "VPN Disconnected on profile: {0}", Level = EventLevel.Informational, Channel = EventChannel.Operational)]
        public void NewVPNDisconnectionEvent(string profileName) { WriteEvent(1003, profileName); }

        [Event(1004, Message = "VPN Connection reconnected", Level = EventLevel.Informational, Channel = EventChannel.Operational)]
        public void NewVPNReconnectionEvent() { WriteEvent(1004); }

        [Event(1005, Message = "Shutting down VPN Monitoring Service", Level = EventLevel.Informational, Channel = EventChannel.Admin)]
        public void StoppingVPNMonitoring() { WriteEvent(1005); }

        [Event(1006, Message = "VPN Monitoring Service Shutdown Complete", Level = EventLevel.Informational, Channel = EventChannel.Admin)]
        public void VPNMonitoringStopped() { WriteEvent(1006); }

        [Event(1008, Message = "New VPN Connection Made on Unknown Profile", Level = EventLevel.Informational, Channel = EventChannel.Operational)]
        public void NewVPNUnknownConnectionEvent() { WriteEvent(1008); }

        [Event(1009, Message = "Unable to gather currently Connected profiles, Exception Message: {0}", Level = EventLevel.Warning, Channel = EventChannel.Operational)]
        public void UnableToGetConnectedProfiles(string message) { WriteEvent(1009, message); }

        [Event(1010, Message = "New VPN Disconnection Made on Unknown Profile", Level = EventLevel.Informational, Channel = EventChannel.Operational)]
        public void NewVPNUnknownDisconnectionEvent() { WriteEvent(1010); }
        [Event(1011, Message = "Spinlock for profile {0} was already owned, skipping profile update", Level = EventLevel.Warning, Channel = EventChannel.Operational)]
        public void ProfileUpdateSkipped(string profileName) { WriteEvent(1011, profileName); }
        [Event(1012, Message = "Event Handler currently engaged for profile {0}, waiting for update to complete...", Level = EventLevel.Informational, Channel = EventChannel.Debug)]
        public void ProfileShutdownRequested(string profileName) { WriteEvent(1012, profileName); }
        [Event(1013, Message = "Profile {0} shutdown requested", Level = EventLevel.Informational, Channel = EventChannel.Debug)]
        public void ProfileUpdateShutdownRequested(string profileName) { WriteEvent(1013, profileName); }
        [Event(1014, Message = "Profile {0} shutdown complete", Level = EventLevel.Informational, Channel = EventChannel.Debug)]
        public void ProfileUpdateShutdownComplete(string profileName) { WriteEvent(1014, profileName); }
        [Event(1018, Message = "An Error occurred during a monitoring Process\nError: {0}\nStackTrace:{1}", Level = EventLevel.Error, Channel = EventChannel.Operational)]
        public void VPNMonitorErrorOnWait(string message, string stackTrace) { WriteEvent(1018, message, stackTrace); }
        [Event(1019, Message = "Starting profile update for {0}", Level = EventLevel.Informational, Channel = EventChannel.Debug)]
        public void ProfileUpdateStartedDebug(string profileName) { WriteEvent(1019, profileName); }
        [Event(1020, Message = "Profile update generated for {0}", Level = EventLevel.Informational, Channel = EventChannel.Debug)]
        public void ProfileUpdateGenerated(string profileName) { WriteEvent(1019, profileName); }
        [Event(1021, Message = "Check profile {0} for updates", Level = EventLevel.Informational, Channel = EventChannel.Operational)]
        public void ProfileUpdateStarted(string profileName) { WriteEvent(1021, profileName); }
        [Event(1022, Message = "An Error occurred while configuring the GPUpdate Notification Process\nError: {0}\nStackTrace:{1}", Level = EventLevel.Error, Channel = EventChannel.Operational)]
        public void MonitorGPUpdateErrorOnStartup(string message, string stackTrace) { WriteEvent(1022, message, stackTrace); }
        [Event(1023, Message = "Spinlock for checking corrupt PBKs in Profile {0} was already owned, skipping profile update", Level = EventLevel.Warning, Channel = EventChannel.Debug)]
        public void CorruptPbkCheckSkipped(string profileName) { WriteEvent(1023, profileName); }
        [Event(1024, Message = "Network change detected, local gateway capability has changed from {0} to {1}", Level = EventLevel.Informational, Channel = EventChannel.Operational)]
        public void NetworkChangeDetected(string oldValue, string newValue) { WriteEvent(1024, oldValue, newValue); }
        [Event(1025, Message = "Network change detected, neither IPv4 or IPv6 are supported", Level = EventLevel.Warning, Channel = EventChannel.Operational)]
        public void NetworkChangeUnkownType() { WriteEvent(1025); }
        #endregion 1000-1099 VPN Monitoring

        #region 1100-1299 Profile Monitoring

        [Event(1101, Message = "Shutting down Profile Monitoring Service", Level = EventLevel.Informational, Channel = EventChannel.Admin)]
        public void StoppingProfileMonitor() { WriteEvent(1101); }

        [Event(1102, Message = "Profile Monitoring Service Shutdown Complete", Level = EventLevel.Informational, Channel = EventChannel.Admin)]
        public void VPNProfileMonitorStopped() { WriteEvent(1102); }

        [Event(1103, Message = "Starting Monitoring of {0} profile", Level = EventLevel.Informational, Channel = EventChannel.Admin)]
        public void StartProfileMonitor(string message) { WriteEvent(1103, message); }

        [Event(1105, Message = "VPN Profile Monitoring Status Changed", Level = EventLevel.Informational, Channel = EventChannel.Operational)]
        public void ProfileMonitorChanged() { WriteEvent(1105); }

        [Event(1106, Message = "Update to process Profile {0} due to Errors: \n {1}", Level = EventLevel.Error, Channel = EventChannel.Operational)]
        public void ProfileGenerationErrors(string profileName, string errors) { WriteEvent(1106, profileName, errors); }

        [Event(1107, Message = "Profile {0} may be inaccurate due to the following Warnings: \n{1}", Level = EventLevel.Warning, Channel = EventChannel.Operational)]
        public void ProfileGenerationWarnings(string profileName, string errors) { WriteEvent(1107, profileName, errors); }
        [Event(1108, Message = "Profile {0} (Type {1}) was not added.\nError: \n{2} \nStackTrace: \n{3}", Level = EventLevel.Error, Channel = EventChannel.Operational)]
        public void AddProfileFailed(string profileName, ProfileType profileType, string errors, string stackTrace) { WriteEvent(1108, profileName, profileType, errors, stackTrace); }
        [Event(1109, Message = "Unexpected failure in profile creation process: \nProfile Name: {0}\nError: {1}\nStackTrace: {2}", Level = EventLevel.Error, Channel = EventChannel.Operational)]
        public void ProfileCreationFailed(string profileName, string errors, string stackTrace) { WriteEvent(1109, profileName, errors, stackTrace); }
        [Event(1110, Message = "Unable to save profile to disk: {0} \n Errors: {1} \n Location: {2}", Level = EventLevel.Warning, Channel = EventChannel.Operational)]
        public void UpdateToSaveProfileToDisk(string profileName, string errors, string location) { WriteEvent(1110, profileName, errors, location); }
        [Event(1111, Message = "Unable to load Registry Value {0} due to Reason: \n{1}", Level = EventLevel.Warning, Channel = EventChannel.Operational)]
        public void UnableToReadRegistry(string regValue, string errors) { WriteEvent(1111, regValue, errors); }
        [Event(1112, Message = "Removing Profile {0}", Level = EventLevel.Informational, Channel = EventChannel.Debug)]
        public void ProfileDebugRemoveProfile(string profileName) { WriteEvent(1112, profileName); }
        [Event(1113, Message = "Adding Profile {0}", Level = EventLevel.Informational, Channel = EventChannel.Debug)]
        public void ProfileDebugAddProfile(string profileName) { WriteEvent(1113, profileName); }
        [Event(1114, Message = "Modify Profile {0} Completed successfully", Level = EventLevel.Informational, Channel = EventChannel.Operational)]
        public void ProfileCompletedProfileModification(string profileName) { WriteEvent(1114, profileName); }
        [Event(1115, Message = "Profile {0} Removed", Level = EventLevel.Informational, Channel = EventChannel.Debug)]
        public void ProfileDebugProfileRemoved(string profileName) { WriteEvent(1115, profileName); }
        [Event(1116, Message = "No removal of Profile {0} required", Level = EventLevel.Informational, Channel = EventChannel.Debug)]
        public void ProfileDebugProfileRemoveNotRequired(string profileName) { WriteEvent(1116, profileName); }
        [Event(1117, Message = "{0} scheduled for removal", Level = EventLevel.Informational, Channel = EventChannel.Debug)]
        public void ProfileRemoveScheduled(string profileName) { WriteEvent(1117, profileName); }
        [Event(1118, Message = "Profile {0} is already up to date", Level = EventLevel.Informational, Channel = EventChannel.Operational)]
        public void ProfileAlreadyUpToDate(string profileName) { WriteEvent(1118, profileName); }
        [Event(1119, Message = "Profile {0} Monitoring Status Changed from {1} to {2}", Level = EventLevel.Informational, Channel = EventChannel.Operational)]
        public void MonitorProfileChangeRequested(string profile, bool oldValue, bool newValue) { WriteEvent(1119, profile, oldValue, newValue); }
        [Event(1120, Message = "Profile {0} Failed Unexpectedly \n Error: {1} \n StackTrace: {2}", Level = EventLevel.Error, Channel = EventChannel.Operational)]
        public void MonitorProfileFailed(string profile, string error, string stackTrace) { WriteEvent(1120, profile, error, stackTrace); }
        [Event(1121, Message = "Profile {0} Completed Unexpectedly, restarting...", Level = EventLevel.Warning, Channel = EventChannel.Operational)]
        public void MonitorProfileCompleted(string profile) { WriteEvent(1121, profile); }
        [Event(1124, Message = "Checking for unmanaged VPN Profiles", Level = EventLevel.Informational, Channel = EventChannel.Operational)]
        public void RemovingAllUnmanagedProfiles() { WriteEvent(1124); }
        [Event(1125, Message = "Profile Manager Update skipped as update already in progress", Level = EventLevel.Warning, Channel = EventChannel.Debug)]
        public void ProfileManagerUpdateSkipped() { WriteEvent(1125); }
        [Event(1126, Message = "Profile Manager Clear Existing Profiles skipped as update already in progress", Level = EventLevel.Warning, Channel = EventChannel.Debug)]
        public void ProfileManagerClearSkipped() { WriteEvent(1126); }
        [Event(1127, Message = "Profile Manager Clearing Existing Profiles", Level = EventLevel.Informational, Channel = EventChannel.Debug)]
        public void ProfileManagerClearProfiles() { WriteEvent(1127); }
        [Event(1128, Message = "Profile Manager checking for profile updates", Level = EventLevel.Informational, Channel = EventChannel.Debug)]
        public void ProfileManagerCheckForUpdates() { WriteEvent(1128); }
        [Event(1129, Message = "Profile Manager waiting on existing profile update", Level = EventLevel.Informational, Channel = EventChannel.Debug)]
        public void ProfileManagerWaitForProfileUpdate() { WriteEvent(1129); }
        [Event(1130, Message = "Profile Manager waiting on profile removal from registry", Level = EventLevel.Informational, Channel = EventChannel.Debug)]
        public void ProfileManagerWaitForProfileRemovalReg() { WriteEvent(1130); }
        [Event(1131, Message = "Profile Manager Remove Profiles skipped as update already in progress", Level = EventLevel.Warning, Channel = EventChannel.Debug)]
        public void ProfileManagerRemoveSkipped() { WriteEvent(1131); }
        [Event(1132, Message = "Unable to save currently installed profile data to disk: {0}\nErrors: {1}\nLocation: {2}", Level = EventLevel.Warning, Channel = EventChannel.Operational)]
        public void UpdateToSaveActualProfileToDisk(string profileName, string errors, string location) { WriteEvent(1132, profileName, errors, location); }
        [Event(1133, Message = "Unable to save currently installed profile data to disk: {0}\nErrors: {1}\nLocation: {2}\nStackTrace: {3}", Level = EventLevel.Warning, Channel = EventChannel.Operational)]
        public void UpdateToSaveActualProfileToDiskException(string profileName, string errors, string location, string stackTrace) { WriteEvent(1133, profileName, errors, location, stackTrace); }
        [Event(1134, Message = "Profile {0} is no longer required so will be removed", Level = EventLevel.Warning, Channel = EventChannel.Operational)]
        public void ProfileNoLongerNeeded(string profileName) { WriteEvent(1134, profileName); }
        [Event(1135, Message = "Profile Manager waiting on profile removal from WMI", Level = EventLevel.Informational, Channel = EventChannel.Debug)]
        public void ProfileManagerWaitForProfileRemovalWMI() { WriteEvent(1135); }
        [Event(1136, Message = "No Unmanaged Profiles found", Level = EventLevel.Informational, Channel = EventChannel.Operational)]
        public void NoProfilesToRemove() { WriteEvent(1136); }
        [Event(1137, Message = "Removing Unmanaged Profile {0}", Level = EventLevel.Informational, Channel = EventChannel.Operational)]
        public void RemovingProfile(string profileName) { WriteEvent(1137, profileName); }
        [Event(1138, Message = "Save and Debug paths are configured to identical values leading to profile information being overwritten.  Profile: {0}", Level = EventLevel.Warning, Channel = EventChannel.Operational)]
        public void ProfileSavePathIdentical(string profileName) { WriteEvent(1138, profileName); }
        [Event(1139, Message = "Unable to complete unmanaged profile removal \nErrors: {0} \nLocation: {1}", Level = EventLevel.Error, Channel = EventChannel.Operational)]
        public void UpdateToRemoveProfile(string errors, string location) { WriteEvent(1139, errors, location); }
        [Event(1140, Message = "Unable to clear AutoTriggerDisabledProfileList Registry Entry\nErrors: {0} \nLocation: {1}", Level = EventLevel.Error, Channel = EventChannel.Operational)]
        public void UpdateToDeleteRegistryEntryAutoTriggerDisabledProfileList(string errors, string location) { WriteEvent(1140, errors, location); }
        [Event(1141, Message = "Unable to Update Profile monitor\nErrors: {0} \nLocation: {1}", Level = EventLevel.Error, Channel = EventChannel.Operational)]
        public void UpdateMonitorStatusFailed(string errors, string location) { WriteEvent(1141, errors, location); }
        [Event(1142, Message = "Monitor Start Failed\nErrors: {0} \nLocation: {1}", Level = EventLevel.Error, Channel = EventChannel.Operational)]
        public void MonitorStartFailed(string errors, string location) { WriteEvent(1142, errors, location); }
        [Event(1143, Message = "Monitor Stop Failed\nErrors: {0} \nLocation: {1}", Level = EventLevel.Error, Channel = EventChannel.Operational)]
        public void MonitorStopFailed(string errors, string location) { WriteEvent(1143, errors, location); }
        [Event(1144, Message = "Profile Monitor Shutdown Failed\nErrors: {0} \nLocation: {1}", Level = EventLevel.Error, Channel = EventChannel.Operational)]
        public void MonitorProfileShutdownFailed(string errors, string location) { WriteEvent(1144, errors, location); }
        [Event(1145, Message = "Profile Monitor Startup Failed\nErrors: {0} \nLocation: {1}", Level = EventLevel.Error, Channel = EventChannel.Operational)]
        public void MonitorProfileStartupFailed(string errors, string location) { WriteEvent(1145, errors, location); }
        [Event(1146, Message = "VPN Monitor Creation Failed\nErrors: {0} \nLocation: {1}", Level = EventLevel.Error, Channel = EventChannel.Operational)]
        public void VPNMonitorCreationFailed(string errors, string location) { WriteEvent(1146, errors, location); }
        [Event(1147, Message = "VPN Monitor Startup Failed\nErrors: {0} \nLocation: {1}", Level = EventLevel.Error, Channel = EventChannel.Operational)]
        public void VPNMonitorStartupFailed(string errors, string location) { WriteEvent(1147, errors, location); }
        [Event(1148, Message = "Unable to Process Disconnect Event\nErrors: {0} \nLocation: {1}", Level = EventLevel.Error, Channel = EventChannel.Operational)]
        public void DisconnectionEventFailed(string errors, string location) { WriteEvent(1148, errors, location); }
        [Event(1149, Message = "Unable to Process Connection Event\nErrors: {0} \nLocation: {1}", Level = EventLevel.Error, Channel = EventChannel.Operational)]
        public void ConnectionEventFailed(string errors, string location) { WriteEvent(1149, errors, location); }
        [Event(1150, Message = "Update VPN Monitor Failed\nErrors: {0} \nLocation: {1}", Level = EventLevel.Error, Channel = EventChannel.Operational)]
        public void MonitorUpdateFailed(string errors, string location) { WriteEvent(1150, errors, location); }
        [Event(1151, Message = "VPN Monitor Shutdown Failed\nErrors: {0} \nLocation: {1}", Level = EventLevel.Error, Channel = EventChannel.Operational)]
        public void MonitorShutdownFailed(string errors, string location) { WriteEvent(1151, errors, location); }
        [Event(1152, Message = "Unable to remove whitespace\nErrors: {0} \nLocation: {1}", Level = EventLevel.Error, Channel = EventChannel.Operational)]
        public void UnableToRemoveWhiteSpace(string errors, string location) { WriteEvent(1152, errors, location); }
        [Event(1153, Message = "Cleaning up EnterpriseResourceManager entry for profile {0}", Level = EventLevel.Informational, Channel = EventChannel.Operational)]
        public void CleanUpEnterpriseResourceManager(string profileName) { WriteEvent(1153, profileName); }
        [Event(1154, Message = "Updating EnterpriseResourceManager count to {0}", Level = EventLevel.Informational, Channel = EventChannel.Debug)]
        public void UpdateEnterpriseResourceManagerCount(int count) { WriteEvent(1154, count); }
        [Event(1155, Message = "No VPN Profiles left in {0}, Removing key", Level = EventLevel.Informational, Channel = EventChannel.Debug)]
        public void RemoveEnterpriseResourceManagerKey(string key) { WriteEvent(1155, key); }
        [Event(1156, Message = "Cleaning up Network Profile entry for profile {0}", Level = EventLevel.Warning, Channel = EventChannel.Operational)]
        public void CleanUpNetworkProfile(string profileName) { WriteEvent(1153, profileName); }
        [Event(1157, Message = "Cleaning up AutoTriggerDisabledProfilesList entry for profile {0}", Level = EventLevel.Warning, Channel = EventChannel.Operational)]
        public void CleanUpAutoTriggerDisabledProfilesList(string profileName) { WriteEvent(1157, profileName); }
        [Event(1158, Message = "Profile {0} Failed to be Removed\nError: {1}\nStackTrace: {2}", Level = EventLevel.Warning, Channel = EventChannel.Operational)]
        public void IssueDeletingProfile(string profileName, string message, string stackTrace) { WriteEvent(1158, profileName, message, stackTrace); }
        [Event(1159, Message = "Failed to Update Metric for Profile {0}: {1}\nStackTrace: {2}", Level = EventLevel.Error, Channel = EventChannel.Operational)]
        public void IssueUpdatingMetric(string profileName, string errorMessage, string stackTrace) { WriteEvent(1159, profileName, errorMessage, stackTrace); }
        [Event(1160, Message = "Failed to Update VPNStrategy for Profile {0}: {1}\nStackTrace: {2}", Level = EventLevel.Error, Channel = EventChannel.Operational)]
        public void IssueUpdatingVPNStrategy(string profileName, string errorMessage, string stackTrace) { WriteEvent(1160, profileName, errorMessage, stackTrace); }
        [Event(1161, Message = "Profile {0} update Scheduled", Level = EventLevel.Informational, Channel = EventChannel.Operational)]
        public void ProfileScheduled(string profileName) { WriteEvent(1161, profileName); }
        [Event(1162, Message = "Updated Metric for Profile {0}", Level = EventLevel.Informational, Channel = EventChannel.Operational)]
        public void ProfileMetricUpdated(string profileName) { WriteEvent(1162, profileName); }
        [Event(1163, Message = "Metric for Profile {0} already correct", Level = EventLevel.Informational, Channel = EventChannel.Debug)]
        public void ProfileMetricNotUpdated(string profileName) { WriteEvent(1163, profileName); }
        [Event(1164, Message = "Updated VPN Strategy for Profile {0}", Level = EventLevel.Informational, Channel = EventChannel.Operational)]
        public void ProfileVPNStrategyUpdated(string profileName) { WriteEvent(1164, profileName); }
        [Event(1165, Message = "VPN Strategy for Profile {0} already correct", Level = EventLevel.Informational, Channel = EventChannel.Debug)]
        public void ProfileVPNStrategyNotUpdated(string profileName) { WriteEvent(1165, profileName); }
        [Event(1169, Message = "Updated Use Ras Credential for Profile {0}", Level = EventLevel.Informational, Channel = EventChannel.Operational)]
        public void ProfileUseRasCredentialsStatusUpdated(string profileName) { WriteEvent(1169, profileName); }
        [Event(1170, Message = "Use Ras Credential for Profile {0} already correct", Level = EventLevel.Informational, Channel = EventChannel.Debug)]
        public void ProfileUseRasCredentialsStatusNotUpdated(string profileName) { WriteEvent(1170, profileName); }
        [Event(1171, Message = "Failed to Update Use Ras Credential Status for Profile {0}: {1}\nStackTrace: {2}", Level = EventLevel.Error, Channel = EventChannel.Operational)]
        public void IssueUpdatingUseRasCredentialsStatus(string profileName, string errorMessage, string stackTrace) { WriteEvent(1171, profileName, errorMessage, stackTrace); }
        [Event(1172, Message = "Error getting VPN Strategy for Profile {0}: {1}\nStackTrace: {2}\nDefaulting to IKEv2 Only", Level = EventLevel.Error, Channel = EventChannel.Operational)]
        public void ErrorGettingVPNStrategy(string profileName, string errorMessage, string stackTrace) { WriteEvent(1172, profileName, errorMessage, stackTrace); }
        [Event(1173, Message = "Error getting VPN Metric for Profile {0}: {1}\nStackTrace: {2}", Level = EventLevel.Error, Channel = EventChannel.Operational)]
        public void ErrorGettingVPNMetric(string profileName, string errorMessage, string stackTrace) { WriteEvent(1173, profileName, errorMessage, stackTrace); }
        [Event(1174, Message = "Error getting Ras Credentials Status for Profile {0}: {1}\nStackTrace: {2}", Level = EventLevel.Error, Channel = EventChannel.Operational)]
        public void ErrorGettingDisableRasCredentials(string profileName, string errorMessage, string stackTrace) { WriteEvent(1174, profileName, errorMessage, stackTrace); }
        [Event(1175, Message = "Updated Network Outage Time for Profile {0}", Level = EventLevel.Informational, Channel = EventChannel.Operational)]
        public void ProfileNetworkOutageUpdated(string profileName) { WriteEvent(1175, profileName); }
        [Event(1176, Message = "Network Outage Time for Profile {0} already correct", Level = EventLevel.Informational, Channel = EventChannel.Debug)]
        public void ProfileNetworkOutageTimeNotUpdated(string profileName) { WriteEvent(1176, profileName); }
        [Event(1177, Message = "Failed to Update Network Outage Time for Profile {0}: {1}\nStackTrace: {2}", Level = EventLevel.Error, Channel = EventChannel.Operational)]
        public void IssueUpdatingNetworkOutageTime(string profileName, string errorMessage, string stackTrace) { WriteEvent(1177, profileName, errorMessage, stackTrace); }
        [Event(1178, Message = "Error getting Network Outage Time for Profile {0}: {1}\nStackTrace: {2}", Level = EventLevel.Error, Channel = EventChannel.Operational)]
        public void ErrorGettingNetworkOutageTime(string profileName, string errorMessage, string stackTrace) { WriteEvent(1178, profileName, errorMessage, stackTrace); }
        [Event(1179, Message = "Profile {0} Returned by Windows does not match the profile submitted", Level = EventLevel.Warning, Channel = EventChannel.Operational)]
        public void WarningProfilesDoNotMatch(string profileName) { WriteEvent(1179, profileName); }
        [Event(1180, Message = "Profile Manager Disabling VPN Strategy Overwrite", Level = EventLevel.Informational, Channel = EventChannel.Debug)]
        public void ProfileManagerDisableVPNStrategyOverwrite() { WriteEvent(1180); }
        [Event(1181, Message = "Unable to clear VPNStrategyUsageDisabled Registry Entry\nErrors: {0} \nLocation: {1}", Level = EventLevel.Error, Channel = EventChannel.Operational)]
        public void UpdateToDisableVPNStrategyOverwriteFailed(string errors, string location) { WriteEvent(1181, errors, location); }
        [Event(1182, Message = "Device Tunnel UI Already Configured as {0}", Level = EventLevel.Informational, Channel = EventChannel.Debug)]
        public void DeviceTunnelUIAlreadyCorrect(bool currentValue) { WriteEvent(1182, currentValue); }
        [Event(1183, Message = "Updated Device Tunnel UI to {0}", Level = EventLevel.Informational, Channel = EventChannel.Operational)]
        public void DeviceTunnelUIUpdated(bool currentValue) { WriteEvent(1183, currentValue); }
        [Event(1184, Message = "Unable to update Device Tunnel UI Registry Entry\nErrors: {0} \nLocation: {1}", Level = EventLevel.Error, Channel = EventChannel.Operational)]
        public void UnableToUpdateDeviceTunnelUI(string errors, string location) { WriteEvent(1184, errors, location); }
        [Event(1185, Message = "Device Tunnel UI Update skipped as update already in progress", Level = EventLevel.Warning, Channel = EventChannel.Debug)]
        public void ProfileManagerDeviceTunnelUIUpdateSkipped() { WriteEvent(1185); }
        [Event(1186, Message = "Profile {0} Returned by Windows does not match the profile submitted for variable {1}\n{2}", Level = EventLevel.Informational, Channel = EventChannel.Operational)]
        public void WarningProfilesDoNotMatchDetail(string profileName, string variable, string errorMessage) { WriteEvent(1186, profileName, variable, errorMessage); }
        [Event(1187, Message = "Profile {0} has different Interface Metrics for IPv4 and IPv6, IPv4 Metric will be used, IPv6 Metric will be discarded", Level = EventLevel.Warning, Channel = EventChannel.Operational)]
        public void WarningIPv4AndIPv6MetricsDoNotMatch(string profileName) { WriteEvent(1187, profileName); }
        [Event(1188, Message = "Updated Machine Certificate Filter EKU for Profile {0}", Level = EventLevel.Informational, Channel = EventChannel.Operational)]
        public void ProfileMachineEKUUpdated(string profileName) { WriteEvent(1188, profileName); }
        [Event(1189, Message = "Machine Certificate Filter EKU for Profile {0} already correct", Level = EventLevel.Informational, Channel = EventChannel.Debug)]
        public void ProfileMachineEKUNotUpdated(string profileName) { WriteEvent(1189, profileName); }
        [Event(1190, Message = "Failed to Update Machine Certificate Filter EKU for Profile {0}: {1}\nStackTrace: {2}", Level = EventLevel.Error, Channel = EventChannel.Operational)]
        public void IssueUpdatingMachineEKU(string profileName, string errorMessage, string stackTrace) { WriteEvent(1190, profileName, errorMessage, stackTrace); }
        [Event(1191, Message = "Updated Proxy Exclusions for Profile {0}", Level = EventLevel.Informational, Channel = EventChannel.Operational)]
        public void ProfileProxyExclusionsUpdated(string profileName) { WriteEvent(1191, profileName); }
        [Event(1192, Message = "Proxy Exclusions for Profile {0} already correct", Level = EventLevel.Informational, Channel = EventChannel.Debug)]
        public void ProfileProxyExclusionsNotUpdated(string profileName) { WriteEvent(1192, profileName); }
        [Event(1193, Message = "Failed to Update Proxy Exclusions for Profile {0}: {1}\nStackTrace: {2}", Level = EventLevel.Error, Channel = EventChannel.Operational)]
        public void IssueUpdatingProxyExclusions(string profileName, string errorMessage, string stackTrace) { WriteEvent(1193, profileName, errorMessage, stackTrace); }
        [Event(1194, Message = "Profile {0} Removed", Level = EventLevel.Informational, Channel = EventChannel.Operational)]
        public void ProfileProfileRemoved(string profileName) { WriteEvent(1194, profileName); }
        [Event(1195, Message = "No Profiles under management with {0} unmanaged profiles. DPC will not remove unmanaged profiles to avoid unexpected disconnects", Level = EventLevel.Warning, Channel = EventChannel.Operational)]
        public void ProfileRemovalBlocked(int profileCount) { WriteEvent(1195, profileCount); }
        [Event(1200, Message = "Updated MTU for Profile {0} {1} from {2} to {3}", Level = EventLevel.Informational, Channel = EventChannel.Operational)]
        public void ProfileMTUUpdated(string profileName, string IPInterface, uint oldMTU, uint mtu) { WriteEvent(1200, profileName, IPInterface, oldMTU, mtu); }
        [Event(1201, Message = "MTU for Profile {0} {1} already correct", Level = EventLevel.Informational, Channel = EventChannel.Debug)]
        public void ProfileMTUNotUpdated(string profileName, string IPInterface) { WriteEvent(1201, profileName, IPInterface); }
        [Event(1202, Message = "Failed to Update MTU for Profile {0} - {1}\n{2}\nStackTrace: {3}", Level = EventLevel.Error, Channel = EventChannel.Operational)]
        public void IssueUpdatingProfileMTU(string profileName, string IPInterface, string errorMessage, string stackTrace) { WriteEvent(1202, profileName, IPInterface, errorMessage, stackTrace); }
        [Event(1203, Message = "MTU for Profile {0} {1} not updated as interface cannot be found", Level = EventLevel.Informational, Channel = EventChannel.Debug)]
        public void ProfileMTUIsNull(string profileName, string IPInterface) { WriteEvent(1203, profileName, IPInterface); }
        [Event(1204, Message = "Group Policy Refresh Detected", Level = EventLevel.Informational, Channel = EventChannel.Operational)]
        public void GroupPolicyUpdated() { WriteEvent(1204); }
        [Event(1205, Message = "VPM MTU Update skipped as update already in progress", Level = EventLevel.Warning, Channel = EventChannel.Debug)]
        public void ProfileManagerUpdateVPNMTUSkipped() { WriteEvent(1205); }
        [Event(1206, Message = "Unable to update VPN MTU\nErrors: {0} \nLocation: {1}", Level = EventLevel.Error, Channel = EventChannel.Operational)]
        public void UnableToUpdateVPNMTU(string errors, string location) { WriteEvent(1206, errors, location); }
        [Event(1207, Message = "System VPN MTU is Already Configured as {0}", Level = EventLevel.Informational, Channel = EventChannel.Debug)]
        public void VPNMTUAlreadyCorrect(uint currentValue) { WriteEvent(1207, currentValue); }
        [Event(1208, Message = "System VPN MTU is Already Default", Level = EventLevel.Informational, Channel = EventChannel.Debug)]
        public void VPNMTUAlreadyNull() { WriteEvent(1208); }
        [Event(1209, Message = "System VPN MTU has been updated to {0}", Level = EventLevel.Informational, Channel = EventChannel.Operational)]
        public void VPNMTUUpdated(uint currentValue) { WriteEvent(1209, currentValue); }
        [Event(1210, Message = "Hidden Profile {0} Removed", Level = EventLevel.Informational, Channel = EventChannel.Operational)]
        public void ProfileHiddenProfileRemoved(string profileName) { WriteEvent(1210, profileName); }
        [Event(1211, Message = "Auto Trigger Profile Cleared", Level = EventLevel.Informational, Channel = EventChannel.Operational)]
        public void AutoTriggerProfileCleared() { WriteEvent(1211); }
        [Event(1212, Message = "Profile RasPhone configuration not found for profile {0}", Level = EventLevel.Warning, Channel = EventChannel.Operational)]
        public void UnableToLocatePBKProfile(string profileName) { WriteEvent(1212, profileName); }
        [Event(1213, Message = "Failed to Update Auto Trigger settings for Profile {0}: {1}\nStackTrace: {2}", Level = EventLevel.Error, Channel = EventChannel.Operational)]
        public void IssueUpdatingAutoTriggerRegistry(string profileName, string errorMessage, string stackTrace) { WriteEvent(1213, profileName, errorMessage, stackTrace); }
        [Event(1214, Message = "Restarting RasMan Service", Level = EventLevel.Warning, Channel = EventChannel.Operational)]
        public void RestartingRasManService() { WriteEvent(1214); }
        [Event(1215, Message = "Restarting RasMan Service Failed with error {0}\n{1}", Level = EventLevel.Warning, Channel = EventChannel.Operational)]
        public void RestartingRasManServiceFailed(string errorMessage, string stackTrace) { WriteEvent(1215, errorMessage, stackTrace); }
        [Event(1216, Message = "Restart of RasMan needed however existing connections prevent this", Level = EventLevel.Informational, Channel = EventChannel.Operational)]
        public void RasManRestartNeeded() { WriteEvent(1216); }
        [Event(1217, Message = "Restart RasMan Service Requested", Level = EventLevel.Informational, Channel = EventChannel.Operational)]
        public void RestartRasManServiceRequested() { WriteEvent(1217); }
        [Event(1218, Message = "MiniDump saved to: {0}", Level = EventLevel.Error, Channel = EventChannel.Operational)]
        public void MiniDumpSaved(string miniDumpPath) { WriteEvent(1218, miniDumpPath); }
        [Event(1219, Message = "MiniDump Failed to Save", Level = EventLevel.Error, Channel = EventChannel.Operational)]
        public void MiniDumpSaveFailed() { WriteEvent(1219); }
        [Event(1220, Message = "Unexpected failure in profile creation process: \nProfile Name: {0}\nError:\n{1}", Level = EventLevel.Error, Channel = EventChannel.Operational)]
        public void ProfileCreationFailedDebug(string profileName, string exception) { WriteEvent(1220, profileName, exception); }
        [Event(1221, Message = "Unable to delete Profile {0}\nError message: {1}\nStackTrace: {2}", Level = EventLevel.Warning, Channel = EventChannel.Operational)]
        public void IssueDeletingHiddenPbk(string profile, string message, string stackTrace) { WriteEvent(1221, profile, message, stackTrace); }
        [Event(1222, Message = "Corrupt PBK Profile identified in {0}, file deleted to avoid issues", Level = EventLevel.Warning, Channel = EventChannel.Operational)]
        public void CorruptPbkDeleted(string PBKPath) { WriteEvent(1222, PBKPath); }
        [Event(1223, Message = "Corrupt PBK Profile identified in {0}. Error deleting file: {1}", Level = EventLevel.Error, Channel = EventChannel.Operational)]
        public void CorruptPbkDeleteFailed(string PBKPath, string exception) { WriteEvent(1223, PBKPath, exception); }
        [Event(1224, Message = "Profile {0} Needs {1} updating because existing value {2}", Level = EventLevel.Informational, Channel = EventChannel.Debug)]
        public void ProfileDebugUpdateProfileDetail(string profileName, string variable, string errorMessage) { WriteEvent(1224, profileName, variable, errorMessage); }
        [Event(1225, Message = "No Corrupt PBK Files Found", Level = EventLevel.Informational, Channel = EventChannel.Debug)]
        public void DebugNoCorruptPbksFound() { WriteEvent(1225); }
        [Event(1226, Message = "Network Capabilities changed, forcing profile update to {0}", Level = EventLevel.Informational, Channel = EventChannel.Operational)]
        public void NetworkChangeProfileUpdate(string profileName) { WriteEvent(1226, profileName); }
        [Event(1227, Message = "Scheduled profile update for profile {0}", Level = EventLevel.Informational, Channel = EventChannel.Operational)]
        public void TimeBasedProfileUpdate(string profileName) { WriteEvent(1227, profileName); }
        [Event(1228, Message = "Group Policy Updated, updating profile {0}", Level = EventLevel.Informational, Channel = EventChannel.Operational)]
        public void GPOProfileUpdate(string profileName) { WriteEvent(1228, profileName); }
        //Event Logs now fail to generate if additional logs are added at this point in the file, adding to the end appears to work for some reason...
        #endregion 1100-1299 Profile Monitoring

        #region 2000-2099 Profile Monitoring
        [Event(2000, Message = "Starting Error Event Monitoring", Level = EventLevel.Informational, Channel = EventChannel.Debug)]
        public void ErrorEventMonitoringStarting() { WriteEvent(2000); }
        [Event(2001, Message = "Starting Error Event Monitoring", Level = EventLevel.Informational, Channel = EventChannel.Operational)]
        public void ErrorEventMonitoringStarted() { WriteEvent(2001); }
        [Event(2002, Message = "An Error occurred while Starting Error Event Monitoring\nError: {0}\nStackTrace:{1}", Level = EventLevel.Error, Channel = EventChannel.Operational)]
        public void ErrorEventMonitoringErrorStarting(string message, string stackTrace) { WriteEvent(2002, message, stackTrace); }
        [Event(2003, Message = "Stopping Error Event Monitoring", Level = EventLevel.Informational, Channel = EventChannel.Debug)]
        public void ErrorEventMonitoringStopping() { WriteEvent(2003); }
        [Event(2004, Message = "Error Event Monitoring Stopped", Level = EventLevel.Informational, Channel = EventChannel.Operational)]
        public void ErrorEventMonitoringStopped() { WriteEvent(2004); }
        [Event(2005, Message = "An Error occurred while Stopping Error Event Monitoring\nError: {0}\nStackTrace:{1}", Level = EventLevel.Error, Channel = EventChannel.Operational)]
        public void ErrorEventMonitoringErrorStopping(string message, string stackTrace) { WriteEvent(2005, message, stackTrace); }
        [Event(2006, Message = "Connection Failed for user {0} and Profile {1} with error ID {2} and error message {3}", Level = EventLevel.Warning, Channel = EventChannel.Operational)]
        public void EventMonitoringConnectionFailed(string userName, string profile, string errorId, string errorMessage) { WriteEvent(2006, userName, profile, errorId, errorMessage); }
        [Event(2007, Message = "Connection Failed but event is missing expected data", Level = EventLevel.Warning, Channel = EventChannel.Operational)]
        public void EventMonitoringConnectionFailedNoData() { WriteEvent(2007); }
        [Event(2008, Message = "Connection Failed but event is missing expected data. Property Count = {0}", Level = EventLevel.Warning, Channel = EventChannel.Operational)]
        public void EventMonitoringConnectionFailedUnknownProperties(int propertyCount) { WriteEvent(2008, propertyCount); }
        [Event(2009, Message = "Duplicate Connection Failed event detected with Disconnect Id: {0}", Level = EventLevel.Warning, Channel = EventChannel.Debug)]
        public void EventMonitoringConnectionFailedDuplicateEvent(uint disconnectId) { WriteEvent(2009, disconnectId); }
        #endregion 2000-2099 Profile Monitoring

        #region 9000-10000 Special events

        [Event(9000, Message = "Service halted due to error", Level = EventLevel.Critical, Channel = EventChannel.Admin)]
        public void CriticalHaltDueToError() { WriteEvent(9000); throw new CriticalException("Service Error Has Occurred, check Event log for more information"); }
        [Event(9002, Message = "WARNING: Client is compiled in DEBUG mode, performance may be impacted", Level = EventLevel.Warning, Channel = EventChannel.Admin)]
        public void DebugOn() { WriteEvent(9002); }
        [Event(9800, Message = "Unknown error, Error Function: {0} \nMessage {1} \nStack Trace {2}", Level = EventLevel.Error, Channel = EventChannel.Admin)]
        public void GenericErrorMessage(string codeLocation, string message, string stackTrace) { WriteEvent(9800, codeLocation, message, stackTrace); CriticalHaltDueToError(); }
        [Event(9005, Message = "Method {0} started for Identifier {1}", Level = EventLevel.Informational, Channel = EventChannel.Analytic)]
        public void TraceStartMethod(string methodName, string threadID) { WriteEvent(9005, methodName, threadID); }
        [Event(9006, Message = "Method {0} finished for Identifier {1}", Level = EventLevel.Informational, Channel = EventChannel.Analytic)]
        public void TraceMethodFinished(string methodName, string threadID) { WriteEvent(9006, methodName, threadID); }

        #endregion 9000+ Special events
    }
}