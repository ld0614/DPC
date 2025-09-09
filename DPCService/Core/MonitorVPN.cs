using DPCLibrary.Enums;
using DPCLibrary.Utils;
using DPCService.Models;
using DPCService.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace DPCService.Core
{
    /// <summary>
    /// This class operates the high level monitoring functions relating to monitoring VPN connections. We need to monitor (and work out) which
    /// connections are live to avoid updating a profile when it is in use. This is becuase its not possible to update a connected profile (causing
    /// an error) and it would disrupt a user if we forced the profile to disconnect.
    ///
    /// There is a concept of 'lite monitoring' where only the features required for the application to function are enabled rather than all monitoring.
    /// </summary>
    internal class MonitorVPN
    {
        private readonly SharedData SharedData;
        private CancellationTokenSource MonitorCancelToken = new CancellationTokenSource();
        private readonly CancellationToken RootToken;
        private static readonly List<Task> MonitorList = new List<Task>();
        private string PreviousEventCollationId;
        private bool CurrentlyUsingIPv6 = false;

        /// <summary>
        /// Initialize (but not start) the monitoring thread
        /// </summary>
        /// <param name="sharedData">Reference to the shared data class used for data synchronization</param>
        /// <param name="fullMonitoring">Specifies if the application should be in full monitoring or lite mode</param>
        /// <param name="token">Enables the thread to close correctly when the application is stopping</param>
        public MonitorVPN(SharedData sharedData, CancellationToken token)
        {
            try
            {
                SharedData = sharedData;

                token.Register(TokenCancelled);
                RootToken = token;
            }
            catch (Exception e)
            {
                DPCServiceEvents.Log.VPNMonitorCreationFailed(e.Message, e.StackTrace);
            }
        }

        /// <summary>
        /// Starts up the required monitoring threads and configures an update timer to check if the monitoring type has changed.
        ///
        /// Events are monitored by requesting a method callback when a specific event is raised, each event triggers a method to handle
        /// this type of event.
        /// </summary>
        public void MonitorStart()
        {
            try
            {
                if (RootToken.IsCancellationRequested)
                {
                    //Program is trying to shutdown, it should not be creating new threads
                    return;
                }

                if (MonitorCancelToken.IsCancellationRequested)
                {
                    MonitorCancelToken.Dispose();
                    MonitorCancelToken = new CancellationTokenSource();
                }

                DPCServiceEvents.Log.StartVPNMonitoringFull();
                MonitorList.Add(new TaskFactory().StartNew(() => AccessRasApi.Start(ConnectionEvent.RASCN_Connection, MonitorCancelToken.Token, ProcessConnectionEvent)));
                MonitorList.Add(new TaskFactory().StartNew(() => AccessRasApi.Start(ConnectionEvent.RASCN_Disconnection, MonitorCancelToken.Token, ProcessDisconnectionEvent)));
                MonitorList.Add(new TaskFactory().StartNew(() => AccessRasApi.Start(ConnectionEvent.RASCN_ReConnection, MonitorCancelToken.Token, ProcessReconnectionEvent)));

                NetworkChange.NetworkAddressChanged += new NetworkAddressChangedEventHandler(AddressChangedCallback);

                //Get initial application configuration settings
                bool restartOnPortAlreadyOpen = AccessRegistry.ReadMachineBoolean(RegistrySettings.RestartOnPortAlreadyOpen, false);
                if (restartOnPortAlreadyOpen)
                {
                    EventMonitor.WatchForRasManError(ConnectionFailureLogRestartOnPortAlreadyOpen);
                }
                else
                {
                    EventMonitor.WatchForRasManError(ConnectionFailureLog);
                }

                //Reset the Connected Profile List
                SharedData.SetConnectedVPNList(AccessRasApi.ListConnectedProfiles());
            }
            catch (Exception e)
            {
                DPCServiceEvents.Log.VPNMonitorStartupFailed(e.Message, e.StackTrace);
            }
        }

        private void ProcessConnectionEvent()
        {
            try
            {
                IList<string> newConnectionList;
                try
                {
                    newConnectionList = AccessRasApi.ListConnectedProfiles();
                }
                catch (Exception e)
                {
                    DPCServiceEvents.Log.UnableToGetConnectedProfiles(e.Message);
                    newConnectionList = new List<string>();
                }

                IList<string> newConnections;

                newConnections = SharedData.UpdateConnectionList(newConnectionList, true);

                if (newConnections.Count >= 1)
                {
                    foreach (string profile in newConnections)
                    {
                        DPCServiceEvents.Log.NewVPNConnectionEvent(profile);
                    }
                }
                else
                {
                    DPCServiceEvents.Log.NewVPNUnknownConnectionEvent();
                }

                //Update MTU values on all connected, managed Profiles
                SharedData.HandleConnectedProfileUpdate();
            }
            catch (Exception e)
            {
                DPCServiceEvents.Log.ConnectionEventFailed(e.Message, e.StackTrace);
            }
        }

        private void ProcessDisconnectionEvent()
        {
            try
            {
                IList<string> newConnectionList;
                try
                {
                    newConnectionList = AccessRasApi.ListConnectedProfiles();
                }
                catch (Exception e)
                {
                    DPCServiceEvents.Log.UnableToGetConnectedProfiles(e.Message);
                    newConnectionList = new List<string>();
                }

                IList<string> newConnections = SharedData.UpdateConnectionList(newConnectionList, false);

                SharedData.HandleProfileUpdates();

                //Only log if in full monitoring mode
                if (newConnections.Count >= 1)
                {
                    foreach (string profile in newConnections)
                    {
                        DPCServiceEvents.Log.NewVPNDisconnectionEvent(profile);
                    }
                }
                else
                {
                    DPCServiceEvents.Log.NewVPNUnknownDisconnectionEvent();
                }

                //Update MTU values on all connected, managed Profiles as IPv6 seems to reset whenever a connection gets disconnected
                SharedData.HandleConnectedProfileUpdate();
            }
            catch (Exception e)
            {
                DPCServiceEvents.Log.DisconnectionEventFailed(e.Message, e.StackTrace);
            }
        }

        private void ProcessReconnectionEvent()
        {
            DPCServiceEvents.Log.NewVPNReconnectionEvent();
            //Update MTU values on all connected, managed Profiles as IPv6 seems to reset whenever a connection gets disconnected
            SharedData.HandleConnectedProfileUpdate();
        }

        private void LogConnectionFailure(EventRecordWrittenEventArgs e, uint disconnectID)
        {
            string disconnectReason = "Unknown Reason";
            try
            {
                disconnectReason = Enum.GetName(typeof(RasError), disconnectID);
            }
            catch
            {
                //Hide error as the default will show Unknown Reason
            }

            DPCServiceEvents.Log.EventMonitoringConnectionFailed((string)e.EventRecord.Properties[1].Value, (string)e.EventRecord.Properties[2].Value, (string)e.EventRecord.Properties[3].Value, disconnectReason);
        }

        private uint ValidateConnectionFailureLog(EventRecordWrittenEventArgs e)
        {
            if (e.EventRecord == null || e.EventRecord.Properties == null)
            {
                DPCServiceEvents.Log.EventMonitoringConnectionFailedNoData();
                return 0;
            }

            if (e.EventRecord.Properties.Count != 4)
            {
                DPCServiceEvents.Log.EventMonitoringConnectionFailedUnknownProperties(e.EventRecord.Properties.Count);
                return 0;
            }

            uint disconnectID = 0;
            try
            {
                disconnectID = uint.Parse((string)e.EventRecord.Properties[3].Value);
            }
            catch
            {
                //Hide error as the default will show Unknown Reason
            }

            string collationId = (string)e.EventRecord.Properties[0].Value;
            if (collationId == PreviousEventCollationId)
            {
                DPCServiceEvents.Log.EventMonitoringConnectionFailedDuplicateEvent(disconnectID);
                return 0;
            }

            PreviousEventCollationId = collationId;

            return disconnectID;
        }

        public void ConnectionFailureLog(object sender, EventRecordWrittenEventArgs e)
        {
            uint disconnectId = ValidateConnectionFailureLog(e);

            if (disconnectId != 0)
            {
                LogConnectionFailure(e, disconnectId);
            }
        }

        public void ConnectionFailureLogRestartOnPortAlreadyOpen(object sender, EventRecordWrittenEventArgs e)
        {
            uint disconnectId = ValidateConnectionFailureLog(e);

            if (disconnectId != 0)
            {
                LogConnectionFailure(e, disconnectId);
            }

            if ((RasError)disconnectId == RasError.ERROR_PORT_ALREADY_OPEN)
            {
                SharedData.RequestRasManRestart();
            }
        }

        private void AddressChangedCallback(object sender, EventArgs e)
        {
            IList<NetworkInterface> adapters = AccessNetInterface.GetLocalNetworkInterfaces();

            if (adapters.Where(n => AccessNetInterface.InterfaceHasIPv6Gateway(n)).Count() > 0)
            {
                if (!CurrentlyUsingIPv6)
                {
                    DPCServiceEvents.Log.NetworkChangeIPv6GatewayDetected();
                    CurrentlyUsingIPv6 = true;
                    SharedData.EnableIPv6Routes(true);
                }
                else
                {
                    DPCServiceEvents.Log.NetworkChangeNoChangeNeeded();
                }
            }
            else
            {
                if (CurrentlyUsingIPv6)
                {
                    DPCServiceEvents.Log.NetworkChangeNoIPv6GatewayDetected();
                    CurrentlyUsingIPv6 = false;
                    SharedData.EnableIPv6Routes(false);
                }
                else
                {
                    DPCServiceEvents.Log.NetworkChangeNoChangeNeeded();
                }
            }
        }

        private void TokenCancelled()
        {
            try
            {
                DPCServiceEvents.Log.StoppingVPNMonitoring();

                //Cancel all subThreads
                MonitorCancelToken.Cancel();

                EventMonitor.StopWatchForRasManError();

                //Wait for all child threads to stop
                foreach (Task t in MonitorList)
                {
                    try
                    {
                        t.Wait();
                    }
                    catch (Exception e)
                    {
                        DPCServiceEvents.Log.VPNMonitorErrorOnWait(e.Message, e.StackTrace);
                    }
                }
                MonitorList.Clear();
                DPCServiceEvents.Log.VPNMonitoringStopped();
            }
            catch (Exception e)
            {
                DPCServiceEvents.Log.MonitorShutdownFailed(e.Message, e.StackTrace);
            }
        }
    }
}