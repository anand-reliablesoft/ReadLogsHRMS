using System;
using System.Runtime.InteropServices;
using BiometricAttendance.Common.Interfaces;
using BiometricAttendance.Common.Models;

namespace BiometricAttendance.Common.Services
{
    /// <summary>
    /// Wrapper for the SBXPC ActiveX control SDK
    /// Provides COM interop functionality for biometric device communication
    /// </summary>
    public class SdkWrapper : ISdkWrapper
    {
        private dynamic _sdkObject;
        private SbxpcHostForm _hostForm;
        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the SdkWrapper class
        /// </summary>
        public SdkWrapper()
        {
            try
            {
                if (Environment.UserInteractive)
                {
                    Console.WriteLine("Attempting to load SBXPC ActiveX control with host form...");
                }
                
                // Create hidden host form for the ActiveX control
                _hostForm = new SbxpcHostForm();
                
                if (Environment.UserInteractive)
                {
                    Console.WriteLine("Host form created, creating SBXPC control...");
                }
                
                // Create the SBXPC control hosted in the form
                _sdkObject = _hostForm.CreateSbxpcControl();
                
                if (Environment.UserInteractive)
                {
                    Console.WriteLine("SBXPC COM object created successfully in host form!");
                }
            }
            catch (Exception ex)
            {
                if (Environment.UserInteractive)
                {
                    Console.WriteLine($"Exception details: {ex.GetType().Name}");
                    Console.WriteLine($"Message: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"Inner: {ex.InnerException.Message}");
                    }
                }
                
                // Clean up host form if creation failed
                if (_hostForm != null)
                {
                    _hostForm.Dispose();
                    _hostForm = null;
                }
                
                throw new InvalidOperationException("Failed to initialize SBXPC SDK. Ensure the ActiveX control is properly registered.", ex);
            }
        }

        /// <summary>
        /// Sets the IP address, port, and password for device connection
        /// </summary>
        public bool SetIPAddress(string ipAddress, int port, int password)
        {
            try
            {
                if (Environment.UserInteractive)
                {
                    Console.WriteLine($"Calling SetIPAddress({ipAddress}, {port}, {password})...");
                }
                
                // Call through the AxHost wrapper
                bool result = _sdkObject.CallSetIPAddress(ipAddress, port, password);
                
                if (Environment.UserInteractive)
                {
                    Console.WriteLine($"SetIPAddress returned: {result}");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                if (Environment.UserInteractive)
                {
                    Console.WriteLine($"SetIPAddress exception: {ex.GetType().Name}");
                    Console.WriteLine($"Message: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    }
                    Console.WriteLine($"HRESULT: 0x{Marshal.GetHRForException(ex):X8}");
                }
                throw new InvalidOperationException($"Failed to set IP address {ipAddress}:{port}", ex);
            }
        }

        /// <summary>
        /// Opens the communication port to the specified machine
        /// </summary>
        public bool OpenCommPort(int machineNumber)
        {
            try
            {
                return _sdkObject.OpenCommPort(machineNumber);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to open communication port for machine {machineNumber}", ex);
            }
        }

        /// <summary>
        /// Closes the communication port
        /// </summary>
        public void CloseCommPort()
        {
            try
            {
                _sdkObject.CloseCommPort();
            }
            catch (Exception ex)
            {
                // Log but don't throw on close failures
                System.Diagnostics.Debug.WriteLine($"Warning: Failed to close communication port: {ex.Message}");
            }
        }

        /// <summary>
        /// Enables or disables the device
        /// </summary>
        public bool EnableDevice(int machineNumber, bool enable)
        {
            try
            {
                // SDK expects 1 for enable, 0 for disable
                int enableFlag = enable ? 1 : 0;
                return _sdkObject.EnableDevice(machineNumber, enableFlag);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to {(enable ? "enable" : "disable")} device {machineNumber}", ex);
            }
        }

        /// <summary>
        /// Sets the device time to current server time
        /// </summary>
        public bool SetDeviceTime(int machineNumber)
        {
            try
            {
                return _sdkObject.SetDeviceTime(machineNumber);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to set device time for machine {machineNumber}", ex);
            }
        }

        /// <summary>
        /// Gets the last error code from the SDK
        /// </summary>
        public int GetLastError()
        {
            try
            {
                return _sdkObject.GetLastError();
            }
            catch (Exception)
            {
                return -1; // Return -1 if we can't get the error code
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
                    _sdkObject.SetReadMark = value;
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
        public bool ReadGeneralLogData(int machineNumber)
        {
            try
            {
                return _sdkObject.ReadGeneralLogData(machineNumber);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to read general log data from machine {machineNumber}", ex);
            }
        }

        /// <summary>
        /// Reads all log data from the device
        /// </summary>
        public bool ReadAllGLogData(int machineNumber)
        {
            try
            {
                return _sdkObject.ReadAllGLogData(machineNumber);
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
                // Declare output parameters for COM interop
                object vTMachineNumber = null;
                object vSEnrollNumber = null;
                object vSMachineNumber = null;
                object vVerifyMode = null;
                object vYear = null;
                object vMonth = null;
                object vDay = null;
                object vHour = null;
                object vMinute = null;
                object vSecond = null;

                // Call SDK method with ref parameters for COM interop
                bool result = _sdkObject.GetGeneralLogData(
                    machineNumber,
                    ref vTMachineNumber,
                    ref vSEnrollNumber,
                    ref vSMachineNumber,
                    ref vVerifyMode,
                    ref vYear,
                    ref vMonth,
                    ref vDay,
                    ref vHour,
                    ref vMinute,
                    ref vSecond);

                if (result)
                {
                    // Create AttendanceLog from output parameters
                    log = new AttendanceLog
                    {
                        TMachineNumber = Convert.ToInt32(vTMachineNumber),
                        SMachineNumber = Convert.ToInt32(vSMachineNumber),
                        SEnrollNumber = Convert.ToInt32(vSEnrollNumber),
                        VerifyMode = Convert.ToInt32(vVerifyMode),
                        Year = Convert.ToInt32(vYear),
                        Month = Convert.ToInt32(vMonth),
                        Day = Convert.ToInt32(vDay),
                        Hour = Convert.ToInt32(vHour),
                        Minute = Convert.ToInt32(vMinute),
                        Second = Convert.ToInt32(vSecond)
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
                // Declare output parameters for COM interop
                object vTMachineNumber = null;
                object vSEnrollNumber = null;
                object vSMachineNumber = null;
                object vVerifyMode = null;
                object vYear = null;
                object vMonth = null;
                object vDay = null;
                object vHour = null;
                object vMinute = null;
                object vSecond = null;

                // Call SDK method with ref parameters for COM interop
                bool result = _sdkObject.GetAllGLogData(
                    machineNumber,
                    ref vTMachineNumber,
                    ref vSEnrollNumber,
                    ref vSMachineNumber,
                    ref vVerifyMode,
                    ref vYear,
                    ref vMonth,
                    ref vDay,
                    ref vHour,
                    ref vMinute,
                    ref vSecond);

                if (result)
                {
                    // Create AttendanceLog from output parameters
                    log = new AttendanceLog
                    {
                        TMachineNumber = Convert.ToInt32(vTMachineNumber),
                        SMachineNumber = Convert.ToInt32(vSMachineNumber),
                        SEnrollNumber = Convert.ToInt32(vSEnrollNumber),
                        VerifyMode = Convert.ToInt32(vVerifyMode),
                        Year = Convert.ToInt32(vYear),
                        Month = Convert.ToInt32(vMonth),
                        Day = Convert.ToInt32(vDay),
                        Hour = Convert.ToInt32(vHour),
                        Minute = Convert.ToInt32(vMinute),
                        Second = Convert.ToInt32(vSecond)
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

        /// <summary>
        /// Disposes the SDK wrapper and releases COM resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected dispose method
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    try
                    {
                        CloseCommPort();
                    }
                    catch
                    {
                        // Ignore errors during disposal
                    }
                    
                    // Dispose host form
                    if (_hostForm != null)
                    {
                        _hostForm.Dispose();
                        _hostForm = null;
                    }
                }

                // Release COM object
                if (_sdkObject != null)
                {
                    try
                    {
                        Marshal.ReleaseComObject(_sdkObject);
                    }
                    catch
                    {
                        // Ignore errors during COM release
                    }
                    _sdkObject = null;
                }

                _disposed = true;
            }
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~SdkWrapper()
        {
            Dispose(false);
        }
    }
}
