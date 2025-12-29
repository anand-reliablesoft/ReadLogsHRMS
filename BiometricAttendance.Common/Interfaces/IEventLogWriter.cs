namespace BiometricAttendance.Common.Interfaces
{
    public interface IEventLogWriter
    {
        void WriteError(string message, int eventId);
        void WriteWarning(string message, int eventId);
        void WriteInformation(string message, int eventId);
    }
}
