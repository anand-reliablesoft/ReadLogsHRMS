using System.Data;

namespace BiometricAttendance.Common.Interfaces
{
    /// <summary>
    /// Processes raw logs into attendance records
    /// </summary>
    public interface IAttendanceRecordProcessor
    {
        /// <summary>
        /// Processes unprocessed raw logs and creates attendance records
        /// </summary>
        /// <param name="accessConn">Access database connection</param>
        /// <param name="sqlConn">SQL Server database connection</param>
        /// <param name="logger">File logger for logging operations</param>
        void ProcessRawLogs(IDbConnection accessConn, IDbConnection sqlConn, IFileLogger logger);

        /// <summary>
        /// Sets the database connection manager for retry logic
        /// </summary>
        void SetConnectionManager(IDatabaseConnectionManager connectionManager);
    }
}
