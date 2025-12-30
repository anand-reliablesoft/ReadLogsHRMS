using System;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using BiometricAttendance.Common.Interfaces;
using BiometricAttendance.Common.Services;

namespace DataCollectionService
{
    public partial class DataCollectionService : ServiceBase
    {
        private IFileLogger _fileLogger;
        private IEventLogWriter _eventLogWriter;
        private ISettingsProvider _settingsProvider;
        private IMachineConfigurationProvider _configProvider;
        private IDatabaseConnectionManager _dbConnectionManager;
        private IDeviceConnectionManager _deviceConnectionManager;
        private ILogReader _logReader;
        private IRawLogProcessor _rawLogProcessor;
        private IAttendanceRecordProcessor _attendanceRecordProcessor;
        
        private string _commandParameter;

        public DataCollectionService()
        {
            InitializeComponent();
            
            // Initialize dependencies
            _fileLogger = new FileLogger();
            _eventLogWriter = new EventLogWriter();
            
            string accessDbPath = ConfigurationManager.AppSettings["AccessDbPath"] ?? "RCMSBio.mdb";
            string accessDbPassword = ConfigurationManager.AppSettings["AccessDbPassword"] ?? "szus";
            string sqlDsnFile = ConfigurationManager.AppSettings["SqlDsnFile"] ?? "ReadLogsHRMS.dsn";
            
            _settingsProvider = new BiometricAttendance.Common.Services.SettingsProvider(accessDbPath, accessDbPassword);
            _configProvider = new MachineConfigurationProvider();
            _dbConnectionManager = new DatabaseConnectionManager(accessDbPath, accessDbPassword, sqlDsnFile);
            _deviceConnectionManager = new DeviceConnectionManager(_fileLogger);
            _logReader = new LogReader();
            
            // Create shared repositories
            var accessRepository = new AccessDatabaseRepository();
            var sqlRepository = new SqlServerRepository();
            
            // Create employee mapper with access repository
            var employeeMapper = new EmployeeMapper(accessRepository);
            
            _rawLogProcessor = new RawLogProcessor(employeeMapper);
            _attendanceRecordProcessor = new AttendanceRecordProcessor(employeeMapper, sqlRepository);
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                // Accept command-line args (1 or 2)
                _commandParameter = args != null && args.Length > 0 ? args[0] : "1";
                
                if (Environment.UserInteractive)
                {
                    Console.WriteLine($"Data Collection Service started with parameter: {_commandParameter}");
                }
                
                // Check for manual mode (parameter "2")
                if (_commandParameter == "2")
                {
                    ExecuteManualMode();
                }
                else
                {
                    ExecuteDataCollection();
                }
                
                if (Environment.UserInteractive)
                {
                    Console.WriteLine("Data Collection Service completed.");
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

        private void ExecuteDataCollection()
        {
            IDbConnection accessConn = null;
            IDbConnection sqlConn = null;
            ISdkWrapper sdk = null;
            
            try
            {
                // Validate configuration before starting
                if (!ValidateConfiguration())
                {
                    throw new InvalidOperationException("Configuration validation failed");
                }
                
                // Get log directory from configuration
                string logDirectory = ConfigurationManager.AppSettings["LogDirectory"] ?? "Logs";
                
                if (Environment.UserInteractive)
                {
                    Console.WriteLine($"Log directory: {logDirectory}");
                    Console.WriteLine("Initializing file logger...");
                }
                
                // Initialize FileLogger with "Log" prefix in Logs directory
                _fileLogger.Initialize(logDirectory, "Log");
                _fileLogger.Log("Data Collection Service started");
                
                // Set logger on database connection manager for retry logging
                _dbConnectionManager.SetLogger(_fileLogger);
                
                // Set connection manager on raw log processor for retry logic
                _rawLogProcessor.SetConnectionManager(_dbConnectionManager);
                
                // Set connection manager on attendance record processor for retry logic
                _attendanceRecordProcessor.SetConnectionManager(_dbConnectionManager);
                
                // Set logger on log reader for device error logging
                _logReader.SetLogger(_fileLogger);
                
                // Read DeleteAll mode from SettingsProvider
                bool deleteAllMode = _settingsProvider.GetDeleteAllMode();
                _fileLogger.Log($"DeleteAll mode: {deleteAllMode}");
                
                if (Environment.UserInteractive)
                {
                    Console.WriteLine($"DeleteAll mode: {deleteAllMode}");
                }
                
                // Load all machine configurations (all 6 machines)
                var machines = _configProvider.GetAllMachines();
                _fileLogger.Log($"Loaded {machines.Count} machine configurations");
                
                if (Environment.UserInteractive)
                {
                    Console.WriteLine($"Processing {machines.Count} machines...");
                }
                
                // Create SDK wrapper instance
                sdk = new SbxpcDllWrapper();
                
                // Loop through each machine in batch 1
                foreach (var machine in machines)
                {
                    IFileLogger machineLogger = null;
                    
                    try
                    {
                        // CRITICAL FIX: Get fresh connections for each machine
                        // This prevents connection corruption from accumulating across machines
                        accessConn = _dbConnectionManager.GetAccessConnection();
                        sqlConn = _dbConnectionManager.GetSqlServerConnection();
                        
                        // Create machine-specific log file
                        machineLogger = new FileLogger();
                        machineLogger.Initialize(logDirectory, $"Log_Machine{machine.MachineNumber}_");
                        machineLogger.Log($"Processing machine {machine.MachineNumber} at {machine.IPAddress}:{machine.Port}");
                        
                        _fileLogger.Log($"Processing machine {machine.MachineNumber}...");
                        
                        if (Environment.UserInteractive)
                        {
                            Console.WriteLine($"  Machine {machine.MachineNumber}: Connecting...");
                        }
                        
                        // Connect via DeviceConnectionManager
                        bool connected = _deviceConnectionManager.Connect(machine, sdk);
                        
                        if (!connected)
                        {
                            string errorMsg = $"Failed to connect to machine {machine.MachineNumber}";
                            _fileLogger.LogError(errorMsg, null);
                            machineLogger.LogError(errorMsg, null);
                            
                            if (Environment.UserInteractive)
                            {
                                Console.WriteLine($"  Machine {machine.MachineNumber}: Connection failed");
                            }
                            
                            continue; // Continue processing remaining machines
                        }
                        
                        machineLogger.Log($"Connected to machine {machine.MachineNumber}");
                        _fileLogger.Log($"Connected to machine {machine.MachineNumber}");
                        
                        if (Environment.UserInteractive)
                        {
                            Console.WriteLine($"  Machine {machine.MachineNumber}: Reading logs...");
                        }
                        
                        // Read logs via LogReader
                        var logs = _logReader.ReadLogs(sdk, machine, deleteAllMode);
                        
                        int logCount = 0;
                        foreach (var log in logs)
                        {
                            // Save raw data via RawLogProcessor
                            _rawLogProcessor.SaveRawLog(log, accessConn, sqlConn, machineLogger);
                            logCount++;
                        }
                        
                        machineLogger.Log($"Processed {logCount} logs from machine {machine.MachineNumber}");
                        _fileLogger.Log($"Processed {logCount} logs from machine {machine.MachineNumber}");
                        
                        if (Environment.UserInteractive)
                        {
                            Console.WriteLine($"  Machine {machine.MachineNumber}: Processed {logCount} logs");
                        }
                        
                        // Disconnect
                        _deviceConnectionManager.Disconnect(sdk);
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
                        
                        if (Environment.UserInteractive)
                        {
                            Console.WriteLine($"  Machine {machine.MachineNumber}: ERROR - {ex.Message}");
                        }
                    }
                    finally
                    {
                        // CRITICAL: Close database connections after each machine
                        // This prevents connection corruption from accumulating
                        if (accessConn != null)
                        {
                            try
                            {
                                _dbConnectionManager.CloseConnection(accessConn);
                                accessConn = null;
                            }
                            catch (Exception ex)
                            {
                                _fileLogger.LogError($"Error closing Access connection for machine {machine.MachineNumber}", ex);
                            }
                        }
                        
                        if (sqlConn != null)
                        {
                            try
                            {
                                _dbConnectionManager.CloseConnection(sqlConn);
                                sqlConn = null;
                            }
                            catch (Exception ex)
                            {
                                _fileLogger.LogError($"Error closing SQL connection for machine {machine.MachineNumber}", ex);
                            }
                        }
                        
                        // Close machine-specific log file
                        if (machineLogger != null)
                        {
                            machineLogger.Close();
                        }
                    }
                }
                
                // All machines processed, now launch batch processor in separate process
                _fileLogger.Log("All machines processed successfully");
                
                if (Environment.UserInteractive)
                {
                    Console.WriteLine("All machines processed successfully");
                }
                
                // CRITICAL FIX: Launch batch processing in separate process to avoid Winsock corruption
                // This completely isolates batch processing from device communication
                LaunchBatchProcessor(deleteAllMode);
                
                _fileLogger.Log("DataCollectionService completed - batch processing launched in separate process");
                
                if (Environment.UserInteractive)
                {
                    Console.WriteLine("DataCollectionService completed - batch processing launched in separate process");
                }
            }
            catch (Exception)
            {
                // This will be caught by OnStart and handled by HandleCrash
                throw;
            }
            finally
            {
                // Cleanup
                if (sdk != null)
                {
                    sdk.Dispose();
                }
                
                if (accessConn != null)
                {
                    _dbConnectionManager.CloseConnection(accessConn);
                }
                
                if (sqlConn != null)
                {
                    _dbConnectionManager.CloseConnection(sqlConn);
                }
                
                if (_fileLogger != null)
                {
                    _fileLogger.Close();
                }
            }
        }

        private void LaunchBatchProcessor(bool deleteAllMode)
        {
            Process process = null;
            
            try
            {
                _fileLogger.Log("Launching BatchProcessor in separate process...");
                
                // Get the directory where DataCollectionService.exe is located
                string serviceDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string batchProcessorPath = Path.Combine(serviceDirectory, "BatchProcessor.exe");
                
                if (!File.Exists(batchProcessorPath))
                {
                    throw new FileNotFoundException($"BatchProcessor.exe not found at: {batchProcessorPath}");
                }
                
                // Get log directory from configuration
                string logDirectory = ConfigurationManager.AppSettings["LogDirectory"] ?? "Logs";
                
                // Ensure log directory is absolute path
                if (!Path.IsPathRooted(logDirectory))
                {
                    logDirectory = Path.Combine(serviceDirectory, logDirectory);
                }
                
                // Build command line arguments
                string arguments = $"--deleteAllMode={deleteAllMode.ToString().ToLower()} --logDirectory=\"{logDirectory}\"";
                
                _fileLogger.Log($"Launching: {batchProcessorPath} {arguments}");
                
                var startInfo = new ProcessStartInfo
                {
                    FileName = batchProcessorPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = serviceDirectory
                };
                
                if (Environment.UserInteractive)
                {
                    Console.WriteLine($"Launching BatchProcessor: {batchProcessorPath}");
                    Console.WriteLine($"Arguments: {arguments}");
                }
                
                process = Process.Start(startInfo);
                
                if (process == null)
                {
                    throw new InvalidOperationException("Failed to start BatchProcessor process");
                }
                
                // Read output for logging
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                
                process.WaitForExit();
                
                if (!string.IsNullOrEmpty(output))
                {
                    _fileLogger.Log($"BatchProcessor output: {output}");
                }
                
                if (!string.IsNullOrEmpty(error))
                {
                    _fileLogger.LogError($"BatchProcessor error output: {error}", null);
                }
                
                if (process.ExitCode == 0)
                {
                    _fileLogger.Log("BatchProcessor completed successfully");
                    
                    if (Environment.UserInteractive)
                    {
                        Console.WriteLine("BatchProcessor completed successfully");
                    }
                }
                else
                {
                    string errorMsg = $"BatchProcessor failed with exit code: {process.ExitCode}";
                    _fileLogger.LogError(errorMsg, null);
                    
                    if (Environment.UserInteractive)
                    {
                        Console.WriteLine($"ERROR: {errorMsg}");
                    }
                    
                    throw new InvalidOperationException(errorMsg);
                }
            }
            catch (Exception ex)
            {
                _fileLogger.LogError("Failed to launch BatchProcessor", ex);
                
                if (Environment.UserInteractive)
                {
                    Console.WriteLine($"ERROR launching BatchProcessor: {ex.Message}");
                }
                
                throw;
            }
            finally
            {
                // Ensure process is properly disposed
                if (process != null)
                {
                    try
                    {
                        if (!process.HasExited)
                        {
                            process.Kill();
                        }
                        process.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _fileLogger?.LogError("Error disposing BatchProcessor process", ex);
                    }
                }
            }
        }

        private void ExecuteManualMode()
        {
            try
            {
                if (Environment.UserInteractive)
                {
                    Console.WriteLine("Manual mode activated");
                    Console.WriteLine("Displaying configuration form...");
                }
                
                // Display configuration form for manual operation
                using (var configForm = new ConfigurationForm())
                {
                    var result = configForm.ShowDialog();
                    
                    if (result == System.Windows.Forms.DialogResult.OK && configForm.TriggerDataCollection)
                    {
                        if (Environment.UserInteractive)
                        {
                            Console.WriteLine("User triggered data collection");
                        }
                        
                        // Allow manual trigger of data collection
                        ExecuteDataCollection();
                    }
                    else
                    {
                        if (Environment.UserInteractive)
                        {
                            Console.WriteLine("User cancelled data collection");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (Environment.UserInteractive)
                {
                    Console.WriteLine($"ERROR in manual mode: {ex.Message}");
                }
                
                throw;
            }
        }

        private void HandleCrash(Exception ex)
        {
            try
            {
                // Log crash to file logger with "CRASHED" prefix
                if (_fileLogger != null)
                {
                    _fileLogger.LogError("CRASHED: Data Collection Service encountered a fatal error", ex);
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
                        $"CRASHED: Data Collection Service encountered a fatal error: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}",
                        2000);
                }
            }
            catch
            {
                // If event log writing fails, nothing more we can do
            }
            
            // Allow graceful service exit - do not rethrow
        }

        private bool ValidateConfiguration()
        {
            try
            {
                // Check if required configuration files exist
                string accessDbPath = ConfigurationManager.AppSettings["AccessDbPath"] ?? "RCMSBio.mdb";
                string sqlDsnFile = ConfigurationManager.AppSettings["SqlDsnFile"] ?? "ReadLogsHRMS.dsn";
                
                // Resolve paths relative to executable directory
                string exeDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                
                if (!Path.IsPathRooted(accessDbPath))
                {
                    accessDbPath = Path.Combine(exeDirectory, accessDbPath);
                }
                
                if (!Path.IsPathRooted(sqlDsnFile))
                {
                    sqlDsnFile = Path.Combine(exeDirectory, sqlDsnFile);
                }

                if (!File.Exists(accessDbPath))
                {
                    string errorMsg = $"Access database not found: {accessDbPath}";
                    if (Environment.UserInteractive)
                    {
                        Console.WriteLine($"ERROR: {errorMsg}");
                    }
                    if (_fileLogger != null)
                    {
                        _fileLogger.LogError(errorMsg, null);
                    }
                    return false;
                }

                if (!File.Exists(sqlDsnFile))
                {
                    string errorMsg = $"SQL DSN file not found: {sqlDsnFile}";
                    if (Environment.UserInteractive)
                    {
                        Console.WriteLine($"ERROR: {errorMsg}");
                    }
                    if (_fileLogger != null)
                    {
                        _fileLogger.LogError(errorMsg, null);
                    }
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                string errorMsg = $"Configuration validation failed: {ex.Message}";
                if (Environment.UserInteractive)
                {
                    Console.WriteLine($"ERROR: {errorMsg}");
                }
                if (_fileLogger != null)
                {
                    _fileLogger.LogError(errorMsg, ex);
                }
                return false;
            }
        }
    }
}
