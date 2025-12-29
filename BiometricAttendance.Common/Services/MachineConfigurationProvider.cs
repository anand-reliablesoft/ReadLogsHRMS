using System.Collections.Generic;
using System.Linq;
using BiometricAttendance.Common.Interfaces;
using BiometricAttendance.Common.Models;

namespace BiometricAttendance.Common.Services
{
    /// <summary>
    /// Provides hardcoded machine configurations for 6 biometric devices
    /// </summary>
    public class MachineConfigurationProvider : IMachineConfigurationProvider
    {
        private readonly List<MachineConfiguration> _allMachines;

        public MachineConfigurationProvider()
        {
            // Initialize hardcoded configurations for 6 biometric devices
            // Devices alternate between IN and OUT designation
            _allMachines = new List<MachineConfiguration>
            {
                new MachineConfiguration
                {
                    MachineNumber = 1,
                    IPAddress = "192.168.2.224",
                    Port = 5005,
                    NetworkPassword = 1,
                    InOutFlag = "I"
                },
                new MachineConfiguration
                {
                    MachineNumber = 2,
                    IPAddress = "192.168.2.225",
                    Port = 5005,
                    NetworkPassword = 1,
                    InOutFlag = "O"
                },
                new MachineConfiguration
                {
                    MachineNumber = 3,
                    IPAddress = "192.168.2.226",
                    Port = 5005,
                    NetworkPassword = 1,
                    InOutFlag = "I"
                },
                new MachineConfiguration
                {
                    MachineNumber = 4,
                    IPAddress = "192.168.2.227",
                    Port = 5005,
                    NetworkPassword = 1,
                    InOutFlag = "O"
                },
                new MachineConfiguration
                {
                    MachineNumber = 5,
                    IPAddress = "192.168.2.228",
                    Port = 5005,
                    NetworkPassword = 1,
                    InOutFlag = "I"
                },
                new MachineConfiguration
                {
                    MachineNumber = 6,
                    IPAddress = "192.168.2.229",
                    Port = 5005,
                    NetworkPassword = 1,
                    InOutFlag = "O"
                }
            };
        }

        /// <summary>
        /// Gets all machine configurations (all 6 devices)
        /// </summary>
        /// <returns>List of all machine configurations</returns>
        public List<MachineConfiguration> GetAllMachines()
        {
            return _allMachines;
        }

        /// <summary>
        /// Gets machine configurations for a specific batch
        /// </summary>
        /// <param name="batch">Batch number (1 for machines 1-4, 2 for machines 5-6)</param>
        /// <returns>List of machine configurations for the specified batch</returns>
        public List<MachineConfiguration> GetMachines(int batch)
        {
            if (batch == 1)
            {
                // Return machines 1-4
                return _allMachines.Where(m => m.MachineNumber >= 1 && m.MachineNumber <= 4).ToList();
            }
            else if (batch == 2)
            {
                // Return machines 5-6
                return _allMachines.Where(m => m.MachineNumber >= 5 && m.MachineNumber <= 6).ToList();
            }
            else
            {
                // Invalid batch, return empty list
                return new List<MachineConfiguration>();
            }
        }
    }
}
