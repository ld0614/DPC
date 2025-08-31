using Microsoft.Diagnostics.Tracing;
using System;
using System.IO;
using System.Linq;

namespace DPCService.Utils
{
    sealed class EventLogFileWriter : EventListener
    {
        private StreamWriter LogFile;

        private readonly string FileName;

        public EventLogFileWriter(string filePath)
        {
            //Turn values such as %TEMP% into actual file location
            filePath = Environment.ExpandEnvironmentVariables(filePath);

            if (Directory.Exists(filePath))
            {
                //Specified path is a directory and not a specific log name
                filePath = Path.Combine(filePath, "DPCLog.txt");
            }

            //Fully resolve the file Path to avoid ambiguity with log locations
            filePath = Path.GetFullPath(filePath);

            //Ensure that the Directory exists otherwise creating the file will fail
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            FileName = filePath;
            DPCServiceEvents.Log.FileLoggingPath(FileName);
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (null == LogFile)
            {
                //Event Sources can be registered prior to filePaths being defined and therefore leaving the Log File unopened
                OnEventSourceCreated(eventData.EventSource);
            }
            try
            {
                string eventMessage = string.Format(eventData.Message, eventData.Payload.ToArray()).Replace("\r","").Replace("\n","\\n"); //Resolve event parameters and ensure that the message is all on 1 line for log file consistency
                string logMessage = string.Format("{0} UTC\tEvent ID: {1}\tLog: {2}\tSeverity: {3}\tMessage: {4}", DateTime.UtcNow.ToString(), eventData.EventId, eventData.Channel, eventData.Level, eventMessage);
                LogFile?.WriteLine(logMessage);
            }
            catch
            {
                //Don't throw any errors relating to failing to write to the file log, this is a secondary logging mechanism and a failure to log shouldn't be the reason that primary functionality breaks
            }
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (string.IsNullOrEmpty(FileName)) {return; } //Do not attempt to set up logging prior to full class initialisation where FileName is defined
            LogFile = new StreamWriter(FileName)
            {
                AutoFlush = true
            };

            LogFile.WriteLine("Starting File Logging for " + eventSource.Name + " at " + DateTime.UtcNow.ToString() + " UTC");
        }

        public override void Dispose()
        {
            //Flush and dispose of the open log stream file to ensure that it gets written to disk prior to shutdown
            LogFile?.WriteLine(DateTime.UtcNow.ToString() + " UTC - File logging Stopped");
            LogFile?.Flush();
            LogFile?.Close();
            LogFile?.Dispose();
            LogFile = null;
            base.Dispose();
        }
    }
}
