using System;

namespace BiometricAttendance.Common.Models
{
    /// <summary>
    /// Represents biometric device connection parameters
    /// </summary>
    public class MachineConfiguration
    {
        /// <summary>
        /// Logical machine number (1-6)
        /// </summary>
        public int MachineNumber { get; set; }

        /// <summary>
        /// IP address of the biometric device
        /// </summary>
        public string IPAddress { get; set; }

        /// <summary>
        /// TCP port number for device communication
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Network password for device authentication
        /// </summary>
        public int NetworkPassword { get; set; }

        /// <summary>
        /// IN/OUT designation: "I" for IN, "O" for OUT
        /// </summary>
        public string InOutFlag { get; set; }
    }
}
