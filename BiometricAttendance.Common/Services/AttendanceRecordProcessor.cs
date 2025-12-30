using System;
using System.Data;
using BiometricAttendance.Common.Interfaces;
using BiometricAttendance.Common.Models;

namespace BiometricAttendance.Common.Services
{
    /// <summary>
    /// Processes raw logs into attendance records
    /// </summary>
    public class AttendanceRecordProcessor : IAttendanceRecordProcessor
    {
        private readonly ISqlServerRepository _sqlRepository;
        private readonly IEmployeeMapper _employeeMapper;

        public AttendanceRecordProcessor(IEmployeeMapper employeeMapper, ISqlServerRepository sqlRepository)
        {
            _sqlRepository = sqlRepository ?? throw new ArgumentNullException(nameof(sqlRepository));
            _employeeMapper = employeeMapper ?? throw new ArgumentNullException(nameof(employeeMapper));
        }

        /// <summary>
        /// Sets the database connection manager for retry logic
        /// </summary>
        public void SetConnectionManager(IDatabaseConnectionManager connectionManager)
        {
            _sqlRepository.SetConnectionManager(connectionManager);
        }

        /// <summary>
        /// Processes unprocessed raw logs and creates attendance records
        /// </summary>
        public void ProcessRawLogs(IDbConnection accessConn, IDbConnection sqlConn, IFileLogger logger)
        {
            if (accessConn == null)
                throw new ArgumentNullException(nameof(accessConn));
            
            if (sqlConn == null)
                throw new ArgumentNullException(nameof(sqlConn));

            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            IDbTransaction transaction = null;

            try
            {
                logger.Log("Starting batch processing of raw logs...");

                // CRITICAL FIX: Get unprocessed raw logs BEFORE starting transaction
                // This prevents the "ExecuteReader requires transaction" error
                var unprocessedLogs = _sqlRepository.GetUnprocessedRawLogs(sqlConn);

                // Begin transaction for batch processing
                transaction = sqlConn.BeginTransaction();

                int processedCount = 0;
                int skippedCount = 0;
                int errorCount = 0;

                foreach (var log in unprocessedLogs)
                {
                    try
                    {
                        // Validate log data
                        if (log == null)
                        {
                            logger.LogError("Null log entry encountered, skipping", null);
                            skippedCount++;
                            continue;
                        }

                        // Map enrollment number to employee ID
                        string empCode = _employeeMapper.GetEmployeeId(log.SEnrollNumber, accessConn);

                        // Validate employee code
                        if (string.IsNullOrWhiteSpace(empCode))
                        {
                            logger.LogError($"Empty employee code for enrollment {log.SEnrollNumber}, skipping", null);
                            skippedCount++;
                            continue;
                        }

                        // Create attendance record
                        var attendanceRecord = new AttendanceRecord
                        {
                            EmpCode = empCode,
                            TicketNo = 0,
                            EntryDate = log.GetDateTime().Date,
                            InOutFlag = log.InOut,
                            EntryTime = new DateTime(1900, 1, 1).Add(log.GetDateTime().TimeOfDay),
                            TrfFlag = 0,
                            UpdateUID = null,
                            Location = null,
                            ErrMsg = null
                        };

                        // Insert attendance record (repository handles duplicate check)
                        _sqlRepository.InsertAttendanceRecord(attendanceRecord, sqlConn, transaction);

                        // Update transfer flag to mark as processed
                        _sqlRepository.UpdateRawLogTransferFlag(log, sqlConn, transaction);

                        processedCount++;
                        
                        logger.Log($"Processed: ID={log.ID}, EmpCode={empCode}, " +
                                  $"DateTime={log.GetDateTime():yyyy-MM-dd HH:mm:ss}, InOut={log.InOut}");
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        string logId = log?.ID.ToString() ?? "unknown";
                        int enrollNumber = log?.SEnrollNumber ?? 0;
                        logger.LogError($"Error processing raw log ID={logId}, Enroll={enrollNumber}", ex);
                        // Continue processing remaining records
                    }
                }

                // Commit transaction
                transaction.Commit();

                logger.Log($"Batch processing completed: Processed={processedCount}, Skipped={skippedCount}, Errors={errorCount}");
            }
            catch (Exception ex)
            {
                // Rollback transaction on error
                if (transaction != null)
                {
                    try
                    {
                        transaction.Rollback();
                        logger.Log("Transaction rolled back due to error");
                    }
                    catch (Exception rollbackEx)
                    {
                        logger.LogError("Error rolling back transaction", rollbackEx);
                    }
                }

                logger.LogError("Error during batch processing", ex);
                throw;
            }
            finally
            {
                transaction?.Dispose();
            }
        }
    }
}
