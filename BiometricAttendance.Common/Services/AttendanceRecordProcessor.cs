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

                // Begin transaction for batch processing
                transaction = sqlConn.BeginTransaction();

                // Get unprocessed raw logs sorted chronologically
                var unprocessedLogs = _sqlRepository.GetUnprocessedRawLogs(sqlConn);

                int processedCount = 0;
                int skippedCount = 0;
                int errorCount = 0;

                foreach (var log in unprocessedLogs)
                {
                    try
                    {
                        // Map enrollment number to employee ID
                        string empCode = _employeeMapper.GetEmployeeId(log.SEnrollNumber, accessConn);

                        // Create attendance record
                        var attendanceRecord = new AttendanceRecord
                        {
                            EmpCode = empCode,
                            TicketNo = 0,
                            EntryDate = log.GetDateTime().Date,
                            InOutFlag = log.InOut,
                            EntryTime = log.GetDateTime().TimeOfDay,
                            TrfFlag = 0,
                            UpdateUID = null,
                            Location = null,
                            ErrMsg = null
                        };

                        // Insert attendance record (repository handles duplicate check)
                        _sqlRepository.InsertAttendanceRecord(attendanceRecord, sqlConn);

                        // Update transfer flag to mark as processed
                        _sqlRepository.UpdateRawLogTransferFlag(log.ID, sqlConn);

                        processedCount++;
                        
                        logger.Log($"Processed: ID={log.ID}, EmpCode={empCode}, " +
                                  $"DateTime={log.GetDateTime():yyyy-MM-dd HH:mm:ss}, InOut={log.InOut}");
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        logger.LogError($"Error processing raw log ID={log.ID}, Enroll={log.SEnrollNumber}", ex);
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
