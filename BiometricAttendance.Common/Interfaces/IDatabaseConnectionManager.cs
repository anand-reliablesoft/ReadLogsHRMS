using System;
using System.Data;

namespace BiometricAttendance.Common.Interfaces
{
    /// <summary>
    /// Manages database connections for Access and SQL Server databases
    /// </summary>
    public interface IDatabaseConnectionManager
    {
        /// <summary>
        /// Gets a connection to the Access database
        /// </summary>
        /// <returns>Open database connection</returns>
        IDbConnection GetAccessConnection();

        /// <summary>
        /// Gets a connection to the SQL Server database via DSN
        /// </summary>
        /// <returns>Open database connection</returns>
        IDbConnection GetSqlServerConnection();

        /// <summary>
        /// Gets a fresh SQL Server connection specifically for batch processing
        /// Uses direct connection string to avoid ODBC driver corruption issues
        /// </summary>
        /// <returns>Open database connection</returns>
        IDbConnection GetFreshSqlServerConnection();

        /// <summary>
        /// Closes a database connection
        /// </summary>
        /// <param name="connection">Connection to close</param>
        void CloseConnection(IDbConnection connection);

        /// <summary>
        /// Reopens a database connection (for retry logic)
        /// </summary>
        /// <param name="connection">Connection to reopen</param>
        void ReopenConnection(IDbConnection connection);

        /// <summary>
        /// Sets the logger for retry logging
        /// </summary>
        /// <param name="logger">Logger instance</param>
        void SetLogger(IFileLogger logger);

        /// <summary>
        /// Executes a database operation with automatic retry on transient errors
        /// </summary>
        T ExecuteWithRetry<T>(IDbConnection connection, Func<IDbConnection, T> operation, string operationName = "Database operation");

        /// <summary>
        /// Executes a database operation with automatic retry on transient errors (void return)
        /// </summary>
        void ExecuteWithRetry(IDbConnection connection, Action<IDbConnection> operation, string operationName = "Database operation");
    }
}
