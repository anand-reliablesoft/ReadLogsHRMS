using System;
using System.Configuration;
using System.ServiceProcess;
using BiometricAttendance.Common.Interfaces;
using BiometricAttendance.Common.Services;

namespace TimeSyncService
{
    public partial class TimeSyncService : ServiceBase
    {
        private IFileLogger _fileLogger;
        private IEventLogWriter _eventLogWriter;
        private IMachineConfigurationProvider _configProvider;
        private IDeviceConnectionManager _connectionManager;
        private ITimeSynchronizer _timeSynchronizer;

        public TimeSyncService()
        {
            InitializeComponent();
            
            // Initialize dependencies
            _fileLogger = new FileLogger();
            _eventLogWriter = new EventLogWriter();
            _configProvider = new MachineConfigurationProvider();
            _connectionManager = new DeviceConnectionManager(_fileLogger);
            _timeSynchronizer = new TimeSynchronizer();
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                if (Environment.UserInteractive)
                {
                    Console.WriteLine("Executing time synchronization...");
                }
                
                ExecuteTimeSynchronization();
                
                if (Environment.UserInteractive)
                {
                    Console.WriteLine("Time synchronization completed.");
                }
            }
            catch (Exception ex)
            {
                if (Environment.UserInteractive)
                {
                    Console.WriteLine($"EXCEPTION in OnStart: {ex.Message}");
                    Console.WriteLine($"Stack: {ex.StackTrace}");
                }
                HandleCrash(ex);
            }
            finally
            {
                // Service should stop after execution
                if (!Environment.UserInteractive)
                {
                    Stop();
                }
            }
        }

        protected override void OnStop()
        {
            // Cleanup resources
            if (_fileLogger != null)
            {
                _fileLogger.Close();
            }
        }

        private void ExecuteTimeSynchronization()
        {
            ISdkWrapper sdk = null;
            
            try
            {
                if (Environment.UserInteractive)
                {
                    Console.WriteLine("Getting log directory from configuration...");
                }
                
                // Get log directory from configuration
                string logDirectory = ConfigurationManager.AppSettings["LogDirectory"] ?? "TLogs";
                
                if (Environment.UserInteractive)
                {
                    Console.WriteLine($"Log directory: {logDirectory}");
                    Console.WriteLine("Initializing file logger...");
                }
                
                // Initialize FileLogger with "TLog" prefix in TLogs directory
                _fileLogger.Initialize(logDirectory, "TLog");
                _fileLogger.Log("Time Sync Service started");
                
                if (Environment.UserInteractive)
                {
                    Console.WriteLine("File logger initialized successfully.");
                }
                
                // Load all 6 machine configurations
                var machines = _configProvider.GetAllMachines();
                _fileLogger.Log($"Loaded {machines.Count} machine configurations");
                
                // Create SDK wrapper instance - use DLL API for better reliability
                sdk = new SbxpcDllWrapper();
                
                // Loop through each machine configuration
                foreach (var machine in machines)
                {
                    IFileLogger machineLogger = null;
                    
                    try
                    {
                        // Create machine-specific log file
                        machineLogger = new FileLogger();
                        machineLogger.Initialize(logDirectory, $"TLog_Machine{machine.MachineNumber}");
                        machineLogger.Log($"Processing machine {machine.MachineNumber} at {machine.IPAddress}:{machine.Port}");
                        
                        // Connect to device
                        _fileLogger.Log($"Connecting to machine {machine.MachineNumber}...");
                        bool connected = _connectionManager.Connect(machine, sdk);
                        
                        if (!connected)
                        {
                            string errorMsg = $"Failed to connect to machine {machine.MachineNumber}";
                            _fileLogger.LogError(errorMsg, null);
                            machineLogger.LogError(errorMsg, null);
                            continue; // Continue processing remaining machines
                        }
                        
                        machineLogger.Log($"Connected to machine {machine.MachineNumber}");
                        _fileLogger.Log($"Connected to machine {machine.MachineNumber}");
                        
                        // Synchronize time
                        bool syncSuccess = _timeSynchronizer.SynchronizeTime(sdk, machine.MachineNumber, machineLogger);
                        
                        if (syncSuccess)
                        {
                            _fileLogger.Log($"Time synchronized successfully for machine {machine.MachineNumber}");
                            machineLogger.Log($"Time synchronized successfully");
                        }
                        else
                        {
                            string errorMsg = $"Time synchronization failed for machine {machine.MachineNumber}";
                            _fileLogger.LogError(errorMsg, null);
                            machineLogger.LogError(errorMsg, null);
                        }
                        
                        // Disconnect from device
                        _connectionManager.Disconnect(sdk);
                        machineLogger.Log($"Disconnected from machine {machine.MachineNumber}");
                        _fileLogger.Log($"Disconnected from machine {machine.MachineNumber}");
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue processing remaining machines
                        string errorMsg = $"Error processing machine {machine.MachineNumber}: {ex.Message}";
                        _fileLogger.LogError(errorMsg, ex);
                        
                        if (machineLogger != null)
                        {
                            machineLogger.LogError(errorMsg, ex);
                        }
                    }
                    finally
                    {
                        // Close machine-specific log file
                        if (machineLogger != null)
                        {
                            machineLogger.Close();
                        }
                    }
                }
                
                _fileLogger.Log("Time Sync Service completed successfully");
            }
            catch (Exception)
            {
                // This will be caught by OnStart and handled by HandleCrash
                throw;
            }
            finally
            {
                // Close all log files on completion
                if (_fileLogger != null)
                {
                    _fileLogger.Close();
                }
            }
        }

        private void HandleCrash(Exception ex)
        {
            try
            {
                // Log crash to file logger with "CRASHED" prefix
                if (_fileLogger != null)
                {
                    _fileLogger.LogError("CRASHED: Time Sync Service encountered a fatal error", ex);
                }
            }
            catch
            {
                // If file logging fails, continue to event log
            }
            
            try
            {
                // Write crash to Windows event log with error severity
                if (_eventLogWriter != null)
                {
                    _eventLogWriter.WriteError(
                        $"CRASHED: Time Sync Service encountered a fatal error: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}",
                        1000);
                }
            }
            catch
            {
                // If event log writing fails, nothing more we can do
            }
            
            // Allow graceful service exit - do not rethrow
        }
    }
}
