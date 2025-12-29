using System;
using BiometricAttendance.Common.Interfaces;

namespace BiometricAttendance.Common.Services
{
    /// <summary>
    /// Synchronizes time on biometric devices with server time
    /// </summary>
    public class TimeSynchronizer : ITimeSynchronizer
    {
        /// <summary>
        /// Synchronizes the device time with the current server time
        /// </summary>
        /// <param name="sdk">SDK wrapper instance for device communication</param>
        /// <param name="machineNumber">Machine number to synchronize</param>
        /// <param name="logger">Optional logger for recording sync operations</param>
        /// <returns>True if synchronization was successful, false otherwise</returns>
        public bool SynchronizeTime(ISdkWrapper sdk, int machineNumber, IFileLogger logger = null)
        {
            if (sdk == null)
            {
                throw new ArgumentNullException(nameof(sdk), "SDK wrapper cannot be null");
            }

            try
            {
                logger?.Log($"Starting time synchronization for machine {machineNumber}");

                // Step 1: Disable device to prevent data corruption during time update
                logger?.Log($"Disabling device {machineNumber}...");
                bool disableResult = sdk.EnableDevice(machineNumber, false);
                
                if (!disableResult)
                {
                    int errorCode = sdk.GetLastError();
                    // Use appropriate error message method based on SDK type
                    string errorMessage = sdk is SbxpcDllWrapper 
                        ? SbxpcDllWrapper.GetErrorMessage(errorCode)
                        : SdkWrapper.GetErrorMessage(errorCode);
                    logger?.LogError($"Failed to disable device {machineNumber}. Error: {errorMessage} (Code: {errorCode})", null);
                    return false;
                }

                logger?.Log($"Device {machineNumber} disabled successfully");

                // Step 2: Set device time to current server time
                logger?.Log($"Setting device time for machine {machineNumber} to {DateTime.Now:yyyy-MM-dd HH:mm:ss}...");
                bool setTimeResult = sdk.SetDeviceTime(machineNumber);
                
                if (!setTimeResult)
                {
                    int errorCode = sdk.GetLastError();
                    // Use appropriate error message method based on SDK type
                    string errorMessage = sdk is SbxpcDllWrapper 
                        ? SbxpcDllWrapper.GetErrorMessage(errorCode)
                        : SdkWrapper.GetErrorMessage(errorCode);
                    logger?.LogError($"Failed to set device time for machine {machineNumber}. Error: {errorMessage} (Code: {errorCode})", null);
                    
                    // Try to re-enable device even if time sync failed
                    try
                    {
                        sdk.EnableDevice(machineNumber, true);
                        logger?.Log($"Device {machineNumber} re-enabled after time sync failure");
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError($"Failed to re-enable device {machineNumber} after time sync failure", ex);
                    }
                    
                    return false;
                }

                logger?.Log($"Device time set successfully for machine {machineNumber}");

                // Step 3: Re-enable device for normal operation
                logger?.Log($"Re-enabling device {machineNumber}...");
                bool enableResult = sdk.EnableDevice(machineNumber, true);
                
                if (!enableResult)
                {
                    int errorCode = sdk.GetLastError();
                    // Use appropriate error message method based on SDK type
                    string errorMessage = sdk is SbxpcDllWrapper 
                        ? SbxpcDllWrapper.GetErrorMessage(errorCode)
                        : SdkWrapper.GetErrorMessage(errorCode);
                    logger?.LogError($"Failed to re-enable device {machineNumber}. Error: {errorMessage} (Code: {errorCode})", null);
                    return false;
                }

                logger?.Log($"Device {machineNumber} re-enabled successfully");
                logger?.Log($"Time synchronization completed successfully for machine {machineNumber}");
                
                return true;
            }
            catch (Exception ex)
            {
                logger?.LogError($"Exception during time synchronization for machine {machineNumber}", ex);
                
                // Attempt to re-enable device in case of exception
                try
                {
                    sdk.EnableDevice(machineNumber, true);
                    logger?.Log($"Device {machineNumber} re-enabled after exception");
                }
                catch (Exception enableEx)
                {
                    logger?.LogError($"Failed to re-enable device {machineNumber} after exception", enableEx);
                }
                
                return false;
            }
        }
    }
}
