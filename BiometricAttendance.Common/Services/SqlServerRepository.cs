using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Odbc;
using BiometricAttendance.Common.Interfaces;
using BiometricAttendance.Common.Models;

namespace BiometricAttendance.Common.Services
{
    /// <summary>
    /// Repository for SQL Server database operations
    /// </summary>
    public class SqlServerRepository : ISqlServerRepository
    {
        private readonly int _backYearBlocked;
        private IDatabaseConnectionManager _connectionManager;

        public SqlServerRepository()
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
                
                using (var adapter = new OdbcDataAdapter((OdbcCommand)command))
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
                }, "Insert raw log to SQL Server");
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
        /// Inserts an attendance record with duplicate check
        /// </summary>
        public void InsertAttendanceRecord(AttendanceRecord record, IDbConnection connection)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));
            
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            // Use retry logic if connection manager is available
            if (_connectionManager != null)
            {
                _connectionManager.ExecuteWithRetry(connection, (conn) =>
                {
                    InsertAttendanceRecordInternal(record, conn);
                }, "Insert attendance record to SQL Server");
            }
            else
            {
                InsertAttendanceRecordInternal(record, connection);
            }
        }

        /// <summary>
        /// Internal method to insert attendance record (used by retry logic)
        /// </summary>
        private void InsertAttendanceRecordInternal(AttendanceRecord record, IDbConnection connection)
        {
            // Check for duplicate
            string checkSql = $@"
                SELECT COUNT(*) 
                FROM AttenInfo 
                WHERE EmpCode = ? 
                  AND EntryDate = ? 
                  AND InOutFlag = ? 
                  AND EntryTime = ?";

            using (var checkCommand = connection.CreateCommand())
            {
                checkCommand.CommandText = checkSql;
                AddParameter(checkCommand, record.EmpCode);
                AddParameter(checkCommand, record.EntryDate);
                AddParameter(checkCommand, record.InOutFlag);
                AddParameter(checkCommand, record.EntryTime);

                var result = checkCommand.ExecuteScalar();
                if (result != null && result != DBNull.Value && Convert.ToInt32(result) > 0)
                {
                    // Duplicate found, skip insertion
                    return;
                }
            }

            // Insert new record using parameterized query
            string insertSql = @"
                INSERT INTO AttenInfo 
                (EmpCode, TicketNo, EntryDate, InOutFlag, EntryTime, TrfFlag, UpdateUID, Location, ErrMsg) 
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)";

            using (var command = connection.CreateCommand())
            {
                command.CommandText = insertSql;

                // Add parameters
                AddParameter(command, record.EmpCode);
                AddParameter(command, record.TicketNo);
                AddParameter(command, record.EntryDate);
                AddParameter(command, record.InOutFlag);
                AddParameter(command, record.EntryTime);
                AddParameter(command, record.TrfFlag);
                AddParameter(command, record.UpdateUID);
                AddParameter(command, record.Location);
                AddParameter(command, record.ErrMsg);

                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Updates the transfer flag for a raw log record
        /// </summary>
        public void UpdateRawLogTransferFlag(int id, IDbConnection connection)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            string updateSql = "UPDATE [0RawLog] SET vtrfFlag = '1' WHERE ID = ?";

            using (var command = connection.CreateCommand())
            {
                command.CommandText = updateSql;
                AddParameter(command, id);
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Gets unprocessed raw logs sorted chronologically
        /// </summary>
        public IEnumerable<AttendanceLog> GetUnprocessedRawLogs(IDbConnection connection)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            string sql = @"
                SELECT ID, vTMachineNumber, vSMachineNumber, vSEnrollNumber, vVerifyMode, 
                       vYear, vMonth, vDay, vHour, vMinute, vSecond, vInOut 
                FROM [0RawLog] 
                WHERE vtrfFlag IS NULL OR vtrfFlag = '0' 
                ORDER BY vSEnrollNumber, vYear, vMonth, vDay, vHour, vMinute, vSecond";

            var logs = new List<AttendanceLog>();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var log = new AttendanceLog
                        {
                            ID = GetInt32(reader, "ID"),
                            TMachineNumber = GetInt32(reader, "vTMachineNumber"),
                            SMachineNumber = GetInt32(reader, "vSMachineNumber"),
                            SEnrollNumber = GetInt32(reader, "vSEnrollNumber"),
                            VerifyMode = GetInt32(reader, "vVerifyMode"),
                            Year = GetInt32(reader, "vYear"),
                            Month = GetInt32(reader, "vMonth"),
                            Day = GetInt32(reader, "vDay"),
                            Hour = GetInt32(reader, "vHour"),
                            Minute = GetInt32(reader, "vMinute"),
                            Second = GetInt32(reader, "vSecond"),
                            InOut = GetString(reader, "vInOut")
                        };

                        logs.Add(log);
                    }
                }
            }

            return logs;
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

        /// <summary>
        /// Helper method to safely get int32 from data reader
        /// </summary>
        private int GetInt32(IDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal))
                return 0;
            
            return Convert.ToInt32(reader.GetValue(ordinal));
        }

        /// <summary>
        /// Helper method to safely get string from data reader
        /// </summary>
        private string GetString(IDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal))
                return string.Empty;
            
            return reader.GetString(ordinal);
        }
    }
}
