using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Xml;

namespace DPCService.Utils
{
    public static class EventLogInstaller
    {
        private const string EventSourceName = "DPCService.DPC-AOVPN-DPCService.etwManifest";
        private static StreamWriter logWriter;

        private static void WriteLog(string logEntry)
        {
            if (logWriter == null)
            {
                logWriter = new StreamWriter(Path.GetTempFileName(), true);
            }

            logWriter.WriteLine(logEntry);
            logWriter.Flush();
        }

        public static void CreateEventLog()
        {
            //Register the Event Logs
            string currentEXEDirectory = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().GetName().CodeBase).LocalPath);
            string eventManPath = Path.Combine(currentEXEDirectory, EventSourceName + ".man");
            string eventDLLPath = Path.Combine(currentEXEDirectory, EventSourceName + ".dll");
            GrantAccess(eventManPath);
            GrantAccess(eventDLLPath);
            RunWevtutil("install-manifest \"" + eventManPath + "\" /resourceFilePath:\"" + eventDLLPath + "\" /messageFilePath:\"" + eventDLLPath + "\"");
#if DEBUG
            DPCServiceEvents.Log.DebugOn();
            //Enable the debug log on creation automatically when compiled in debug mode
            EnableLog("DPC-AOVPN-DPCService/Debug");
            EnableLog("DPC-AOVPN-DPCService/Analytic");
#endif

            WriteLog("Event Log Install complete");

            return;
        }

        private static void GrantAccess(string fullPath)
        {
            DirectoryInfo dInfo = new DirectoryInfo(fullPath);
            DirectorySecurity dSecurity = dInfo.GetAccessControl();
            dSecurity.AddAccessRule(new FileSystemAccessRule(
                new SecurityIdentifier(WellKnownSidType.LocalSid, null),
                FileSystemRights.ReadAndExecute,
                InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit,
                PropagationFlags.NoPropagateInherit,
                AccessControlType.Allow));

            dInfo.SetAccessControl(dSecurity);
        }

        public static string RunWevtutil(string argument)
        {
            string output = "";
            try
            {
                using (Process proc = new Process())
                {
                    proc.EnableRaisingEvents = true;
                    proc.StartInfo = GenerateStartInfo("wevtutil", argument);

                    proc.Start();
                    proc.WaitForExit();

                    output = proc.StandardOutput.ReadToEnd();

                    LogProcessOutput(
                        proc.StartInfo.FileName + " " + proc.StartInfo.Arguments,
                        output,
                        proc.StandardError.ReadToEnd()
                    );
                }
            }
            catch (Exception e)
            {
                WriteLog("Error Occurred while running wevtutil: " + e.Message);
            }

            return output;
        }

        private static ProcessStartInfo GenerateStartInfo(string processName, string arguments)
        {
            string currentEXEDirectory = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().GetName().CodeBase).LocalPath);

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                FileName = processName,
                CreateNoWindow = true,
                WorkingDirectory = currentEXEDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                Arguments = arguments
            };
            return startInfo;
        }

        private static void EnableLog(string logName)
        {
            if (!GetLogState(logName))
            {
                RunWevtutil("set-log \"" + logName + "\" /enabled:true /quiet:true /retention:true");
            }
            else
            {

                WriteLog(logName + " already enabled");
            }
        }

        private static bool GetLogState(string logName)
        {
            string LogStatusXML = RunWevtutil("get-log \"" + logName + "\" /format:XML");

            try
            {
                XmlDocument xml = new XmlDocument();
                xml.LoadXml(LogStatusXML);
                return bool.Parse(xml.FirstChild.NextSibling.Attributes["enabled"]?.Value);
            }
            catch (Exception e)
            {
                WriteLog("Unable to get the state of " + logName);
                WriteLog("Error message: " + e.Message);
            }

            return false;
        }

        public static void RemoveEventLog()
        {

            WriteLog("Removing Event Log");


            //Remove the Event Logs
            string currentEXEDirectory = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().GetName().CodeBase).LocalPath);
            string eventManPath = Path.Combine(currentEXEDirectory, EventSourceName + ".man");
            RunWevtutil("uninstall-manifest \"" + eventManPath + "\"");


            WriteLog("Event Log removal complete");

            return;
        }

        private static void LogProcessOutput(string cmdline, string output, string error)
        {
            WriteLog("Results for " + cmdline);
            if (!string.IsNullOrWhiteSpace(output))
            {
                WriteLog("\tOutput: ");
                WriteLog("\t\t" + output);
            }

            if (!string.IsNullOrWhiteSpace(error))
            {
                WriteLog("\tError: ");
                WriteLog("\t\t" + error);
            }
        }
    }
}
