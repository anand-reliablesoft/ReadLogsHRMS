using System;
using System.Collections.Generic;
using BiometricAttendance.Common.Interfaces;
using BiometricAttendance.Common.Models;

namespace BiometricAttendance.Common.Services
{
    /// <summary>
    /// Reads attendance logs from biometric devices using the SDK
    /// </summary>
    public class LogReader : ILogReader
    {
        private IFileLogger _logger;

        /// <summary>
        /// Initializes a new instance of the LogReader class
        /// </summary>
        public LogReader()
        {
            // Logger will be set via SetLogger method
        }

        /// <summary>
        /// Sets the logger for operational logging
        /// </summary>
        public void SetLogger(IFileLogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Reads attendance logs from a biometric device
        /// </summary>
        /// <param name="sdk">SDK wrapper for device communication</param>
        /// <param name="config">Machine configuration with device details</param>
        /// <param name="deleteAllMode">True to read all logs, false to read only new logs</param>
        /// <returns>Enumerable collection of attendance logs</returns>
        public IEnumerable<AttendanceLog> ReadLogs(ISdkWrapper sdk, MachineConfiguration config, bool deleteAllMode)
        {
            if (sdk == null)
                throw new ArgumentNullException(nameof(sdk));
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (_logger != null)
            {
                _logger.Log($"Starting log read for Machine {config.MachineNumber} (DeleteAllMode: {deleteAllMode})");
            }

            // Use a list to collect logs, then yield them outside the try-catch
            var logs = new List<AttendanceLog>();
            
            try
            {
                // Set ReadMark to 1 before reading to enable incremental log retrieval tracking
                sdk.SetReadMark = 1;
                
                if (_logger != null)
                {
                    _logger.Log($"Set ReadMark to 1 for Machine {config.MachineNumber}");
                }

                bool readSuccess;
                
                // Call appropriate SDK method based on deleteAllMode
                if (deleteAllMode)
                {
                    if (_logger != null)
                    {
                        _logger.Log($"Reading all logs from Machine {config.MachineNumber}");
                    }
                    readSuccess = sdk.ReadAllGLogData(config.MachineNumber);
                }
                else
                {
                    if (_logger != null)
                    {
                        _logger.Log($"Reading new logs from Machine {config.MachineNumber}");
                    }
                    readSuccess = sdk.ReadGeneralLogData(config.MachineNumber);
                }

                if (!readSuccess)
                {
                    int errorCode = sdk.GetLastError();
                    
                    // Error code 6 (ERR_LOG_END) is normal - means no data available
                    if (errorCode == 6)
                    {
                        if (_logger != null)
                        {
                            _logger.Log($"No logs available on Machine {config.MachineNumber} (ERR_LOG_END)");
                        }
                        // Don't add any logs to the list, will yield break at the end
                    }
                    else
                    {
                        // Use SDK wrapper's error message mapping for consistency
                        string errorMessage = sdk is SbxpcDllWrapper 
                            ? SbxpcDllWrapper.GetErrorMessage(errorCode)
                            : SdkWrapper.GetErrorMessage(errorCode);
                        
                        if (_logger != null)
                        {
                            _logger.LogError($"Failed to read logs from Machine {config.MachineNumber} at {config.IPAddress}:{config.Port}. Error: {errorMessage} (Code: {errorCode})", null);
                        }
                    }
                }
                else
                {
                    // Loop until SDK method returns false (no more data)
                    int logCount = 0;
                    while (true)
                    {
                        AttendanceLog log;
                        bool hasMoreData;

                        // Get log data using appropriate method based on deleteAllMode
                        if (deleteAllMode)
                        {
                            hasMoreData = sdk.GetAllGLogData(config.MachineNumber, out log);
                        }
                        else
                        {
                            hasMoreData = sdk.GetGeneralLogData(config.MachineNumber, out log);
                        }

                        if (!hasMoreData)
                        {
                            // Check if this is an error or just end of data
                            int errorCode = sdk.GetLastError();
                            if (errorCode != 0 && errorCode != 6)
                            {
                                // Use SDK wrapper's error message mapping for consistency
                                string errorMessage = sdk is SbxpcDllWrapper 
                                    ? SbxpcDllWrapper.GetErrorMessage(errorCode)
                                    : SdkWrapper.GetErrorMessage(errorCode);
                                
                                if (_logger != null)
                                {
                                    _logger.LogError($"Error reading log data from Machine {config.MachineNumber} at {config.IPAddress}:{config.Port}. Error: {errorMessage} (Code: {errorCode})", null);
                                }
                            }
                            break;
                        }

                        // Set the InOut flag from machine configuration
                        log.InOut = config.InOutFlag;
                        log.TMachineNumber = config.MachineNumber;

                        logCount++;
                        logs.Add(log);
                    }

                    if (_logger != null)
                    {
                        _logger.Log($"Successfully read {logCount} logs from Machine {config.MachineNumber}");
                    }
                }
            }
            catch (Exception ex)
            {
                if (_logger != null)
                {
                    _logger.LogError($"Exception while reading logs from Machine {config.MachineNumber} at {config.IPAddress}:{config.Port}: {ex.Message}", ex);
                }
                // Don't rethrow - allow service to continue processing other devices
                // Return whatever logs we collected before the exception
            }
            
            // Yield return logs outside the try-catch
            foreach (var log in logs)
            {
                yield return log;
            }
        }
    }
}
