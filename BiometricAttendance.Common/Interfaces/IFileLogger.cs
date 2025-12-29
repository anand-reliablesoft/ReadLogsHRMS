namespace BiometricAttendance.Common.Interfaces
{
    public interface IFileLogger
    {
        void Initialize(string logDirectory, string prefix);
        void Log(string message);
        void LogError(string message, System.Exception ex);
        void Close();
    }
}
