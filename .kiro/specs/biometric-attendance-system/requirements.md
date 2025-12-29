# Requirements Document

## Introduction

This document specifies the requirements for a Biometric Attendance System that replaces a legacy Visual Basic 6 application. The system consists of two Windows services that interact with biometric devices to capture employee attendance data (IN/OUT punches), synchronize device time, and store the data in both Access and SQL Server databases for consumption by external HR software.

The system's primary responsibility is to read attendance logs from biometric machines, process the raw data, map biometric IDs to employee IDs, and prepare final attendance records in a standardized format. The external HR software handles all attendance processing, reporting, and business logic.

## Glossary

- **Biometric_Device**: Physical fingerprint/biometric attendance machine connected via TCP/IP that records employee IN/OUT punches
- **Time_Sync_Service**: Windows service that synchronizes the date and time on all biometric devices with the server time
- **Data_Collection_Service**: Windows service that reads attendance logs from biometric devices and stores them in databases
- **Raw_Log_Table**: Database table (0RawLog) that stores unprocessed attendance data directly from biometric devices
- **Attendance_Info_Table**: Database table (AttenInfo) that stores processed attendance records with employee codes for HR software consumption
- **Employee_Master_Table**: Database table (M_Executive) that maps biometric IDs to employee IDs
- **Access_Database**: Local Microsoft Access database (RCMSBio.mdb) used for employee master data and local storage
- **SQL_Server_Database**: Remote SQL Server database used for storing raw logs and final attendance information
- **Machine_Configuration**: Settings defining biometric device connection parameters (IP address, port, machine number, IN/OUT designation)
- **Delete_All_Mode**: Configuration flag that determines whether to read all logs or only new/unread logs from devices
- **Transfer_Flag**: Database field (vtrfFlag) that marks raw log records as processed after transfer to AttenInfo table
- **SDK_Component**: Third-party ActiveX control (SBXPC) that provides communication interface with biometric devices
- **SDK_Methods**: Functions provided by SDK_Component including SetIPAddress, OpenCommPort, CloseCommPort, EnableDevice, SetDeviceTime, ReadGeneralLogData, ReadAllGLogData, GetGeneralLogData, GetAllGLogData, GetLastError
- **Read_Mark**: SDK feature that tracks which logs have been read from device to enable incremental data retrieval
- **Log_File**: Text file created for each service execution containing timestamped operational logs
- **Windows_Scheduler**: Windows Task Scheduler that triggers service execution at configured intervals
- **COM_Interop**: Technology that enables .NET applications to interact with COM/ActiveX components like the biometric device SDK

## Requirements

### Requirement 1: Time Synchronization Service

**User Story:** As a system administrator, I want all biometric devices to have synchronized time with the server, so that attendance timestamps are accurate and consistent across all locations.

#### Acceptance Criteria

1. WHEN THE Time_Sync_Service executes, THE Time_Sync_Service SHALL connect to each configured Biometric_Device sequentially
2. WHEN connected to a Biometric_Device, THE Time_Sync_Service SHALL disable the device temporarily to prevent data corruption during time update
3. WHEN a Biometric_Device is disabled, THE Time_Sync_Service SHALL set the device time to match the current server system time
4. IF time synchronization fails for a Biometric_Device, THEN THE Time_Sync_Service SHALL log the error with device details and error code
5. WHEN time synchronization completes for a Biometric_Device, THE Time_Sync_Service SHALL re-enable the device for normal operation
6. WHEN THE Time_Sync_Service completes execution, THE Time_Sync_Service SHALL create a timestamped Log_File with all synchronization results

### Requirement 2: Biometric Device Configuration Management

**User Story:** As a system administrator, I want to configure multiple biometric devices with their network settings and IN/OUT designation, so that the system knows which devices to connect to and how to interpret their data.

#### Acceptance Criteria

1. THE Data_Collection_Service SHALL maintain Machine_Configuration for at least six biometric devices with unique machine numbers
2. WHEN reading Machine_Configuration, THE Data_Collection_Service SHALL retrieve IP address, port number, network password, and IN/OUT flag for each device
3. WHERE a Biometric_Device is designated as IN, THE Data_Collection_Service SHALL mark all attendance records from that device with InOutFlag value 'I'
4. WHERE a Biometric_Device is designated as OUT, THE Data_Collection_Service SHALL mark all attendance records from that device with InOutFlag value 'O'
5. THE Data_Collection_Service SHALL process devices in two batches: machines 1-4 in first execution and machines 5-6 in second execution

### Requirement 3: Attendance Data Collection from Biometric Devices

**User Story:** As a system administrator, I want the system to automatically read attendance logs from all biometric devices, so that employee punch data is captured without manual intervention.

#### Acceptance Criteria

1. WHEN THE Data_Collection_Service executes, THE Data_Collection_Service SHALL establish TCP/IP connection to each configured Biometric_Device using SDK_Component
2. WHEN Delete_All_Mode is enabled, THE Data_Collection_Service SHALL read all attendance logs from the Biometric_Device
3. WHEN Delete_All_Mode is disabled, THE Data_Collection_Service SHALL read only new unread attendance logs from the Biometric_Device using read mark functionality
4. WHEN reading attendance data, THE Data_Collection_Service SHALL retrieve machine number, enroll number, verify mode, year, month, day, hour, minute, and second for each log entry
5. WHEN invoking GetGeneralLogData or GetAllGLogData SDK_Methods, THE Data_Collection_Service SHALL pass output parameters to receive vTMachineNumber, vSEnrollNumber, vSMachineNumber, vVerifyMode, vYear, vMonth, vDay, vHour, vMinute, vSecond values
6. WHEN SDK_Methods return false indicating no more data, THE Data_Collection_Service SHALL exit the data retrieval loop for that device
7. IF connection to a Biometric_Device fails, THEN THE Data_Collection_Service SHALL log the error and continue processing remaining devices
8. WHEN attendance data retrieval completes for a device, THE Data_Collection_Service SHALL close the connection to that Biometric_Device

### Requirement 4: Raw Log Data Storage

**User Story:** As a data analyst, I want all raw attendance data stored in both Access and SQL Server databases, so that we have redundant storage and can audit the original device data.

#### Acceptance Criteria

1. WHEN THE Data_Collection_Service retrieves an attendance log entry, THE Data_Collection_Service SHALL store the record in Raw_Log_Table in Access_Database
2. WHEN THE Data_Collection_Service retrieves an attendance log entry, THE Data_Collection_Service SHALL store the record in Raw_Log_Table in SQL_Server_Database
3. WHEN storing to Raw_Log_Table, THE Data_Collection_Service SHALL include vTMachineNumber, vSMachineNumber, vSEnrollNumber, vVerifyMode, vYear, vMonth, vDay, vHour, vMinute, vSecond, and vInOut fields
4. IF a duplicate attendance record exists in Raw_Log_Table with matching machine number, enroll number, date, time, and IN/OUT flag, THEN THE Data_Collection_Service SHALL skip insertion and log duplicate detection
5. WHEN storing raw log data, THE Data_Collection_Service SHALL filter records to include only entries from year 2023 onwards based on BackYearBlocked configuration

### Requirement 5: Employee ID Mapping

**User Story:** As an HR manager, I want biometric enrollment numbers automatically mapped to employee codes, so that attendance records use our standard employee identifiers.

#### Acceptance Criteria

1. WHEN processing an attendance record, THE Data_Collection_Service SHALL query Employee_Master_Table using the biometric enrollment number (BioID)
2. IF a matching employee record exists in Employee_Master_Table, THEN THE Data_Collection_Service SHALL retrieve the corresponding EmpID
3. IF no matching employee record exists in Employee_Master_Table, THEN THE Data_Collection_Service SHALL use the biometric enrollment number as the employee code
4. IF the EmpID field in Employee_Master_Table is null or empty, THEN THE Data_Collection_Service SHALL use the biometric enrollment number as the employee code

### Requirement 6: Processed Attendance Data Storage

**User Story:** As an HR software integration developer, I want processed attendance records stored in a standardized AttenInfo table format, so that our HR software can consume the data without modification.

#### Acceptance Criteria

1. WHEN processing raw logs in batch mode, THE Data_Collection_Service SHALL read all unprocessed records from Raw_Log_Table where Transfer_Flag is null or zero
2. WHEN creating an attendance record, THE Data_Collection_Service SHALL populate Attendance_Info_Table with EmpCode, TicketNo (set to 0), EntryDate, InOutFlag, EntryTime, and TrfFlag (set to 0)
3. IF a duplicate attendance record exists in Attendance_Info_Table with matching EmpCode, EntryDate, InOutFlag, and EntryTime, THEN THE Data_Collection_Service SHALL skip insertion and log duplicate detection
4. WHEN an attendance record is successfully inserted into Attendance_Info_Table, THE Data_Collection_Service SHALL update the corresponding Raw_Log_Table record Transfer_Flag to '1'
5. THE Data_Collection_Service SHALL process attendance records in chronological order sorted by enroll number, year, month, day, hour, minute, and second
6. WHEN batch processing completes, THE Data_Collection_Service SHALL commit all database transactions atomically

### Requirement 7: Database Connection Management

**User Story:** As a system administrator, I want the system to handle database connection failures gracefully, so that temporary network issues don't crash the services.

#### Acceptance Criteria

1. WHEN THE Data_Collection_Service starts, THE Data_Collection_Service SHALL establish connection to Access_Database using configured file path and password
2. WHEN THE Data_Collection_Service starts, THE Data_Collection_Service SHALL establish connection to SQL_Server_Database using DSN file with credentials
3. IF SQL_Server_Database connection encounters error "TCP Provider: An existing connection was forcibly closed by the remote host", THEN THE Data_Collection_Service SHALL close and reopen the connection
4. IF SQL_Server_Database connection encounters error "TCP Provider: An operation was attempted on something that is not a socket", THEN THE Data_Collection_Service SHALL close and reopen the connection
5. WHEN THE Data_Collection_Service completes processing a device, THE Data_Collection_Service SHALL close database connections before processing the next device
6. WHEN THE Data_Collection_Service completes execution, THE Data_Collection_Service SHALL close all open database connections

### Requirement 8: Operational Logging and Monitoring

**User Story:** As a system administrator, I want detailed logs of all service operations, so that I can troubleshoot issues and verify successful execution.

#### Acceptance Criteria

1. WHEN a service starts execution, THE service SHALL create a timestamped Log_File in the Logs subdirectory with format "Log" or "TLog" followed by YYYYMMDDHHMMSS
2. WHEN processing each Biometric_Device, THE service SHALL create a separate Log_File with machine number suffix
3. WHEN a significant operation occurs, THE service SHALL write a log entry with timestamp and descriptive message to the Log_File
4. WHEN an error occurs, THE service SHALL write error details including error number and description to the Log_File
5. WHEN an error occurs, THE service SHALL log the error to Windows Application Event Log with error severity
6. WHEN a service completes execution, THE service SHALL close the Log_File

### Requirement 9: Delete All Mode Configuration

**User Story:** As a system administrator, I want to control whether the system reads all historical data or only new data from devices, so that I can perform full data refreshes when needed.

#### Acceptance Criteria

1. WHEN THE Data_Collection_Service starts, THE Data_Collection_Service SHALL read Delete_All_Mode setting from Settings table in Access_Database
2. WHERE Delete_All_Mode is enabled (value = 1), THE Data_Collection_Service SHALL read all attendance logs from biometric devices
3. WHERE Delete_All_Mode is disabled (value = 0), THE Data_Collection_Service SHALL read only new unread attendance logs from biometric devices
4. WHEN Delete_All_Mode processing completes successfully, THE Data_Collection_Service SHALL update Settings table to set Delete_All_Mode to disabled (value = 0)
5. IF Settings table does not contain Delete_All_Mode entry, THEN THE Data_Collection_Service SHALL insert a new record with Delete_All_Mode disabled

### Requirement 10: Multi-Stage Data Processing

**User Story:** As a system administrator, I want the system to process devices in stages and chain executions, so that large device deployments don't timeout or overwhelm system resources.

#### Acceptance Criteria

1. WHEN THE Data_Collection_Service executes with command parameter "1", THE Data_Collection_Service SHALL process machines 1-4 and set part2 flag to true
2. WHEN part2 flag is false after device processing, THE Data_Collection_Service SHALL launch second executable to process machines 5-6
3. WHEN part2 flag is true, THE Data_Collection_Service SHALL execute batch processing to transfer raw logs to Attendance_Info_Table
4. WHEN THE Data_Collection_Service executes with command parameter "2", THE Data_Collection_Service SHALL display configuration form for manual operation
5. THE Data_Collection_Service SHALL execute as a command-line application compatible with Windows_Scheduler

### Requirement 11: Table Structure Preservation

**User Story:** As an HR software vendor, I want the database table structures to remain unchanged, so that our existing HR software integration continues to work without modification.

#### Acceptance Criteria

1. THE system SHALL maintain the existing Raw_Log_Table structure with fields: ID, vTMachineNumber, vSMachineNumber, vSEnrollNumber, vVerifyMode, vYear, vMonth, vDay, vHour, vMinute, vSecond, vInOut, vtrfFlag
2. THE system SHALL maintain the existing Attendance_Info_Table structure with fields: Srno, EmpCode, TicketNo, EntryDate, InOutFlag, EntryTime, TrfFlag, UpdateUID, Location, ErrMsg
3. THE system SHALL maintain the existing Employee_Master_Table structure with fields including EmpID and BioID
4. THE system SHALL not modify, add, or remove any columns from these three tables
5. THE system SHALL maintain existing data types and constraints for all table fields

### Requirement 12: Biometric Device SDK Integration

**User Story:** As a developer, I want to integrate with the biometric device manufacturer's SDK, so that the system can communicate with the devices using their proprietary protocol.

#### Acceptance Criteria

1. THE system SHALL utilize the SBXPC ActiveX/COM component provided by the biometric device manufacturer for all device communication
2. WHEN establishing device connection, THE system SHALL invoke SetIPAddress SDK_Method with IP address, port number, and network password parameters
3. WHEN opening communication channel, THE system SHALL invoke OpenCommPort SDK_Method with machine number parameter and verify successful connection
4. WHEN reading attendance logs, THE system SHALL set Read_Mark to 1 to enable incremental log retrieval tracking on the device
5. WHEN reading new logs only, THE system SHALL invoke ReadGeneralLogData and GetGeneralLogData SDK_Methods to retrieve unread entries
6. WHEN reading all logs, THE system SHALL invoke ReadAllGLogData and GetAllGLogData SDK_Methods to retrieve complete log history
7. WHEN SDK_Methods return false, THE system SHALL invoke GetLastError SDK_Method to retrieve error code and log the error description
8. WHEN closing device connection, THE system SHALL invoke CloseCommPort SDK_Method to release communication resources
9. THE system SHALL be implemented in a language that supports COM_Interop to interact with the ActiveX SDK_Component

### Requirement 13: Technology Stack and Language Selection

**User Story:** As a development team, I want to select appropriate modern technologies that support Windows services, COM interop, and database connectivity, so that the system is maintainable and compatible with existing infrastructure.

#### Acceptance Criteria

1. THE system SHALL be implemented using a programming language that supports Windows service development
2. THE system SHALL be implemented using a programming language that supports COM_Interop for ActiveX component integration
3. THE system SHALL be implemented using a programming language that supports ADO.NET or equivalent for SQL Server database connectivity
4. THE system SHALL be implemented using a programming language that supports OLE DB for Microsoft Access database connectivity
5. THE system SHALL support deployment on Windows Server operating systems with .NET Framework or equivalent runtime
6. THE system SHALL provide mechanism to register and interact with the SBXPC ActiveX control on the target Windows system

### Requirement 14: Windows Service Implementation

**User Story:** As a system administrator, I want both time synchronization and data collection to run as Windows services, so that they execute automatically without user login and can be managed through Windows service controls.

#### Acceptance Criteria

1. THE Time_Sync_Service SHALL be implemented as a Windows service that can be installed, started, stopped, and uninstalled using Windows service management tools
2. THE Data_Collection_Service SHALL be implemented as a Windows service that can be installed, started, stopped, and uninstalled using Windows service management tools
3. WHEN Windows_Scheduler triggers a service, THE service SHALL execute its main processing logic and terminate upon completion
4. THE services SHALL run under a configured Windows service account with appropriate database and network permissions
5. THE services SHALL be configurable to run on-demand via Windows_Scheduler rather than as continuously running services
