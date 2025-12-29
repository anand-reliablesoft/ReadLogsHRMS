using System;

namespace BiometricAttendance.Common.Models
{
    /// <summary>
    /// Represents processed attendance record for AttenInfo table
    /// </summary>
    public class AttendanceRecord
    {
        /// <summary>
        /// Employee code (mapped from biometric enrollment number)
        /// </summary>
        public string EmpCode { get; set; }

        /// <summary>
        /// Ticket number (always 0 for biometric attendance)
        /// </summary>
        public int TicketNo { get; set; } = 0;

        /// <summary>
        /// Date portion of attendance
        /// </summary>
        public DateTime EntryDate { get; set; }

        /// <summary>
        /// IN/OUT designation: "I" for IN, "O" for OUT
        /// </summary>
        public string InOutFlag { get; set; }

        /// <summary>
        /// Time portion of attendance
        /// </summary>
        public TimeSpan EntryTime { get; set; }

        /// <summary>
        /// Transfer flag (always 0, reserved for HR software)
        /// </summary>
        public int TrfFlag { get; set; } = 0;

        /// <summary>
        /// User ID who updated the record
        /// </summary>
        public string UpdateUID { get; set; }

        /// <summary>
        /// Location identifier
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// Error message if any
        /// </summary>
        public string ErrMsg { get; set; }
    }
}
