using System.Data;
using BiometricAttendance.Common.Models;

namespace BiometricAttendance.Common.Interfaces
{
    /// <summary>
    /// Repository for Access database operations
    /// </summary>
    public interface IAccessDatabaseRepository
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
        /// Gets employee ID from M_Executive table by BioID
        /// </summary>
        string GetEmployeeId(int bioId, IDbConnection connection);

        /// <summary>
        /// Sets the database connection manager for retry logic
        /// </summary>
        void SetConnectionManager(IDatabaseConnectionManager connectionManager);
    }
}
