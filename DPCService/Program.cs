using DPCLibrary.Enums;
using DPCLibrary.Utils;
using DPCService.Utils;
using Microsoft.Diagnostics.Tracing;
using System;
using System.IO;
using System.ServiceProcess;

namespace DPCService
{
    /// <summary>
    /// This is the primary entry point for the application and handles initial startup processes
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application. Depending on how the application is launched different startup processes are called
        /// to support launching in the optimal way (its not possible to start a service outside of a registered service for example)
        /// </summary>
        private static void Main(string[] args)
        {
            EventListener genericListener = null;
            try
            {
                string eventLogPath = AccessRegistry.ReadMachineString(RegistrySettings.EventLogPath);
                if (!string.IsNullOrEmpty(eventLogPath)) //If Event Log Path is not defined, do not save event logs to a file
                {
                    int eventLogType = AccessRegistry.ReadMachineInt32(RegistrySettings.EventLogFilter, 4); //Default to Informational messages - in line with Event Viewer
                    EventLevel eventLevel = (EventLevel)eventLogType;

                    genericListener = new EventLogFileWriter(eventLogPath);
                    genericListener.EnableEvents(DPCServiceEvents.Log, eventLevel);
                }
            }
            catch (Exception ex)
            {
                DPCServiceEvents.Log.FileLoggingConfigError(ex.Message);
            }
#if SYSATTACH
            //Used to debug under system privileges.  Launch the application using PSExec and then attach VS to the process
            string startProg = null;
            do
            {
                DPCServiceEvents.Log.SYSATTACHEnabled();
                Console.WriteLine("Awaiting Debug Attach, Type 'start' to continue");
                try
                {
                    //Console.ReadLine(); //Absorb starting key
                    startProg = Reader.ReadLine(10000);
                }
                catch (System.TimeoutException)
                {
                    startProg = null;
                }
            } while (startProg == null || startProg.ToLower() != "start");
            StartNoService(args);
#elif DEBUG
            //In normal Debug mode start the service if called without VS but launch manually if using VS
            if (Environment.UserInteractive)
            {
                DPCServiceEvents.Log.InteractiveModeEnabled();
                StartNoService(args);
            }
            else
            {
                DPCServiceEvents.Log.ServiceModeEnabled();
                StartService();
            }
#else
            //In release mode always try and launch as a service, this stops VS debugging from working
            DPCServiceEvents.Log.ServiceModeEnabled();
            StartService();
#endif
            try
            {
                genericListener?.Dispose();
            }
            catch (Exception ex)
            {
                DPCServiceEvents.Log.FileLoggingDisposeError(ex.Message);
            }
        }

        /// <summary>
        /// Start the application as a Windows Service. For this to work the application must have been previously registered as a
        /// Windows Service (Which is done in production by the installer)
        /// </summary>
        private static void StartService()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new Core.DPCService()
            };
            ServiceBase.Run(ServicesToRun);
        }

        /// <summary>
        /// Start the application without relying on the Windows Service backend. Used for testing and debugging
        /// </summary>
        /// <param name="args">Arguments are ignored by this application as all configuration is done via Registry Settings</param>
        private static void StartNoService(string[] args)
        {
            //Manually start the service, install event logs for easy monitoring
            EventLogInstaller.CreateEventLog();
            using (Core.DPCService coreService = new Core.DPCService())
            {
                coreService.TestStartupAndStop(args);
            }
            EventLogInstaller.RemoveEventLog();
        }
    }
}