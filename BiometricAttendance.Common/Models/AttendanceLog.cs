using System;

namespace BiometricAttendance.Common.Models
{
    /// <summary>
    /// Represents a single attendance log entry from biometric device
    /// </summary>
    public class AttendanceLog
    {
        /// <summary>
        /// Database record ID (for tracking processed records)
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// Logical machine number (1-6)
        /// </summary>
        public int TMachineNumber { get; set; }

        /// <summary>
        /// Physical machine number from device
        /// </summary>
        public int SMachineNumber { get; set; }

        /// <summary>
        /// Biometric enrollment number
        /// </summary>
        public int SEnrollNumber { get; set; }

        /// <summary>
        /// Verification mode used for authentication
        /// </summary>
        public int VerifyMode { get; set; }

        /// <summary>
        /// Year component of timestamp
        /// </summary>
        public int Year { get; set; }

        /// <summary>
        /// Month component of timestamp
        /// </summary>
        public int Month { get; set; }

        /// <summary>
        /// Day component of timestamp
        /// </summary>
        public int Day { get; set; }

        /// <summary>
        /// Hour component of timestamp
        /// </summary>
        public int Hour { get; set; }

        /// <summary>
        /// Minute component of timestamp
        /// </summary>
        public int Minute { get; set; }

        /// <summary>
        /// Second component of timestamp
        /// </summary>
        public int Second { get; set; }

        /// <summary>
        /// IN/OUT designation: "I" for IN, "O" for OUT
        /// </summary>
        public string InOut { get; set; }

        /// <summary>
        /// Constructs DateTime from individual timestamp components
        /// </summary>
        /// <returns>DateTime representing the attendance log timestamp</returns>
        public DateTime GetDateTime()
        {
            return new DateTime(Year, Month, Day, Hour, Minute, Second);
        }
    }
}
