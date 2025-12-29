using System;
using System.Data;
using BiometricAttendance.Common.Interfaces;

namespace BiometricAttendance.Common.Services
{
    /// <summary>
    /// Maps biometric enrollment numbers to employee IDs
    /// </summary>
    public class EmployeeMapper : IEmployeeMapper
    {
        private readonly IAccessDatabaseRepository _accessRepository;

        public EmployeeMapper(IAccessDatabaseRepository accessRepository)
        {
            _accessRepository = accessRepository ?? throw new ArgumentNullException(nameof(accessRepository));
        }

        /// <summary>
        /// Gets employee ID from M_Executive table by biometric enrollment number
        /// </summary>
        public string GetEmployeeId(int enrollNumber, IDbConnection accessConn)
        {
            if (accessConn == null)
                throw new ArgumentNullException(nameof(accessConn));

            try
            {
                // Query M_Executive table using the repository
                string empId = _accessRepository.GetEmployeeId(enrollNumber, accessConn);
                
                // Repository already handles the logic:
                // - Returns EmpID if found and not null/empty
                // - Returns enrollment number as fallback
                return empId;
            }
            catch (Exception ex)
            {
                // Handle database errors gracefully
                // Log the error but don't crash - return enrollment number as fallback
                Console.WriteLine($"Error mapping employee ID for enrollment {enrollNumber}: {ex.Message}");
                return enrollNumber.ToString();
            }
        }
    }
}
