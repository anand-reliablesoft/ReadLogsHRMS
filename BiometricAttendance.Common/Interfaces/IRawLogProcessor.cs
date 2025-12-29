using System.Data;
using BiometricAttendance.Common.Models;

namespace BiometricAttendance.Common.Interfaces
{
    /// <summary>
    /// Processes and saves raw attendance logs to databases
    /// </summary>
    public interface IRawLogProcessor
    {
        /// <summary>
        /// Saves raw log to both Access and SQL Server databases
        /// </summary>
        /// <param name="log">Attendance log to save</param>
        /// <param name="accessConn">Access database connection</param>
        /// <param name="sqlConn">SQL Server database connection</param>
        /// <param name="logger">File logger for logging operations</param>
        void SaveRawLog(AttendanceLog log, IDbConnection accessConn, IDbConnection sqlConn, IFileLogger logger);

        /// <summary>
        /// Checks if a log entry is a duplicate
        /// </summary>
        /// <param name="log">Attendance log to check</param>
        /// <param name="connection">Database connection</param>
        /// <returns>True if duplicate exists, false otherwise</returns>
        bool IsDuplicate(AttendanceLog log, IDbConnection connection);

        /// <summary>
        /// Sets the database connection manager for retry logic
        /// </summary>
        void SetConnectionManager(IDatabaseConnectionManager connectionManager);
    }
}
