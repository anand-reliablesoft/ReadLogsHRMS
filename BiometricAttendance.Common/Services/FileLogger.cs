using System;
using System.IO;
using BiometricAttendance.Common.Interfaces;

namespace BiometricAttendance.Common.Services
{
    public class FileLogger : IFileLogger
    {
        private StreamWriter _writer;
        private string _logFilePath;
        private readonly object _lockObject = new object();

        public void Initialize(string logDirectory, string prefix)
        {
            try
            {
                // Resolve log directory relative to executable if it's a relative path
                string resolvedLogDirectory = logDirectory;
                if (!Path.IsPathRooted(logDirectory))
                {
                    string exeDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                    resolvedLogDirectory = Path.Combine(exeDirectory, logDirectory);
                }
                
                if (Environment.UserInteractive)
                {
                    Console.WriteLine($"FileLogger: Initializing with directory='{logDirectory}', prefix='{prefix}'");
                    Console.WriteLine($"FileLogger: Resolved directory='{resolvedLogDirectory}'");
                    Console.WriteLine($"FileLogger: Current directory='{Directory.GetCurrentDirectory()}'");
                }
                
                // Create log directory if it doesn't exist
                if (!Directory.Exists(resolvedLogDirectory))
                {
                    if (Environment.UserInteractive)
                    {
                        Console.WriteLine($"FileLogger: Creating directory '{resolvedLogDirectory}'...");
                    }
                    Directory.CreateDirectory(resolvedLogDirectory);
                    if (Environment.UserInteractive)
                    {
                        Console.WriteLine($"FileLogger: Directory created successfully");
                    }
                }
                else
                {
                    if (Environment.UserInteractive)
                    {
                        Console.WriteLine($"FileLogger: Directory already exists");
                    }
                }

                // Create timestamped log file name (e.g., TLog20231115143022.txt)
                string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                string fileName = $"{prefix}{timestamp}.txt";
                _logFilePath = Path.Combine(resolvedLogDirectory, fileName);

                if (Environment.UserInteractive)
                {
                    Console.WriteLine($"FileLogger: Log file path='{_logFilePath}'");
                }

                // Initialize the StreamWriter
                _writer = new StreamWriter(_logFilePath, true);
                _writer.AutoFlush = true;
                
                if (Environment.UserInteractive)
                {
                    Console.WriteLine($"FileLogger: StreamWriter initialized successfully");
                }
            }
            catch (Exception ex)
            {
                if (Environment.UserInteractive)
                {
                    Console.WriteLine($"FileLogger ERROR: {ex.Message}");
                    Console.WriteLine($"FileLogger Stack: {ex.StackTrace}");
                }
                
                // If we can't create the log file, write to event log
                try
                {
                    System.Diagnostics.EventLog.WriteEntry("Application", 
                        $"Failed to initialize file logger: {ex.Message}", 
                        System.Diagnostics.EventLogEntryType.Error);
                }
                catch
                {
                    // Silently fail if we can't write to event log either
                }
            }
        }

        public void Log(string message)
        {
            if (_writer == null)
                return;

            lock (_lockObject)
            {
                try
                {
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    _writer.WriteLine($"[{timestamp}] {message}");
                }
                catch (Exception ex)
                {
                    // If we can't write to file, try event log
                    try
                    {
                        System.Diagnostics.EventLog.WriteEntry("Application", 
                            $"Failed to write to log file: {ex.Message}", 
                            System.Diagnostics.EventLogEntryType.Warning);
                    }
                    catch
                    {
                        // Silently fail
                    }
                }
            }
        }

        public void LogError(string message, Exception ex)
        {
            if (_writer == null)
                return;

            lock (_lockObject)
            {
                try
                {
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    _writer.WriteLine($"[{timestamp}] ERROR: {message}");
                    if (ex != null)
                    {
                        _writer.WriteLine($"Exception Type: {ex.GetType().Name}");
                        _writer.WriteLine($"Exception Message: {ex.Message}");
                        _writer.WriteLine($"Stack Trace: {ex.StackTrace}");
                        
                        if (ex.InnerException != null)
                        {
                            _writer.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                        }
                    }
                }
                catch (Exception writeEx)
                {
                    // If we can't write to file, try event log
                    try
                    {
                        System.Diagnostics.EventLog.WriteEntry("Application", 
                            $"Failed to write error to log file: {writeEx.Message}", 
                            System.Diagnostics.EventLogEntryType.Warning);
                    }
                    catch
                    {
                        // Silently fail
                    }
                }
            }
        }

        public void Close()
        {
            lock (_lockObject)
            {
                try
                {
                    if (_writer != null)
                    {
                        _writer.Flush();
                        _writer.Close();
                        _writer.Dispose();
                        _writer = null;
                    }
                }
                catch
                {
                    // Silently fail on close
                }
            }
        }
    }
}
