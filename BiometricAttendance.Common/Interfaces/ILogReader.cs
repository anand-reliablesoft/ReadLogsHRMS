using System.Collections.Generic;
using BiometricAttendance.Common.Models;

namespace BiometricAttendance.Common.Interfaces
{
    /// <summary>
    /// Interface for reading attendance logs from biometric devices
    /// </summary>
    public interface ILogReader
    {
        /// <summary>
        /// Reads attendance logs from a biometric device
        /// </summary>
        /// <param name="sdk">SDK wrapper for device communication</param>
        /// <param name="config">Machine configuration with device details</param>
        /// <param name="deleteAllMode">True to read all logs, false to read only new logs</param>
        /// <returns>Enumerable collection of attendance logs</returns>
        IEnumerable<AttendanceLog> ReadLogs(ISdkWrapper sdk, MachineConfiguration config, bool deleteAllMode);

        /// <summary>
        /// Sets the logger for operational logging
        /// </summary>
        void SetLogger(IFileLogger logger);
    }
}
