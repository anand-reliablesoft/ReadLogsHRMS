using System;
using System.Configuration;
using System.Data;
using BiometricAttendance.Common.Interfaces;
using BiometricAttendance.Common.Models;

namespace BiometricAttendance.Common.Services
{
    /// <summary>
    /// Processes and saves raw attendance logs to databases
    /// </summary>
    public class RawLogProcessor : IRawLogProcessor
    {
        private readonly IAccessDatabaseRepository _accessRepository;
        private readonly ISqlServerRepository _sqlRepository;
        private readonly int _backYearBlocked;

        public RawLogProcessor(IEmployeeMapper employeeMapper)
        {
            // Create repository instances
            _accessRepository = new AccessDatabaseRepository();
            _sqlRepository = new SqlServerRepository();

            // Read BackYearBlocked configuration (default to 2023)
            string backYearConfig = ConfigurationManager.AppSettings["BackYearBlocked"] ?? "2023";
            _backYearBlocked = int.Parse(backYearConfig);
        }

        /// <summary>
        /// Sets the database connection manager for retry logic
        /// </summary>
        public void SetConnectionManager(IDatabaseConnectionManager connectionManager)
        {
            _accessRepository.SetConnectionManager(connectionManager);
            _sqlRepository.SetConnectionManager(connectionManager);
        }

        /// <summary>
        /// Saves raw log to both Access and SQL Server databases
        /// </summary>
        public void SaveRawLog(AttendanceLog log, IDbConnection accessConn, IDbConnection sqlConn, IFileLogger logger)
        {
            if (log == null)
                throw new ArgumentNullException(nameof(log));
            
            if (accessConn == null)
                throw new ArgumentNullException(nameof(accessConn));
            
            if (sqlConn == null)
                throw new ArgumentNullException(nameof(sqlConn));

            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            try
            {
                // Filter by BackYearBlocked
                if (log.Year < _backYearBlocked)
                {
                    logger.Log($"Skipping log from year {log.Year} (before BackYearBlocked {_backYearBlocked}): " +
                              $"Machine={log.TMachineNumber}, Enroll={log.SEnrollNumber}");
                    return;
                }

                // Check for duplicates in Access database
                bool isDuplicateInAccess = IsDuplicate(log, accessConn);
                
                // Check for duplicates in SQL Server database
                bool isDuplicateInSql = IsDuplicate(log, sqlConn);

                if (isDuplicateInAccess && isDuplicateInSql)
                {
                    logger.Log($"Duplicate detected: Machine={log.TMachineNumber}, Enroll={log.SEnrollNumber}, " +
                              $"DateTime={log.Year}-{log.Month:D2}-{log.Day:D2} {log.Hour:D2}:{log.Minute:D2}:{log.Second:D2}, " +
                              $"InOut={log.InOut}");
                    return;
                }

                // Insert to Access database if not duplicate
                if (!isDuplicateInAccess)
                {
                    _accessRepository.InsertRawLog(log, accessConn);
                    logger.Log($"Inserted to Access: Machine={log.TMachineNumber}, Enroll={log.SEnrollNumber}, " +
                              $"DateTime={log.Year}-{log.Month:D2}-{log.Day:D2} {log.Hour:D2}:{log.Minute:D2}:{log.Second:D2}, " +
                              $"InOut={log.InOut}");
                }

                // Insert to SQL Server database if not duplicate
                if (!isDuplicateInSql)
                {
                    _sqlRepository.InsertRawLog(log, sqlConn);
                    logger.Log($"Inserted to SQL Server: Machine={log.TMachineNumber}, Enroll={log.SEnrollNumber}, " +
                              $"DateTime={log.Year}-{log.Month:D2}-{log.Day:D2} {log.Hour:D2}:{log.Minute:D2}:{log.Second:D2}, " +
                              $"InOut={log.InOut}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error saving raw log: Machine={log.TMachineNumber}, Enroll={log.SEnrollNumber}", ex);
                throw;
            }
        }

        /// <summary>
        /// Checks if a log entry is a duplicate
        /// </summary>
        public bool IsDuplicate(AttendanceLog log, IDbConnection connection)
        {
            if (log == null)
                throw new ArgumentNullException(nameof(log));
            
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            // Check for duplicate matching all key fields
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

            using (var command = connection.CreateCommand())
            {
                command.CommandText = checkSql;
                var result = command.ExecuteScalar();
                
                if (result == null || result == DBNull.Value)
                    return false;

                return Convert.ToInt32(result) > 0;
            }
        }
    }
}
