# Implementation Plan

## Phase 1: Time Sync Service (Proof of Concept)

This phase focuses on building the Time Sync Service first to validate COM interop with SBXPC ActiveX control and basic device communication before tackling the more complex Data Collection Service.

- [x] 1. Set up project structure for Time Sync Service





  - Create Visual Studio solution with Windows Service project (TimeSyncService)
  - Add shared class library project for common components (models, interfaces, utilities)
  - Configure .NET Framework 4.7.2 target
  - Add NuGet package: System.ServiceProcess
  - Set up project references and folder structure (Models, Interfaces, Services, Utilities)
  - _Requirements: 13.1, 13.2, 14.1_

- [x] 2. Implement basic data models for Time Sync Service






  - [x] 2.1 Create MachineConfiguration class

    - Write MachineConfiguration class with properties for machine number, IP, port, password, IN/OUT flag
    - _Requirements: 2.1, 2.2, 2.3, 2.4_
  
  - [x] 2.2 Implement machine configuration provider


    - Write MachineConfigurationProvider with hardcoded 6 device configurations
    - Implement GetAllMachines() method returning all 6 machines for time sync
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5_

- [x] 3. Implement SDK wrapper and COM interop for Time Sync





  - [x] 3.1 Create COM interop wrapper for SBXPC ActiveX control


    - Add COM reference to SBXPC control in project (register if needed)
    - Create ISdkWrapper interface with time sync methods only
    - Implement SdkWrapper class wrapping SBXPC COM object
    - Implement SetIPAddress method
    - Implement OpenCommPort method
    - Implement CloseCommPort method
    - Implement EnableDevice method
    - Implement SetDeviceTime method
    - Implement GetLastError method and error code mapping
    - _Requirements: 12.1, 12.2, 12.3, 12.7, 12.8, 13.2_
  
  - [x] 3.2 Create device connection manager


    - Write DeviceConnectionManager class implementing IDeviceConnectionManager
    - Implement Connect method: SetIPAddress, OpenCommPort, verify success
    - Implement Disconnect method: CloseCommPort
    - Add error handling and logging for connection failures
    - _Requirements: 3.1, 3.5, 3.7, 3.8_

- [x] 4. Create time synchronizer component





  - Write TimeSynchronizer implementing ITimeSynchronizer
  - Implement SynchronizeTime method
  - Call EnableDevice(machineNumber, false) to disable device
  - Call SetDeviceTime(machineNumber) to sync time
  - Call EnableDevice(machineNumber, true) to re-enable device
  - Handle errors using GetLastError and log results
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5_

- [x] 5. Implement logging infrastructure for Time Sync Service






  - [x] 5.1 Create file logger

    - Write FileLogger implementing IFileLogger
    - Implement Initialize method creating timestamped log file (TLogYYYYMMDDHHMMSS.txt)
    - Implement Log method writing timestamped entries
    - Implement LogError method with exception details
    - Implement Close method for cleanup
    - Support log file rotation per machine
    - _Requirements: 8.1, 8.2, 8.3, 8.6_
  

  - [x] 5.2 Create event log writer

    - Write EventLogWriter implementing IEventLogWriter
    - Implement WriteError, WriteWarning, WriteInformation methods
    - Write to Windows Application event log
    - Include event IDs and proper severity levels
    - _Requirements: 8.4, 8.5_

- [x] 6. Implement Time Sync Service main logic





  - [x] 6.1 Create Windows service class


    - Create TimeSyncService class inheriting from ServiceBase
    - Implement OnStart method accepting command-line args
    - Implement OnStop method for graceful shutdown
    - Add service installer components (ServiceInstaller and ServiceProcessInstaller)
    - _Requirements: 14.1, 14.3, 14.5_
  
  - [x] 6.2 Implement time synchronization workflow


    - Initialize FileLogger with "TLog" prefix in TLogs directory
    - Load all 6 machine configurations using MachineConfigurationProvider
    - Loop through each machine configuration
    - For each machine: create machine-specific log file, connect via DeviceConnectionManager, call TimeSynchronizer.SynchronizeTime, disconnect
    - Log all operations (connection, sync success/failure, errors) to file
    - Continue processing remaining machines if individual machine fails
    - Close all log files on completion
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 8.1, 8.2, 8.3, 8.6_
  
  - [x] 6.3 Add global error handling


    - Wrap main workflow in try-catch block
    - Log crashes to file logger with "CRASHED" prefix
    - Write crashes to Windows event log with error severity
    - Allow graceful service exit on unhandled exceptions
    - _Requirements: 8.4, 8.5_

- [x] 7. Create configuration and deployment for Time Sync Service






  - [x] 7.1 Create App.config file

    - Add configuration for log directory path (TLogs)
    - Add any other needed settings
    - _Requirements: 8.1_
  
  - [x] 7.2 Create service installation script


    - Write PowerShell script to register SBXPC ActiveX control (regsvr32)
    - Write script to install TimeSyncService using sc.exe or installutil.exe
    - Write script to create Windows Scheduler task (daily at 2 AM)
    - Add uninstall script
    - _Requirements: 14.1, 14.4_
  
  - [x] 7.3 Build and test Time Sync Service


    - Build solution in Release mode
    - Set platform target to x86 for COM interop compatibility
    - Test service installation on Windows machine
    - Test COM interop with SBXPC control
    - Test connection to at least one biometric device
    - Test time synchronization functionality
    - Verify log file creation and content
    - Verify Windows event log entries
    - _Requirements: 12.1, 12.2, 12.3, 12.8, 13.2, 13.5_

## Phase 2: Data Collection Service

Once Time Sync Service is working successfully with COM interop and device communication validated, proceed with the Data Collection Service implementation.

- [x] 8. Extend data models for Data Collection Service





  - [x] 8.1 Create AttendanceLog class


    - Write AttendanceLog class with device log fields (TMachineNumber, SMachineNumber, SEnrollNumber, VerifyMode, Year, Month, Day, Hour, Minute, Second, InOut)
    - Implement GetDateTime() method to construct DateTime from components
    - _Requirements: 3.4, 3.5_
  
  - [x] 8.2 Create AttendanceRecord class


    - Write AttendanceRecord class for AttenInfo table structure (EmpCode, TicketNo, EntryDate, InOutFlag, EntryTime, TrfFlag, UpdateUID, Location, ErrMsg)
    - _Requirements: 6.2_
  
  - [x] 8.3 Update MachineConfigurationProvider

    - Add GetMachines(batch) method to return machines 1-4 or 5-6
    - _Requirements: 2.5, 10.1_
  
  - [x] 8.4 Create settings provider for Access database


    - Write SettingsProvider class to read/write Settings table
    - Implement GetDeleteAllMode() to read DeleteAll setting
    - Implement SetDeleteAllMode(bool) to update setting
    - Handle missing settings with default values
    - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.5_

- [x] 9. Extend SDK wrapper for log reading






  - [x] 9.1 Add log reading methods to ISdkWrapper interface

    - Add SetReadMark property setter
    - Add ReadGeneralLogData and ReadAllGLogData methods
    - Add GetGeneralLogData with output parameters for log data
    - Add GetAllGLogData with output parameters for log data
    - _Requirements: 12.4, 12.5, 12.6_
  
  - [x] 9.2 Implement log reading methods in SdkWrapper


    - Implement SetReadMark property setter
    - Implement ReadGeneralLogData and ReadAllGLogData methods
    - Implement GetGeneralLogData with proper parameter marshaling for COM interop
    - Implement GetAllGLogData with proper parameter marshaling for COM interop
    - _Requirements: 12.4, 12.5, 12.6, 3.5, 3.6_

- [x] 10. Implement database access layer






  - [x] 10.1 Create database connection manager

    - Write DatabaseConnectionManager implementing IDatabaseConnectionManager
    - Implement GetAccessConnection with OLE DB provider, file path, and password
    - Implement GetSqlServerConnection using ODBC DSN file
    - Implement connection retry logic for SQL Server transient errors
    - Add configuration reading from App.config
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 13.3, 13.4_
  

  - [x] 10.2 Implement Access database repository

    - Write AccessDatabaseRepository implementing IAccessDatabaseRepository
    - Implement ExecuteQuery and ExecuteNonQuery methods
    - Implement InsertRawLog method with duplicate check
    - Implement GetEmployeeId method querying M_Executive table
    - Add parameterized queries to prevent SQL injection
    - _Requirements: 4.1, 4.4, 5.1, 5.2, 5.3, 5.4_
  

  - [x] 10.3 Implement SQL Server repository

    - Write SqlServerRepository implementing ISqlServerRepository
    - Implement ExecuteQuery and ExecuteNonQuery methods
    - Implement InsertRawLog with duplicate check
    - Implement InsertAttendanceRecord with duplicate check
    - Implement UpdateRawLogTransferFlag method
    - Implement GetUnprocessedRawLogs query with sorting
    - Add transaction support for batch operations
    - _Requirements: 4.2, 4.4, 6.1, 6.2, 6.3, 6.4, 6.5, 7.5_

- [x] 11. Implement data processing components




  - [x] 11.1 Create raw log processor


    - Write RawLogProcessor implementing IRawLogProcessor
    - Implement SaveRawLog method calling both Access and SQL repositories
    - Implement IsDuplicate check matching all key fields
    - Add year filtering logic (BackYearBlocked >= 2023)
    - Add logging for duplicate detection and successful inserts
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5_
  
  - [x] 11.2 Implement employee mapper


    - Write EmployeeMapper implementing IEmployeeMapper
    - Implement GetEmployeeId querying M_Executive by BioID
    - Return EmpID if found and not null/empty
    - Return enrollment number as fallback
    - Handle database errors gracefully
    - _Requirements: 5.1, 5.2, 5.3, 5.4_
  
  - [x] 11.3 Create attendance record processor


    - Write AttendanceRecordProcessor implementing IAttendanceRecordProcessor
    - Implement ProcessRawLogs reading unprocessed records (vtrfFlag = 0 or null)
    - Map enrollment numbers to employee IDs using EmployeeMapper
    - Check for duplicates in AttenInfo table
    - Insert new AttendanceRecord with proper date/time formatting
    - Update vtrfFlag to '1' after successful insert
    - Process records in chronological order
    - Use transactions for batch processing
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 6.6_

- [x] 12. Create log reader component





  - Write LogReader implementing ILogReader
  - Implement ReadLogs method accepting SDK, config, and deleteAllMode
  - Set ReadMark to 1 before reading
  - Call ReadAllGLogData/GetAllGLogData when deleteAllMode is true
  - Call ReadGeneralLogData/GetGeneralLogData when deleteAllMode is false
  - Loop until SDK method returns false (no more data)
  - Yield return each AttendanceLog with InOut flag from machine config
  - Handle SDK errors using GetLastError
  - _Requirements: 3.2, 3.3, 3.4, 3.5, 3.6, 12.4, 12.5, 12.6_

- [x] 13. Implement Data Collection Service




  - [x] 13.1 Create Windows service project and main class



    - Create DataCollectionService class inheriting from ServiceBase
    - Implement OnStart method accepting command-line args (1 or 2)
    - Implement OnStop method for graceful shutdown
    - Add service installer components
    - _Requirements: 14.2, 14.3, 14.5_
  
  - [x] 13.2 Implement data collection workflow for batch 1


    - Initialize FileLogger with "Log" prefix in Logs directory
    - Read DeleteAll mode from SettingsProvider
    - Load machine configurations for batch 1 (machines 1-4) using MachineConfigurationProvider
    - Open Access and SQL Server database connections via DatabaseConnectionManager
    - Loop through each machine in batch 1
    - For each machine: create machine-specific log file, connect via DeviceConnectionManager, read logs via LogReader, save raw data via RawLogProcessor, disconnect
    - Close database connections after each machine
    - Set part2 flag based on completion
    - _Requirements: 3.1, 3.2, 3.3, 3.6, 3.7, 3.8, 7.5, 7.6, 9.1, 9.2, 9.3, 10.1_
  
  - [x] 13.3 Implement batch processing and chaining


    - Check part2 flag after batch 1 completion
    - If part2 is true, execute AttendanceRecordProcessor.ProcessRawLogs for batch processing
    - Update DeleteAll mode to false via SettingsProvider after successful processing
    - If part2 is false, launch second executable for machines 5-6 using Process.Start
    - _Requirements: 6.6, 9.4, 10.1, 10.2, 10.3_
  
  - [x] 13.4 Implement manual mode support


    - Check for command parameter "2"
    - Display configuration form for manual operation
    - Allow manual trigger of data collection
    - _Requirements: 10.4_
  
  - [x] 13.5 Add global error handling


    - Wrap main workflow in try-catch block
    - Log crashes to file logger with "CRASHED" prefix
    - Write crashes to Windows event log with error severity
    - Allow graceful service exit on unhandled exceptions
    - _Requirements: 8.4, 8.5_
-

- [x] 14. Implement error handling and resilience






  - [x] 14.1 Implement database connection retry logic


    - Detect "forcibly closed by the remote host" error in DatabaseConnectionManager
    - Detect "not a socket" error
    - Close and reopen connection on these errors
    - Retry operation once after reconnection
    - Log retry attempts
    - _Requirements: 7.3, 7.4_
  
  - [x] 14.2 Add device communication error handling




    - Continue processing remaining devices on connection failure
    - Log device errors with machine details
    - Map SDK error codes to descriptive messages in SdkWrapper
    - Don't crash service on individual device failures
    - _Requirements: 3.7, 12.7_

- [x] 15. Create configuration files and deployment for Data Collection Service







  - [x] 15.1 Create App.config file

    - Add configuration for Access database path and password
    - Add configuration for SQL DSN file path
    - Add configuration for log directory path (Logs)
    - Add configuration for BackYearBlocked value (2023)
    - _Requirements: 7.1, 7.2, 4.5_
  


  - [x] 15.2 Create DSN template file

    - Create ReadLogsHRMS.dsn template with ODBC Driver 13
    - Document server, database, and credential placeholders
    - Add deployment instructions
    - _Requirements: 7.2_

  
  - [x] 15.3 Create service installation script

    - Write script to install DataCollectionService using sc.exe or installutil.exe
    - Write script to create Windows Scheduler task (every 15 minutes during business hours)
    - Add uninstall script
    - _Requirements: 14.2, 14.4_
  

  - [x] 15.4 Build and test Data Collection Service

    - Build solution in Release mode
    - Test service installation
    - Test database connectivity (Access and SQL Server)
    - Test data collection from devices
    - Test raw log storage in both databases
    - Test employee ID mapping
    - Test attendance record processing
    - Test batch processing and chaining
    - Verify table structures match requirements
    - _Requirements: All Phase 2 requirements_

- [ ] 16. Final integration and deployment
  - [ ] 16.1 Create complete deployment package
    - Build both service executables (TimeSyncService and DataCollectionService)
    - Copy all dependencies and config files
    - Include DSN template
    - Include all installation scripts
    - Create deployment documentation with prerequisites and installation steps
    - _Requirements: 13.5, 14.4_
  
  - [ ] 16.2 Perform end-to-end system testing
    - Install both services on test server
    - Configure Windows Scheduler tasks for both services
    - Test complete workflow: time sync → data collection → batch processing
    - Verify data flow from devices to 0RawLog to AttenInfo
    - Test with all 6 devices
    - Verify no data loss or corruption
    - Test error scenarios (device offline, database unavailable)
    - Validate table structures unchanged
    - _Requirements: 11.1, 11.2, 11.3, 11.4, 11.5_

- [ ]* 17. Optional testing and validation
  - [ ]* 17.1 Create unit tests for core components
    - Write tests for MachineConfigurationProvider
    - Write tests for EmployeeMapper with mocked database
    - Write tests for RawLogProcessor duplicate detection
    - Write tests for AttendanceRecordProcessor
    - Write tests for date/time formatting logic
    - _Requirements: All_
  
  - [ ]* 17.2 Create integration tests
    - Test Access database connectivity
    - Test SQL Server connectivity via DSN
    - Test COM interop with SBXPC control
    - Test connection retry logic
    - Test transaction commit/rollback
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 12.1, 12.2, 13.2_
  
  - [ ]* 17.3 Perform performance and stress testing
    - Test with large log volumes (1000+ records)
    - Measure processing time per device
    - Test memory usage over extended periods
    - Test concurrent database access
    - _Requirements: All_
