using System;
using System.Configuration;
using System.Data;
using System.Data.Odbc;
using System.Data.OleDb;
using System.IO;
using BiometricAttendance.Common.Interfaces;

namespace BiometricAttendance.Common.Services
{
    /// <summary>
    /// Manages database connections for Access and SQL Server databases
    /// </summary>
    public class DatabaseConnectionManager : IDatabaseConnectionManager
    {
        private readonly string _accessDbPath;
        private readonly string _accessDbPassword;
        private readonly string _sqlDsnFile;
        private IFileLogger _logger;

        public DatabaseConnectionManager()
        {
            // Read configuration from App.config
            string accessDbPath = ConfigurationManager.AppSettings["AccessDbPath"] ?? "RCMSBio.mdb";
            string sqlDsnFile = ConfigurationManager.AppSettings["SqlDsnFile"] ?? "ReadLogsHRMS.dsn";
            
            // Resolve paths relative to executable directory if they are relative paths
            _accessDbPath = ResolvePathRelativeToExecutable(accessDbPath);
            _accessDbPassword = ConfigurationManager.AppSettings["AccessDbPassword"] ?? "szus";
            _sqlDsnFile = ResolvePathRelativeToExecutable(sqlDsnFile);
        }

        public DatabaseConnectionManager(string accessDbPath, string accessDbPassword, string sqlDsnFile)
        {
            _accessDbPath = ResolvePathRelativeToExecutable(accessDbPath);
            _accessDbPassword = accessDbPassword;
            _sqlDsnFile = ResolvePathRelativeToExecutable(sqlDsnFile);
        }
        
        /// <summary>
        /// Resolves a path relative to the executable directory if it's not an absolute path
        /// </summary>
        private string ResolvePathRelativeToExecutable(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;
                
            // If already absolute, return as-is
            if (Path.IsPathRooted(path))
                return path;
                
            // Get executable directory
            string exeDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            
            // Combine with executable directory
            return Path.Combine(exeDirectory, path);
        }

        /// <summary>
        /// Sets the logger for retry logging
        /// </summary>
        public void SetLogger(IFileLogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Gets a connection to the Access database using OLE DB provider
        /// </summary>
        public IDbConnection GetAccessConnection()
        {
            try
            {
                // Try ACE provider first (newer, supports both .mdb and .accdb)
                string connectionString = $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={_accessDbPath};Jet OLEDB:Database Password={_accessDbPassword};";
                
                try
                {
                    var connection = new OleDbConnection(connectionString);
                    connection.Open();
                    return connection;
                }
                catch
                {
                    // Fall back to Jet provider if ACE is not available
                    connectionString = $"Provider=Microsoft.Jet.OLEDB.4.0;Data Source={_accessDbPath};Jet OLEDB:Database Password={_accessDbPassword};";
                    var connection = new OleDbConnection(connectionString);
                    connection.Open();
                    return connection;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to connect to Access database at {_accessDbPath}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets a connection to SQL Server using ODBC DSN file
        /// </summary>
        public IDbConnection GetSqlServerConnection()
        {
            try
            {
                // Read DSN file to build connection string
                string connectionString = ReadDsnFile(_sqlDsnFile);
                
                var connection = new OdbcConnection(connectionString);
                connection.Open();
                
                return connection;
            }
            catch (Exception ex)
            {
                // Check for transient errors that should trigger retry
                if (IsTransientError(ex))
                {
                    throw new InvalidOperationException($"Transient SQL Server connection error: {ex.Message}", ex);
                }
                
                throw new InvalidOperationException($"Failed to connect to SQL Server using DSN file {_sqlDsnFile}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Closes a database connection
        /// </summary>
        public void CloseConnection(IDbConnection connection)
        {
            if (connection != null && connection.State != ConnectionState.Closed)
            {
                try
                {
                    connection.Close();
                }
                catch (Exception ex)
                {
                    // Log but don't throw - closing is best effort
                    Console.WriteLine($"Error closing connection: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Reopens a database connection (for retry logic)
        /// </summary>
        public void ReopenConnection(IDbConnection connection)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            try
            {
                // Close if open
                if (connection.State != ConnectionState.Closed)
                {
                    connection.Close();
                }

                // Reopen
                connection.Open();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to reopen connection: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Reads DSN file and builds ODBC connection string
        /// </summary>
        private string ReadDsnFile(string dsnFilePath)
        {
            if (!File.Exists(dsnFilePath))
            {
                throw new FileNotFoundException($"DSN file not found: {dsnFilePath}");
            }

            string driver = "";
            string server = "";
            string database = "";
            string uid = "";
            string pwd = "";

            // Parse DSN file
            foreach (string line in File.ReadAllLines(dsnFilePath))
            {
                string trimmedLine = line.Trim();
                
                if (trimmedLine.StartsWith("DRIVER=", StringComparison.OrdinalIgnoreCase))
                {
                    driver = trimmedLine.Substring(7);
                }
                else if (trimmedLine.StartsWith("SERVER=", StringComparison.OrdinalIgnoreCase))
                {
                    server = trimmedLine.Substring(7);
                }
                else if (trimmedLine.StartsWith("DATABASE=", StringComparison.OrdinalIgnoreCase))
                {
                    database = trimmedLine.Substring(9);
                }
                else if (trimmedLine.StartsWith("UID=", StringComparison.OrdinalIgnoreCase))
                {
                    uid = trimmedLine.Substring(4);
                }
                else if (trimmedLine.StartsWith("PWD=", StringComparison.OrdinalIgnoreCase))
                {
                    pwd = trimmedLine.Substring(4);
                }
            }

            // Build connection string
            return $"Driver={{{driver}}};Server={server};Database={database};Uid={uid};Pwd={pwd};";
        }

        /// <summary>
        /// Checks if an exception is a transient SQL Server error that should trigger retry
        /// </summary>
        private bool IsTransientError(Exception ex)
        {
            string message = ex.Message.ToLower();
            
            // Check for known transient errors
            return message.Contains("forcibly closed by the remote host") ||
                   message.Contains("not a socket");
        }

        /// <summary>
        /// Executes a database operation with automatic retry on transient errors
        /// </summary>
        public T ExecuteWithRetry<T>(IDbConnection connection, Func<IDbConnection, T> operation, string operationName = "Database operation")
        {
            try
            {
                return operation(connection);
            }
            catch (Exception ex)
            {
                // Check if this is a transient error that should trigger retry
                if (IsTransientError(ex))
                {
                    if (_logger != null)
                    {
                        _logger.Log($"Transient error detected during {operationName}: {ex.Message}");
                        _logger.Log("Attempting to reconnect and retry operation...");
                    }

                    try
                    {
                        // Close and reopen connection
                        ReopenConnection(connection);

                        if (_logger != null)
                        {
                            _logger.Log("Connection reopened successfully, retrying operation...");
                        }

                        // Retry operation once
                        T result = operation(connection);

                        if (_logger != null)
                        {
                            _logger.Log($"{operationName} succeeded after retry");
                        }

                        return result;
                    }
                    catch (Exception retryEx)
                    {
                        if (_logger != null)
                        {
                            _logger.LogError($"{operationName} failed after retry", retryEx);
                        }
                        throw new InvalidOperationException($"{operationName} failed after retry: {retryEx.Message}", retryEx);
                    }
                }
                else
                {
                    // Not a transient error, rethrow
                    throw;
                }
            }
        }

        /// <summary>
        /// Executes a database operation with automatic retry on transient errors (void return)
        /// </summary>
        public void ExecuteWithRetry(IDbConnection connection, Action<IDbConnection> operation, string operationName = "Database operation")
        {
            try
            {
                operation(connection);
            }
            catch (Exception ex)
            {
                // Check if this is a transient error that should trigger retry
                if (IsTransientError(ex))
                {
                    if (_logger != null)
                    {
                        _logger.Log($"Transient error detected during {operationName}: {ex.Message}");
                        _logger.Log("Attempting to reconnect and retry operation...");
                    }

                    try
                    {
                        // Close and reopen connection
                        ReopenConnection(connection);

                        if (_logger != null)
                        {
                            _logger.Log("Connection reopened successfully, retrying operation...");
                        }

                        // Retry operation once
                        operation(connection);

                        if (_logger != null)
                        {
                            _logger.Log($"{operationName} succeeded after retry");
                        }
                    }
                    catch (Exception retryEx)
                    {
                        if (_logger != null)
                        {
                            _logger.LogError($"{operationName} failed after retry", retryEx);
                        }
                        throw new InvalidOperationException($"{operationName} failed after retry: {retryEx.Message}", retryEx);
                    }
                }
                else
                {
                    // Not a transient error, rethrow
                    throw;
                }
            }
        }
    }
}
