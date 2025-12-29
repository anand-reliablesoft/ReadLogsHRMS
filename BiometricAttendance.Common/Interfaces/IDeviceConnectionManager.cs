using BiometricAttendance.Common.Models;

namespace BiometricAttendance.Common.Interfaces
{
    /// <summary>
    /// Interface for managing connections to biometric devices
    /// </summary>
    public interface IDeviceConnectionManager
    {
        /// <summary>
        /// Connects to a biometric device using the provided configuration
        /// </summary>
        /// <param name="config">Machine configuration containing connection parameters</param>
        /// <param name="sdk">SDK wrapper instance to use for connection</param>
        /// <returns>True if connection successful, false otherwise</returns>
        bool Connect(MachineConfiguration config, ISdkWrapper sdk);

        /// <summary>
        /// Disconnects from the currently connected device
        /// </summary>
        /// <param name="sdk">SDK wrapper instance to disconnect</param>
        void Disconnect(ISdkWrapper sdk);
    }
}
