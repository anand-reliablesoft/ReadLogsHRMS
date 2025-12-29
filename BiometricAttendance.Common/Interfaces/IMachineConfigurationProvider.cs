using System.Collections.Generic;
using BiometricAttendance.Common.Models;

namespace BiometricAttendance.Common.Interfaces
{
    /// <summary>
    /// Provides machine configuration for biometric devices
    /// </summary>
    public interface IMachineConfigurationProvider
    {
        /// <summary>
        /// Gets all machine configurations (all 6 devices)
        /// </summary>
        /// <returns>List of all machine configurations</returns>
        List<MachineConfiguration> GetAllMachines();

        /// <summary>
        /// Gets machine configurations for a specific batch
        /// </summary>
        /// <param name="batch">Batch number (1 for machines 1-4, 2 for machines 5-6)</param>
        /// <returns>List of machine configurations for the specified batch</returns>
        List<MachineConfiguration> GetMachines(int batch);
    }
}
