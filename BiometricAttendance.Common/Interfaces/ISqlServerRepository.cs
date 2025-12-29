using System.Collections.Generic;
using System.Data;
using BiometricAttendance.Common.Models;

namespace BiometricAttendance.Common.Interfaces
{
    /// <summary>
    /// Repository for SQL Server database operations
    /// </summary>
    public interface ISqlServerRepository
    {
        /// <summary>
        /// Executes a query and returns results
        /// </summary>
        DataTable ExecuteQuery(string sql, IDbConnection connection);

        /// <summary>
        /// Executes a non-query command (INSERT, UPDATE, DELETE)
        /// </summary>
        int ExecuteNonQuery(string sql, IDbConnection connection);

        /// <summary>
        /// Checks if a record exists
        /// </summary>
        bool RecordExists(string sql, IDbConnection connection);

        /// <summary>
        /// Inserts a raw log entry with duplicate check
        /// </summary>
        void InsertRawLog(AttendanceLog log, IDbConnection connection);

        /// <summary>
        /// Inserts an attendance record with duplicate check
        /// </summary>
        void InsertAttendanceRecord(AttendanceRecord record, IDbConnection connection);

        /// <summary>
        /// Updates the transfer flag for a raw log record
        /// </summary>
        void UpdateRawLogTransferFlag(int id, IDbConnection connection);

        /// <summary>
        /// Gets unprocessed raw logs sorted chronologically
        /// </summary>
        IEnumerable<AttendanceLog> GetUnprocessedRawLogs(IDbConnection connection);

        /// <summary>
        /// Sets the database connection manager for retry logic
        /// </summary>
        void SetConnectionManager(IDatabaseConnectionManager connectionManager);
    }
}
