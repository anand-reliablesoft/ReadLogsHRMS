using System;
using BiometricAttendance.Common.Interfaces;
using BiometricAttendance.Common.Models;

namespace BiometricAttendance.Common.Services
{
    /// <summary>
    /// Manages connections to biometric devices
    /// </summary>
    public class DeviceConnectionManager : IDeviceConnectionManager
    {
        private readonly IFileLogger _logger;

        /// <summary>
        /// Initializes a new instance of the DeviceConnectionManager class
        /// </summary>
        /// <param name="logger">Logger instance for logging connection events</param>
        public DeviceConnectionManager(IFileLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Connects to a biometric device using the provided configuration
        /// </summary>
        public bool Connect(MachineConfiguration config, ISdkWrapper sdk)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            if (sdk == null)
                throw new ArgumentNullException(nameof(sdk));

            try
            {
                _logger.Log($"Attempting to connect to device {config.MachineNumber} at {config.IPAddress}:{config.Port}");

                // Check if this is the DLL wrapper (which has a Connect method)
                if (sdk is SbxpcDllWrapper dllWrapper)
                {
                    // Use the combined Connect method for DLL API
                    bool connected = dllWrapper.Connect(config.IPAddress, config.Port, config.NetworkPassword, config.MachineNumber);
                    if (connected)
                    {
                        _logger.Log($"Successfully connected to device {config.MachineNumber} at {config.IPAddress}:{config.Port}");
                        return true;
                    }
                    else
                    {
                        int errorCode = sdk.GetLastError();
                        string errorMsg = SbxpcDllWrapper.GetErrorMessage(errorCode);
                        _logger.LogError($"Failed to connect to device {config.MachineNumber} at {config.IPAddress}:{config.Port} - Error: {errorMsg} (Code: {errorCode})", null);
                        return false;
                    }
                }
                else
                {
                    // Use the old COM API (SetIPAddress + OpenCommPort)
                    // Step 1: Set IP address, port, and password
                    bool ipSet = sdk.SetIPAddress(config.IPAddress, config.Port, config.NetworkPassword);
                    if (!ipSet)
                    {
                        int errorCode = sdk.GetLastError();
                        string errorMsg = SdkWrapper.GetErrorMessage(errorCode);
                        _logger.LogError($"Failed to set IP address for device {config.MachineNumber} at {config.IPAddress}:{config.Port} - Error: {errorMsg} (Code: {errorCode})", null);
                        return false;
                    }

                    _logger.Log($"IP address set successfully for device {config.MachineNumber}");

                    // Step 2: Open communication port
                    bool portOpened = sdk.OpenCommPort(config.MachineNumber);
                    if (!portOpened)
                    {
                        int errorCode = sdk.GetLastError();
                        string errorMsg = SdkWrapper.GetErrorMessage(errorCode);
                        _logger.LogError($"Failed to open communication port for device {config.MachineNumber} at {config.IPAddress}:{config.Port} - Error: {errorMsg} (Code: {errorCode})", null);
                        return false;
                    }

                    _logger.Log($"Communication port opened successfully for device {config.MachineNumber}");

                    // Step 3: Set ReadMark to 1 (CRITICAL - required before reading data)
                    try
                    {
                        sdk.SetReadMark = 1;
                        _logger.Log($"ReadMark set to 1 for device {config.MachineNumber}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Failed to set ReadMark for device {config.MachineNumber}", ex);
                        // Continue anyway - this might not be critical
                    }

                    _logger.Log($"Successfully connected to device {config.MachineNumber} at {config.IPAddress}:{config.Port}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception while connecting to device {config.MachineNumber}", ex);
                return false;
            }
        }

        /// <summary>
        /// Disconnects from the currently connected device
        /// </summary>
        public void Disconnect(ISdkWrapper sdk)
        {
            if (sdk == null)
                throw new ArgumentNullException(nameof(sdk));

            try
            {
                _logger.Log("Closing communication port");
                sdk.CloseCommPort();
                _logger.Log("Communication port closed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception while disconnecting from device", ex);
            }
        }
    }
}
