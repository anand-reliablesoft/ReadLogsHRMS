using System;
using BiometricAttendance.Common.Models;

namespace BiometricAttendance.Common.Interfaces
{
    /// <summary>
    /// Interface for wrapping the SBXPC ActiveX control SDK
    /// </summary>
    public interface ISdkWrapper : IDisposable
    {
        /// <summary>
        /// Sets the IP address, port, and password for device connection
        /// </summary>
        bool SetIPAddress(string ipAddress, int port, int password);

        /// <summary>
        /// Opens the communication port to the specified machine
        /// </summary>
        bool OpenCommPort(int machineNumber);

        /// <summary>
        /// Closes the communication port
        /// </summary>
        void CloseCommPort();

        /// <summary>
        /// Enables or disables the device
        /// </summary>
        bool EnableDevice(int machineNumber, bool enable);

        /// <summary>
        /// Sets the device time to current server time
        /// </summary>
        bool SetDeviceTime(int machineNumber);

        /// <summary>
        /// Gets the last error code from the SDK
        /// </summary>
        int GetLastError();

        /// <summary>
        /// Sets the read mark to enable incremental log retrieval tracking
        /// </summary>
        int SetReadMark { set; }

        /// <summary>
        /// Reads new/unread general log data from the device
        /// </summary>
        bool ReadGeneralLogData(int machineNumber);

        /// <summary>
        /// Reads all log data from the device
        /// </summary>
        bool ReadAllGLogData(int machineNumber);

        /// <summary>
        /// Gets general log data with output parameters
        /// </summary>
        bool GetGeneralLogData(int machineNumber, out AttendanceLog log);

        /// <summary>
        /// Gets all log data with output parameters
        /// </summary>
        bool GetAllGLogData(int machineNumber, out AttendanceLog log);
    }
}
