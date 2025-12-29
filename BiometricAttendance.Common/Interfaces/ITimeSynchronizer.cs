using System;

namespace BiometricAttendance.Common.Interfaces
{
    /// <summary>
    /// Interface for synchronizing time on biometric devices
    /// </summary>
    public interface ITimeSynchronizer
    {
        /// <summary>
        /// Synchronizes the device time with the current server time
        /// </summary>
        /// <param name="sdk">SDK wrapper instance for device communication</param>
        /// <param name="machineNumber">Machine number to synchronize</param>
        /// <param name="logger">Optional logger for recording sync operations</param>
        /// <returns>True if synchronization was successful, false otherwise</returns>
        bool SynchronizeTime(ISdkWrapper sdk, int machineNumber, IFileLogger logger = null);
    }
}
