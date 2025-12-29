using System;
using System.Runtime.InteropServices;
using System.Text;
using BiometricAttendance.Common.Interfaces;
using BiometricAttendance.Common.Models;

namespace BiometricAttendance.Common.Services
{
    /// <summary>
    /// P/Invoke wrapper for SBXPCDLL.dll
    /// Uses direct DLL calls instead of COM interop for better reliability
    /// </summary>
    public class SbxpcDllWrapper : ISdkWrapper
    {
        private const string DLL_NAME = "SBXPCDLL.dll";
        private bool _disposed = false;
        private bool _connected = false;

        #region P/Invoke Declarations

        [DllImport(DLL_NAME, EntryPoint = "_ConnectTcpip", CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Unicode)]
        private static extern bool ConnectTcpip(int dwMachineNumber, ref IntPtr lpszIPAddress, int dwPortNumber, int dwPassWord);

        [DllImport(DLL_NAME, EntryPoint = "_Disconnect", CallingConvention = CallingConvention.Winapi)]
        private static extern void Disconnect();

        [DllImport(DLL_NAME, EntryPoint = "_EnableDevice", CallingConvention = CallingConvention.Winapi)]
        private static extern bool EnableDevice(int dwMachineNumber, bool bFlag);

        [DllImport(DLL_NAME, EntryPoint = "_SetDeviceTime", CallingConvention = CallingConvention.Winapi)]
        private static extern bool SetDeviceTime(int dwMachineNumber);

        [DllImport(DLL_NAME, EntryPoint = "_GetLastError", CallingConvention = CallingConvention.Winapi)]
        private static extern bool GetLastError(out int dwErrorCode);

        [DllImport(DLL_NAME, EntryPoint = "_SetReadMark", CallingConvention = CallingConvention.Winapi)]
        private static extern bool SetReadMarkDll(int dwMachineNumber, int dwReadMark);

        [DllImport(DLL_NAME, EntryPoint = "_ReadGeneralLogData", CallingConvention = CallingConvention.Winapi)]
        private static extern bool ReadGeneralLogData(int dwMachineNumber);

        [DllImport(DLL_NAME, EntryPoint = "_ReadAllGLogData", CallingConvention = CallingConvention.Winapi)]
        private static extern bool ReadAllGLogData(int dwMachineNumber);

        [DllImport(DLL_NAME, EntryPoint = "_GetGeneralLogData", CallingConvention = CallingConvention.Winapi)]
        private static extern bool GetGeneralLogData(
            int dwMachineNumber,
            out int dwTMachineNumber,
            out int dwSEnrollNumber,
            out int dwSMachineNumber,
            out int dwVerifyMode,
            out int dwYear,
            out int dwMonth,
            out int dwDay,
            out int dwHour,
            out int dwMinute,
            out int dwSecond);

        [DllImport(DLL_NAME, EntryPoint = "_GetAllGLogData", CallingConvention = CallingConvention.Winapi)]
        private static extern bool GetAllGLogData(
            int dwMachineNumber,
            out int dwTMachineNumber,
            out int dwSEnrollNumber,
            out int dwSMachineNumber,
            out int dwVerifyMode,
            out int dwYear,
            out int dwMonth,
            out int dwDay,
            out int dwHour,
            out int dwMinute,
            out int dwSecond);

        #endregion

        public SbxpcDllWrapper()
        {
            if (Environment.UserInteractive)
            {
                Console.WriteLine("Using SBXPCDLL.dll direct API...");
            }
        }

        /// <summary>
        /// Sets the IP address, port, and password for device connection
        /// For DLL API, this is combined with OpenCommPort into ConnectTcpip
        /// </summary>
        public bool SetIPAddress(string ipAddress, int port, int password)
        {
            // Store for later use in OpenCommPort
            // For DLL API, we don't actually call anything here
            if (Environment.UserInteractive)
            {
                Console.WriteLine($"SetIPAddress called (DLL mode): {ipAddress}:{port}");
            }
            return true;
        }

        /// <summary>
        /// Opens the communication port to the specified machine
        /// In DLL mode, this actually calls ConnectTcpip
        /// </summary>
        public bool OpenCommPort(int machineNumber)
        {
            try
            {
                if (Environment.UserInteractive)
                {
                    Console.WriteLine($"OpenCommPort called for machine {machineNumber} (DLL mode)");
                }
                
                // Note: We need to get IP, port, password from somewhere
                // This is a limitation of the DLL API - it combines SetIPAddress and OpenCommPort
                // We'll need to refactor to pass these parameters
                
                return false; // Placeholder - need to refactor
            }
            catch (Exception ex)
            {
                if (Environment.UserInteractive)
                {
                    Console.WriteLine($"OpenCommPort exception: {ex.Message}");
                }
                throw new InvalidOperationException($"Failed to open communication port for machine {machineNumber}", ex);
            }
        }

        /// <summary>
        /// Connects to device using TCP/IP
        /// </summary>
        public bool Connect(string ipAddress, int port, int password, int machineNumber)
        {
            IntPtr bstrPtr = IntPtr.Zero;
            try
            {
                if (Environment.UserInteractive)
                {
                    Console.WriteLine($"Connecting to {ipAddress}:{port} (machine {machineNumber})...");
                }

                // Allocate BSTR for IP address
                bstrPtr = Marshal.StringToBSTR(ipAddress);
                
                bool result = ConnectTcpip(machineNumber, ref bstrPtr, port, password);

                if (result)
                {
                    _connected = true;
                    if (Environment.UserInteractive)
                    {
                        Console.WriteLine($"Connected successfully!");
                    }

                    // CRITICAL: Set ReadMark to 1 before reading data (as per VB6 code)
                    bool readMarkSet = SetReadMarkDll(machineNumber, 1);
                    if (readMarkSet)
                    {
                        if (Environment.UserInteractive)
                        {
                            Console.WriteLine($"ReadMark set to 1");
                        }
                    }
                    else
                    {
                        if (Environment.UserInteractive)
                        {
                            Console.WriteLine($"Warning: Failed to set ReadMark");
                        }
                    }
                }
                else
                {
                    if (Environment.UserInteractive)
                    {
                        int errorCode;
                        if (GetLastError(out errorCode))
                        {
                            Console.WriteLine($"Connection failed with error code: {errorCode} - {GetErrorMessage(errorCode)}");
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                if (Environment.UserInteractive)
                {
                    Console.WriteLine($"Connect exception: {ex.Message}");
                }
                throw new InvalidOperationException($"Failed to connect to {ipAddress}:{port}", ex);
            }
            finally
            {
                // Free BSTR
                if (bstrPtr != IntPtr.Zero)
                {
                    Marshal.FreeBSTR(bstrPtr);
                }
            }
        }

        /// <summary>
        /// Closes the communication port
        /// </summary>
        public void CloseCommPort()
        {
            try
            {
                if (_connected)
                {
                    Disconnect();
                    _connected = false;
                    
                    if (Environment.UserInteractive)
                    {
                        Console.WriteLine("Disconnected from device");
                    }
                }
            }
            catch (Exception ex)
            {
                if (Environment.UserInteractive)
                {
                    Console.WriteLine($"Disconnect exception: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Enables or disables the device
        /// </summary>
        bool ISdkWrapper.EnableDevice(int machineNumber, bool enable)
        {
            try
            {
                return EnableDevice(machineNumber, enable);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to {(enable ? "enable" : "disable")} device {machineNumber}", ex);
            }
        }

        /// <summary>
        /// Sets the device time to current server time
        /// </summary>
        bool ISdkWrapper.SetDeviceTime(int machineNumber)
        {
            try
            {
                return SetDeviceTime(machineNumber);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to set device time for machine {machineNumber}", ex);
            }
        }

        /// <summary>
        /// Gets the last error code from the SDK
        /// </summary>
        int ISdkWrapper.GetLastError()
        {
            try
            {
                int errorCode;
                if (GetLastError(out errorCode))
                {
                    return errorCode;
                }
                return -1;
            }
            catch (Exception)
            {
                return -1;
            }
        }

        /// <summary>
        /// Sets the read mark to enable incremental log retrieval tracking
        /// </summary>
        public int SetReadMark
        {
            set
            {
                try
                {
                    // Note: DLL API may require machine number, using 1 as default
                    SetReadMarkDll(1, value);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to set read mark to {value}", ex);
                }
            }
        }

        /// <summary>
        /// Reads new/unread general log data from the device
        /// </summary>
        bool ISdkWrapper.ReadGeneralLogData(int machineNumber)
        {
            try
            {
                return ReadGeneralLogData(machineNumber);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to read general log data from machine {machineNumber}", ex);
            }
        }

        /// <summary>
        /// Reads all log data from the device
        /// </summary>
        bool ISdkWrapper.ReadAllGLogData(int machineNumber)
        {
            try
            {
                return ReadAllGLogData(machineNumber);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to read all log data from machine {machineNumber}", ex);
            }
        }

        /// <summary>
        /// Gets general log data with output parameters
        /// </summary>
        public bool GetGeneralLogData(int machineNumber, out AttendanceLog log)
        {
            log = null;
            
            try
            {
                int vTMachineNumber, vSEnrollNumber, vSMachineNumber, vVerifyMode;
                int vYear, vMonth, vDay, vHour, vMinute, vSecond;

                bool result = GetGeneralLogData(
                    machineNumber,
                    out vTMachineNumber,
                    out vSEnrollNumber,
                    out vSMachineNumber,
                    out vVerifyMode,
                    out vYear,
                    out vMonth,
                    out vDay,
                    out vHour,
                    out vMinute,
                    out vSecond);

                if (result)
                {
                    log = new AttendanceLog
                    {
                        TMachineNumber = vTMachineNumber,
                        SMachineNumber = vSMachineNumber,
                        SEnrollNumber = vSEnrollNumber,
                        VerifyMode = vVerifyMode,
                        Year = vYear,
                        Month = vMonth,
                        Day = vDay,
                        Hour = vHour,
                        Minute = vMinute,
                        Second = vSecond
                    };
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get general log data from machine {machineNumber}", ex);
            }
        }

        /// <summary>
        /// Gets all log data with output parameters
        /// </summary>
        public bool GetAllGLogData(int machineNumber, out AttendanceLog log)
        {
            log = null;
            
            try
            {
                int vTMachineNumber, vSEnrollNumber, vSMachineNumber, vVerifyMode;
                int vYear, vMonth, vDay, vHour, vMinute, vSecond;

                bool result = GetAllGLogData(
                    machineNumber,
                    out vTMachineNumber,
                    out vSEnrollNumber,
                    out vSMachineNumber,
                    out vVerifyMode,
                    out vYear,
                    out vMonth,
                    out vDay,
                    out vHour,
                    out vMinute,
                    out vSecond);

                if (result)
                {
                    log = new AttendanceLog
                    {
                        TMachineNumber = vTMachineNumber,
                        SMachineNumber = vSMachineNumber,
                        SEnrollNumber = vSEnrollNumber,
                        VerifyMode = vVerifyMode,
                        Year = vYear,
                        Month = vMonth,
                        Day = vDay,
                        Hour = vHour,
                        Minute = vMinute,
                        Second = vSecond
                    };
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get all log data from machine {machineNumber}", ex);
            }
        }

        /// <summary>
        /// Gets a descriptive error message for the given error code
        /// </summary>
        /// <param name="errorCode">SDK error code</param>
        /// <returns>Human-readable error description</returns>
        public static string GetErrorMessage(int errorCode)
        {
            switch (errorCode)
            {
                case 0:
                    return "SUCCESS - Operation completed successfully";
                case 1:
                    return "ERR_COMPORT_ERROR - Communication port error (device unreachable or network issue)";
                case 2:
                    return "ERR_WRITE_FAIL - Write operation failed (device may be busy or disconnected)";
                case 3:
                    return "ERR_READ_FAIL - Read operation failed (device may be busy or disconnected)";
                case 4:
                    return "ERR_INVALID_PARAM - Invalid parameter (check IP address, port, or machine number)";
                case 5:
                    return "ERR_NON_CARRYOUT - Operation not carried out (device may not support this operation)";
                case 6:
                    return "ERR_LOG_END - End of log data (no more records available)";
                case 7:
                    return "ERR_MEMORY - Memory error (device or SDK memory issue)";
                case 8:
                    return "ERR_MULTIUSER - Multiple user error (another connection is active)";
                default:
                    return $"UNKNOWN_ERROR - Unrecognized error code: {errorCode}";
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    CloseCommPort();
                }
                _disposed = true;
            }
        }

        ~SbxpcDllWrapper()
        {
            Dispose(false);
        }
    }
}
