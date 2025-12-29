using System;
using System.Data;
using System.Data.OleDb;
using BiometricAttendance.Common.Interfaces;

namespace BiometricAttendance.Common.Services
{
    /// <summary>
    /// Provides access to system settings stored in Access database Settings table
    /// </summary>
    public class SettingsProvider : ISettingsProvider
    {
        private readonly string _connectionString;
        private const string SettingName = "DeleteAll";

        /// <summary>
        /// Initializes a new instance of SettingsProvider
        /// </summary>
        /// <param name="accessDbPath">Path to Access database file</param>
        /// <param name="password">Database password</param>
        public SettingsProvider(string accessDbPath, string password)
        {
            _connectionString = $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={accessDbPath};Jet OLEDB:Database Password={password};";
        }

        /// <summary>
        /// Gets the DeleteAll mode setting from Settings table
        /// </summary>
        /// <returns>True if DeleteAll mode is enabled (value = 1), false otherwise</returns>
        public bool GetDeleteAllMode()
        {
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    connection.Open();

                    string query = "SELECT SettingValue FROM Settings WHERE SettingName = ?";
                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@SettingName", SettingName);

                        var result = command.ExecuteScalar();

                        if (result != null && result != DBNull.Value)
                        {
                            string value = result.ToString();
                            return value == "1";
                        }

                        // Setting not found, return default value (false)
                        return false;
                    }
                }
            }
            catch (Exception)
            {
                // If Settings table doesn't exist or any error occurs, return default value
                return false;
            }
        }

        /// <summary>
        /// Sets the DeleteAll mode setting in Settings table
        /// </summary>
        /// <param name="value">True to enable DeleteAll mode (1), false to disable (0)</param>
        public void SetDeleteAllMode(bool value)
        {
            using (var connection = new OleDbConnection(_connectionString))
            {
                connection.Open();

                // Check if setting exists
                string checkQuery = "SELECT COUNT(*) FROM Settings WHERE SettingName = ?";
                using (var checkCommand = new OleDbCommand(checkQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@SettingName", SettingName);
                    int count = Convert.ToInt32(checkCommand.ExecuteScalar());

                    string settingValue = value ? "1" : "0";

                    if (count > 0)
                    {
                        // Update existing setting
                        string updateQuery = "UPDATE Settings SET SettingValue = ? WHERE SettingName = ?";
                        using (var updateCommand = new OleDbCommand(updateQuery, connection))
                        {
                            updateCommand.Parameters.AddWithValue("@SettingValue", settingValue);
                            updateCommand.Parameters.AddWithValue("@SettingName", SettingName);
                            updateCommand.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        // Insert new setting
                        string insertQuery = "INSERT INTO Settings (SettingName, SettingValue) VALUES (?, ?)";
                        using (var insertCommand = new OleDbCommand(insertQuery, connection))
                        {
                            insertCommand.Parameters.AddWithValue("@SettingName", SettingName);
                            insertCommand.Parameters.AddWithValue("@SettingValue", settingValue);
                            insertCommand.ExecuteNonQuery();
                        }
                    }
                }
            }
        }
    }
}
