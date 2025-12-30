using System;
using System.Configuration;
using System.Data;
using System.IO;
using BiometricAttendance.Common.Interfaces;
using BiometricAttendance.Common.Services;

namespace BatchProcessor
{
    /// <summary>
    /// Isolated batch processor for attendance records
    /// Runs in a separate process to avoid Winsock corruption from device communication
    /// </summary>
    class Program
    {
        private static IFileLogger _fileLogger;
        private static IDatabaseConnectionManager _dbConnectionManager;
        private static IAttendanceRecordProcessor _attendanceRecordProcessor;

        static int Main(string[] args)
        {
            try
            {
                Console.WriteLine("BatchProcessor starting...");
                
                // Parse command line arguments
                var arguments = ParseArguments(args);
                
                // Validate arguments
                if (!ValidateArguments(arguments))
                {
                    return 1; // Invalid arguments
                }
                
                // Initialize logging
                InitializeLogging(arguments.LogDirectory);
                
                _fileLogger.Log("BatchProcessor started in isolated process");
                _fileLogger.Log($"Arguments: DeleteAllMode={arguments.DeleteAllMode}, LogDirectory={arguments.LogDirectory}");
                
                // Validate configuration files exist
                if (!ValidateConfiguration())
                {
                    _fileLogger.LogError("Configuration validation failed", null);
                    return 1;
                }
                
                // Initialize components in fresh process (clean Winsock)
                InitializeComponents();
                
                // Execute batch processing
                ExecuteBatchProcessing(arguments.DeleteAllMode);
                
                _fileLogger.Log("BatchProcessor completed successfully");
                Console.WriteLine("BatchProcessor completed successfully");
                
                return 0; // Success
            }
            catch (Exception ex)
            {
                string errorMsg = $"BatchProcessor failed: {ex.Message}";
                
                if (_fileLogger != null)
                {
                    _fileLogger.LogError(errorMsg, ex);
                }
                
                Console.Error.WriteLine(errorMsg);
                Console.Error.WriteLine($"Stack trace: {ex.StackTrace}");
                
                return 1; // Failure
            }
            finally
            {
                // Cleanup
                if (_fileLogger != null)
                {
                    try
                    {
                        _fileLogger.Close();
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Error closing logger: {ex.Message}");
                    }
                }
            }
        }

        private static BatchArguments ParseArguments(string[] args)
        {
            var arguments = new BatchArguments
            {
                DeleteAllMode = false,
                LogDirectory = ConfigurationManager.AppSettings["LogDirectory"] ?? "Logs"
            };

            foreach (string arg in args)
            {
                if (arg.StartsWith("--deleteAllMode=", StringComparison.OrdinalIgnoreCase))
                {
                    string value = arg.Substring("--deleteAllMode=".Length);
                    bool deleteAllMode;
                    if (bool.TryParse(value, out deleteAllMode))
                    {
                        arguments.DeleteAllMode = deleteAllMode;
                    }
                }
                else if (arg.StartsWith("--logDirectory=", StringComparison.OrdinalIgnoreCase))
                {
                    arguments.LogDirectory = arg.Substring("--logDirectory=".Length).Trim('"');
                }
                else if (arg == "--help" || arg == "-h")
                {
                    ShowHelp();
                    Environment.Exit(0);
                }
            }

            return arguments;
        }

        private static void ShowHelp()
        {
            Console.WriteLine("BatchProcessor - Isolated batch processing for attendance records");
            Console.WriteLine();
            Console.WriteLine("Usage: BatchProcessor.exe [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --deleteAllMode=<true|false>  Set delete all mode (default: false)");
            Console.WriteLine("  --logDirectory=<path>         Set log directory (default: Logs)");
            Console.WriteLine("  --help, -h                    Show this help message");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  BatchProcessor.exe --deleteAllMode=false");
            Console.WriteLine("  BatchProcessor.exe --deleteAllMode=true --logDirectory=\"C:\\Logs\"");
        }

        private static void InitializeLogging(string logDirectory)
        {
            try
            {
                // Ensure log directory exists
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }
                
                _fileLogger = new FileLogger();
                _fileLogger.Initialize(logDirectory, "BatchProcessor_");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to initialize logging: {ex.Message}");
                throw;
            }
        }

        private static bool ValidateArguments(BatchArguments arguments)
        {
            if (arguments == null)
            {
                Console.Error.WriteLine("ERROR: Invalid arguments");
                return false;
            }

            if (string.IsNullOrWhiteSpace(arguments.LogDirectory))
            {
                Console.Error.WriteLine("ERROR: Log directory cannot be empty");
                return false;
            }

            return true;
        }

        private static bool ValidateConfiguration()
        {
            try
            {
                // Check if required configuration files exist
                string accessDbPath = ConfigurationManager.AppSettings["AccessDbPath"] ?? "RCMSBio.mdb";
                string sqlDsnFile = ConfigurationManager.AppSettings["SqlDsnFile"] ?? "ReadLogsHRMS.dsn";
                
                // Resolve paths relative to executable directory
                string exeDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                
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
                    Console.Error.WriteLine($"ERROR: Access database not found: {accessDbPath}");
                    return false;
                }

                if (!File.Exists(sqlDsnFile))
                {
                    Console.Error.WriteLine($"ERROR: SQL DSN file not found: {sqlDsnFile}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"ERROR: Configuration validation failed: {ex.Message}");
                return false;
            }
        }

        private static void InitializeComponents()
        {
            _fileLogger.Log("Initializing components in fresh process...");
            
            // Initialize database connection manager
            string accessDbPath = ConfigurationManager.AppSettings["AccessDbPath"] ?? "RCMSBio.mdb";
            string accessDbPassword = ConfigurationManager.AppSettings["AccessDbPassword"] ?? "szus";
            string sqlDsnFile = ConfigurationManager.AppSettings["SqlDsnFile"] ?? "ReadLogsHRMS.dsn";
            
            _dbConnectionManager = new DatabaseConnectionManager(accessDbPath, accessDbPassword, sqlDsnFile);
            _dbConnectionManager.SetLogger(_fileLogger);
            
            // Initialize repositories
            var accessRepository = new AccessDatabaseRepository();
            var sqlRepository = new SqlServerRepository();
            
            // Initialize employee mapper
            var employeeMapper = new EmployeeMapper(accessRepository);
            
            // Initialize attendance record processor
            _attendanceRecordProcessor = new AttendanceRecordProcessor(employeeMapper, sqlRepository);
            _attendanceRecordProcessor.SetConnectionManager(_dbConnectionManager);
            
            _fileLogger.Log("Components initialized successfully");
        }

        private static void ExecuteBatchProcessing(bool deleteAllMode)
        {
            IDbConnection accessConn = null;
            IDbConnection sqlConn = null;
            
            try
            {
                _fileLogger.Log("Starting batch processing of raw logs...");
                Console.WriteLine("Processing raw logs into attendance records...");
                
                // Get database connections (fresh process, clean Winsock)
                _fileLogger.Log("Getting database connections...");
                accessConn = _dbConnectionManager.GetAccessConnection();
                sqlConn = _dbConnectionManager.GetSqlServerConnection();
                
                _fileLogger.Log("Database connections established successfully");
                
                // Execute AttendanceRecordProcessor.ProcessRawLogs for batch processing
                _attendanceRecordProcessor.ProcessRawLogs(accessConn, sqlConn, _fileLogger);
                
                _fileLogger.Log("Batch processing completed successfully");
                Console.WriteLine("Batch processing completed");
                
                // Update DeleteAll mode to false after successful processing
                if (deleteAllMode)
                {
                    var settingsProvider = new BiometricAttendance.Common.Services.SettingsProvider(
                        ConfigurationManager.AppSettings["AccessDbPath"] ?? "RCMSBio.mdb",
                        ConfigurationManager.AppSettings["AccessDbPassword"] ?? "szus");
                        
                    settingsProvider.SetDeleteAllMode(false);
                    _fileLogger.Log("DeleteAll mode set to false");
                    Console.WriteLine("DeleteAll mode updated to false");
                }
            }
            catch (Exception ex)
            {
                _fileLogger.LogError("Error during batch processing", ex);
                Console.Error.WriteLine($"ERROR during batch processing: {ex.Message}");
                
                // Data integrity note: If batch processing fails, raw logs remain unprocessed
                // This ensures no data loss - the raw logs can be processed in the next run
                _fileLogger.Log("Batch processing failed - raw logs remain unprocessed for next run");
                
                throw;
            }
            finally
            {
                // Ensure connections are properly closed
                if (accessConn != null)
                {
                    try
                    {
                        _dbConnectionManager.CloseConnection(accessConn);
                        _fileLogger.Log("Access connection closed");
                    }
                    catch (Exception ex)
                    {
                        _fileLogger.LogError("Error closing Access connection", ex);
                    }
                }
                
                if (sqlConn != null)
                {
                    try
                    {
                        _dbConnectionManager.CloseConnection(sqlConn);
                        _fileLogger.Log("SQL Server connection closed");
                    }
                    catch (Exception ex)
                    {
                        _fileLogger.LogError("Error closing SQL Server connection", ex);
                    }
                }
            }
        }

        private class BatchArguments
        {
            public bool DeleteAllMode { get; set; }
            public string LogDirectory { get; set; }
        }
    }
}