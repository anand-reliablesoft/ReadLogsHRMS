using System;
using System.Configuration;
using System.Data;
using System.Data.OleDb;
using BiometricAttendance.Common.Interfaces;
using BiometricAttendance.Common.Models;

namespace BiometricAttendance.Common.Services
{
    /// <summary>
    /// Repository for Access database operations
    /// </summary>
    public class AccessDatabaseRepository : IAccessDatabaseRepository
    {
        private readonly int _backYearBlocked;
        private IDatabaseConnectionManager _connectionManager;

        public AccessDatabaseRepository()
        {
            // Read BackYearBlocked configuration (default to 2023)
            string backYearConfig = ConfigurationManager.AppSettings["BackYearBlocked"] ?? "2023";
            _backYearBlocked = int.Parse(backYearConfig);
        }

        /// <summary>
        /// Sets the database connection manager for retry logic
        /// </summary>
        public void SetConnectionManager(IDatabaseConnectionManager connectionManager)
        {
            _connectionManager = connectionManager;
        }

        /// <summary>
        /// Executes a query and returns results
        /// </summary>
        public DataTable ExecuteQuery(string sql, IDbConnection connection)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            var dataTable = new DataTable();
            
            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                
                using (var adapter = new OleDbDataAdapter((OleDbCommand)command))
                {
                    adapter.Fill(dataTable);
                }
            }

            return dataTable;
        }

        /// <summary>
        /// Executes a non-query command (INSERT, UPDATE, DELETE)
        /// </summary>
        public int ExecuteNonQuery(string sql, IDbConnection connection)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                return command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Checks if a record exists
        /// </summary>
        public bool RecordExists(string sql, IDbConnection connection)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                var result = command.ExecuteScalar();
                
                if (result == null || result == DBNull.Value)
                    return false;

                return Convert.ToInt32(result) > 0;
            }
        }

        /// <summary>
        /// Inserts a raw log entry with duplicate check
        /// </summary>
        public void InsertRawLog(AttendanceLog log, IDbConnection connection)
        {
            if (log == null)
                throw new ArgumentNullException(nameof(log));
            
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            // Filter by BackYearBlocked
            if (log.Year < _backYearBlocked)
            {
                return; // Skip records before the blocked year
            }

            // Use retry logic if connection manager is available
            if (_connectionManager != null)
            {
                _connectionManager.ExecuteWithRetry(connection, (conn) =>
                {
                    InsertRawLogInternal(log, conn);
                }, "Insert raw log to Access database");
            }
            else
            {
                InsertRawLogInternal(log, connection);
            }
        }

        /// <summary>
        /// Internal method to insert raw log (used by retry logic)
        /// </summary>
        private void InsertRawLogInternal(AttendanceLog log, IDbConnection connection)
        {
            // Check for duplicate
            string checkSql = $@"
                SELECT COUNT(*) 
                FROM [0RawLog] 
                WHERE vTMachineNumber = {log.TMachineNumber} 
                  AND vSEnrollNumber = {log.SEnrollNumber} 
                  AND vYear = {log.Year} 
                  AND vMonth = {log.Month} 
                  AND vDay = {log.Day} 
                  AND vHour = {log.Hour} 
                  AND vMinute = {log.Minute} 
                  AND vSecond = {log.Second} 
                  AND vInOut = '{log.InOut}'";

            if (RecordExists(checkSql, connection))
            {
                // Duplicate found, skip insertion
                return;
            }

            // Insert new record using parameterized query
            string insertSql = @"
                INSERT INTO [0RawLog] 
                (vTMachineNumber, vSMachineNumber, vSEnrollNumber, vVerifyMode, 
                 vYear, vMonth, vDay, vHour, vMinute, vSecond, vInOut, vtrfFlag) 
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

            using (var command = connection.CreateCommand())
            {
                command.CommandText = insertSql;

                // Add parameters to prevent SQL injection
                AddParameter(command, log.TMachineNumber);
                AddParameter(command, log.SMachineNumber);
                AddParameter(command, log.SEnrollNumber);
                AddParameter(command, log.VerifyMode);
                AddParameter(command, log.Year);
                AddParameter(command, log.Month);
                AddParameter(command, log.Day);
                AddParameter(command, log.Hour);
                AddParameter(command, log.Minute);
                AddParameter(command, log.Second);
                AddParameter(command, log.InOut);
                AddParameter(command, "0"); // vtrfFlag = 0 (unprocessed)

                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Gets employee ID from M_Executive table by BioID
        /// </summary>
        public string GetEmployeeId(int bioId, IDbConnection connection)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            // Use parameterized query to prevent SQL injection
            string sql = "SELECT EmpID FROM M_Executive WHERE BioID = ?";

            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                AddParameter(command, bioId);

                var result = command.ExecuteScalar();

                // Return EmpID if found and not null/empty, otherwise return bioId as string
                if (result != null && result != DBNull.Value)
                {
                    string empId = result.ToString().Trim();
                    if (!string.IsNullOrEmpty(empId))
                    {
                        return empId;
                    }
                }

                // Fallback to enrollment number
                return bioId.ToString();
            }
        }

        /// <summary>
        /// Helper method to add parameters to command
        /// </summary>
        private void AddParameter(IDbCommand command, object value)
        {
            var parameter = command.CreateParameter();
            parameter.Value = value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }
    }
}
