using DPCLibrary.Enums;
using DPCLibrary.Utils;
using DPCService.Enums;
using DPCService.Models;
using DPCService.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

namespace DPCService.Core
{
    /// <summary>
    /// Main startup and shutdown methods, handles application initialization, license checking and Windows Service Startup
    /// </summary>
    internal partial class DPCService : ServiceBase
    {
        /// <summary>
        /// Primary Cancellation token, used to trigger child process cancellations
        /// </summary>
        private readonly CancellationTokenSource RootCancelToken = new CancellationTokenSource();

        /// <summary>
        /// List of all active services being performed by the application, tracked so that cancel can be called and monitored as required
        /// </summary>
        private static readonly List<Task> ServiceList = new List<Task>();

        /// <summary>
        /// To enable data to be shared between different threads the sharedData class is used
        /// </summary>
        private SharedData SharedData;

        public DPCService()
        {
            DPCServiceEvents.Log.DPCServiceInitializing();
            InitializeComponent();
            DPCServiceEvents.Log.DPCServiceInitialized();
        }

        /// <summary>
        /// Helper method to mimic the behavior of a Windows Service so that debugging can successfully take place
        /// </summary>
        /// <param name="args"></param>
        internal void TestStartupAndStop(string[] args)
        {
            OnStart(args);
            string value;
            do
            {
                Console.WriteLine("Startup Complete, Type 'quit' to Shutdown...");
                value = Console.ReadLine();
            } while (value.ToUpperInvariant() != "QUIT");
            OnStop();
        }

        /// <summary>
        /// Read initial registry values (only checked at application startup so changes require a service restart), start threads to handle application
        /// capabilities and handle licensing related activities.
        /// </summary>
        /// <param name="args">Arguments are ignored by this application as all configuration is done via Registry Settings</param>
        protected override void OnStart(string[] args)
        {
            try
            {

                DPCServiceEvents.Log.Startup();
                //DPCServiceEvents.Log.PrereleaseNotification(); //Product has been released and is no longer not supported
                Version clientVersion = AppSettings.GetProductVersion();
                bool debug = false;
#if DEBUG
                //Log the fact that this is a debug build
                debug = true;
#endif

                DPCServiceEvents.Log.ProductVersion(clientVersion.ToString(), debug, DeviceInfo.GetOSVersion().ToString());

                if (!DeviceInfo.IsUserAdministrator())
                {
                    DPCServiceEvents.Log.UserNotAdmin();
                }

                // Update the service state to Start Pending.
                ServiceStatus serviceStatus = new ServiceStatus
                {
                    dwCurrentState = ServiceState.SERVICE_START_PENDING,
                    dwWaitHint = 100000
                };

                //Get initial application configuration settings
                uint? refreshTime = AccessRegistry.ReadMachineUInt32(RegistrySettings.RefreshPeriod);
                if (refreshTime == null)
                {
                    refreshTime = 60;
                    DPCServiceEvents.Log.MissingRegistryEntry(RegistrySettings.RefreshPeriod, "60 minutes");
                }
                else if (refreshTime <= 0)
                {
                    refreshTime = 60;
                    DPCServiceEvents.Log.InvalidRegistryEntry(RegistrySettings.RefreshPeriod, "60 minutes");
                }

                bool migrationBlock = AccessRegistry.ReadMachineBoolean(RegistrySettings.MigrationBlock, false);
                if (migrationBlock)
                {
                    DPCServiceEvents.Log.MigrationBlockEnabled();
                }
                bool updateOnUnmanagedConnection = !migrationBlock; //Migration Block is a negative (true to block) where as the code needs the positive (true to unblock)

                int refreshTimeInMS = (int)refreshTime * 60 * 1000; //minutes * 60 = seconds * 1000 = milliseconds
                SharedData = new SharedData(refreshTimeInMS, updateOnUnmanagedConnection, true, RootCancelToken.Token);

                //Startup required services
                DPCServiceEvents.Log.LoadingServiceComponents();
                MonitorVPN VPNMon = new MonitorVPN(SharedData, RootCancelToken.Token);
                ServiceList.Add(new TaskFactory().StartNew(() => VPNMon.MonitorStart()));

                //Load Profile Management
                ProfileManager profileManager = new ProfileManager(SharedData, RootCancelToken.Token);
                ServiceList.Add(new TaskFactory().StartNew(() => profileManager.ManagerStartup()));

                // Update the service state to Running.
                serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING;
                NativeMethods.SetServiceStatus(ServiceHandle, ref serviceStatus);

                DPCServiceEvents.Log.StartupComplete();
            }
            catch (Exception e)
            {
                if (SharedData == null || SharedData.DumpOnException)
                {
                    MiniDump.Write();
                }

                DPCServiceEvents.Log.GenericErrorMessage("Service Start", e.Message, e.StackTrace);
            }
        }

        /// <summary>
        /// This is the method automatically called when a Windows Service is asked to stop or restart. It tries to cancel all existing processes, wait for
        /// them to stop and then gracefully return control to the OS.
        /// </summary>
        protected override void OnStop()
        {
            try
            {
                DPCServiceEvents.Log.Shutdown();
                // Update the service state to Start Pending.
                ServiceStatus serviceStatus = new ServiceStatus
                {
                    dwCurrentState = ServiceState.SERVICE_STOP_PENDING,
                    dwWaitHint = 100000
                };
                NativeMethods.SetServiceStatus(ServiceHandle, ref serviceStatus);

                //Cancel all child threads
                DPCServiceEvents.Log.CancelChildServices();
                RootCancelToken.Cancel();

                //Wait for all child threads to stop
                foreach (Task t in ServiceList)
                {
                    t.Wait();
                }
                DPCServiceEvents.Log.CancelChildServicesCompleted();

                // Update the service state to Stopped.
                serviceStatus.dwCurrentState = ServiceState.SERVICE_STOPPED;
                NativeMethods.SetServiceStatus(ServiceHandle, ref serviceStatus);
                DPCServiceEvents.Log.ShutdownCompleted();
            }
            catch (Exception e)
            {
                if (SharedData == null || SharedData.DumpOnException)
                {
                    MiniDump.Write();
                }
                //Attempt to close the service down
                ServiceStatus serviceStatus = new ServiceStatus
                {
                    dwCurrentState = ServiceState.SERVICE_STOPPED,
                    dwWaitHint = 100000
                };
                NativeMethods.SetServiceStatus(ServiceHandle, ref serviceStatus);

                StackTrace st = new StackTrace(e);
                DPCServiceEvents.Log.GenericErrorMessage("Service Stop, Exception Type: " + e.GetType().ToString(), e.Message, st.ToString());
            }
        }
    }
}