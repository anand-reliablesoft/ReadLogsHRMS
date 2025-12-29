using System;
using System.Diagnostics;
using BiometricAttendance.Common.Interfaces;

namespace BiometricAttendance.Common.Services
{
    public class EventLogWriter : IEventLogWriter
    {
        private const string EventLogSource = "BiometricAttendanceSystem";
        private const string EventLogName = "Application";

        public EventLogWriter()
        {
            // Ensure the event source exists
            try
            {
                if (!EventLog.SourceExists(EventLogSource))
                {
                    EventLog.CreateEventSource(EventLogSource, EventLogName);
                }
            }
            catch (Exception)
            {
                // If we can't create the source (requires admin rights), 
                // we'll fall back to using "Application" as the source
            }
        }

        public void WriteError(string message, int eventId)
        {
            WriteEntry(message, EventLogEntryType.Error, eventId);
        }

        public void WriteWarning(string message, int eventId)
        {
            WriteEntry(message, EventLogEntryType.Warning, eventId);
        }

        public void WriteInformation(string message, int eventId)
        {
            WriteEntry(message, EventLogEntryType.Information, eventId);
        }

        private void WriteEntry(string message, EventLogEntryType entryType, int eventId)
        {
            try
            {
                string source = EventLog.SourceExists(EventLogSource) ? EventLogSource : "Application";
                EventLog.WriteEntry(source, message, entryType, eventId);
            }
            catch (Exception ex)
            {
                // If we can't write to event log, silently fail
                // This prevents cascading failures in logging infrastructure
                Debug.WriteLine($"Failed to write to event log: {ex.Message}");
            }
        }
    }
}
