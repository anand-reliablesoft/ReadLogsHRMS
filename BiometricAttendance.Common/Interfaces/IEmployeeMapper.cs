using System.Data;

namespace BiometricAttendance.Common.Interfaces
{
    /// <summary>
    /// Maps biometric enrollment numbers to employee IDs
    /// </summary>
    public interface IEmployeeMapper
    {
        /// <summary>
        /// Gets employee ID from M_Executive table by biometric enrollment number
        /// </summary>
        /// <param name="enrollNumber">Biometric enrollment number</param>
        /// <param name="accessConn">Access database connection</param>
        /// <returns>Employee ID if found, otherwise enrollment number as string</returns>
        string GetEmployeeId(int enrollNumber, IDbConnection accessConn);
    }
}
