using System;
using System.Diagnostics.Eventing.Reader;

namespace DPCService.Utils
{
    public static class EventMonitor
    {
        public delegate void EventCallback(object sender, EventRecordWrittenEventArgs e);

        private static EventLogWatcher eventWatcher;

        public static void WatchForRasManError(EventCallback callback)
        {
            DPCServiceEvents.Log.ErrorEventMonitoringStarting();
            EventLogSession session = new EventLogSession();

            EventLogQuery query = new EventLogQuery("Application", PathType.LogName, "*[System/Provider/@Name=\"RasClient\" and System/Level=2 and System/EventID=20227]")
            {
                TolerateQueryErrors = false,
                Session = session
            };

            eventWatcher = new EventLogWatcher(query);

            eventWatcher.EventRecordWritten += new EventHandler<EventRecordWrittenEventArgs>(callback);

            try
            {
                eventWatcher.Enabled = true;
                DPCServiceEvents.Log.ErrorEventMonitoringStarted();
            }
            catch (EventLogException ex)
            {
                DPCServiceEvents.Log.ErrorEventMonitoringErrorStarting(ex.Message, ex.StackTrace);
            }
        }

        public static void StopWatchForRasManError()
        {
            try
            {
                DPCServiceEvents.Log.ErrorEventMonitoringStopping();
                if (eventWatcher == null)
                    return;

                if (eventWatcher.Enabled)
                {
                    eventWatcher.Enabled = false;
                    eventWatcher.Dispose();
                    eventWatcher = null;
                    DPCServiceEvents.Log.ErrorEventMonitoringStopped();
                }
            }
            catch (EventLogException ex)
            {
                DPCServiceEvents.Log.ErrorEventMonitoringErrorStopping(ex.Message, ex.StackTrace);
            }
        }
    }
}
