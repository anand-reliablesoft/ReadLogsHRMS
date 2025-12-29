namespace BiometricAttendance.Common.Interfaces
{
    /// <summary>
    /// Provides access to system settings stored in Access database
    /// </summary>
    public interface ISettingsProvider
    {
        /// <summary>
        /// Gets the DeleteAll mode setting
        /// </summary>
        /// <returns>True if DeleteAll mode is enabled, false otherwise</returns>
        bool GetDeleteAllMode();

        /// <summary>
        /// Sets the DeleteAll mode setting
        /// </summary>
        /// <param name="value">True to enable DeleteAll mode, false to disable</param>
        void SetDeleteAllMode(bool value);
    }
}
