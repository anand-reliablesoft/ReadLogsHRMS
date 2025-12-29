# Design Document

## Overview

The Biometric Attendance System is a Windows-based solution that replaces a legacy VB6 application with a modern .NET implementation. The system consists of two independent Windows services that run on-demand via Windows Task Scheduler:

1. **Time Synchronization Service** - Synchronizes system time across all biometric devices
2. **Data Collection Service** - Reads attendance logs from biometric devices and processes them into database tables

The system uses C# with .NET Framework to leverage native COM Interop capabilities for integrating with the proprietary SBXPC ActiveX control provided by the biometric device manufacturer. Data is stored redundantly in both Microsoft Access (local) and SQL Server (remote) databases to ensure data integrity and provide audit trails.

## Architecture

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    Windows Task Scheduler                        │
└────────────┬──────────────────────────────────┬─────────────────┘
             │                                   │
             ▼                                   ▼
┌────────────────────────┐          ┌──────────────────────────────┐
│  Time Sync Service     │          │  Data Collection Service     │
│  (Windows Service)     │          │  (Windows Service)           │
└────────┬───────────────┘          └──────────┬───────────────────┘
         │                                      │
         │                                      │
         ▼                                      ▼
┌─────────────────────────────────────────────────────────────────┐
│              SBXPC ActiveX SDK (COM Interop)                     │
└────────────┬────────────────────────────────────────────────────┘
             │
             ▼
┌─────────────────────────────────────────────────────────────────┐
│         Biometric Devices (6 devices via TCP/IP)                 │
│  Device 1-4: 192.168.2.224-227 (IN/OUT alternating)            │
│  Device 5-6: 192.168.2.228-229 (IN/OUT alternating)            │
└─────────────────────────────────────────────────────────────────┘

                           │
                           ▼
┌──────────────────────────────────────────────────────────────────┐
│                      Data Storage Layer                          │
│  ┌────────────────────┐         ┌──────────────────────────┐   │
│  │  Access Database   │         │  SQL Server Database     │   │
│  │  (RCMSBio.mdb)    │         │  (via DSN)               │   │
│  │  - M_Executive     │         │  - 0RawLog               │   │
│  │  - Settings        │         │  - AttenInfo             │   │
│  │  - 0RawLog (local) │         │  - M_Executive (sync)    │   │
│  └────────────────────┘         └──────────────────────────┘   │
└──────────────────────────────────────────────────────────────────┘
```

### Component Architecture

```
Data Collection Service
├── Service Host (Windows Service Framework)
├── Configuration Manager
│   ├── Machine Configuration Provider
│   ├── Database Connection Manager
│   └── Settings Provider
├── Device Communication Layer
│   ├── SDK Wrapper (COM Interop)
│   ├── Device Connection Manager
│   └── Log Reader
├── Data Processing Layer
│   ├── Raw Log Processor
│   ├── Employee Mapper
│   └── Attendance Record Processor
├── Data Access Layer
│   ├── Access Database Repository
│   ├── SQL Server Repository
│   └── Transaction Manager
└── Logging & Error Handling
    ├── File Logger
    └── Event Log Writer

Time Sync Service
├── Service Host (Windows Service Framework)
├── Configuration Manager
│   └── Machine Configuration Provider
├── Device Communication Layer
│   ├── SDK Wrapper (COM Interop)
│   └── Time Synchronizer
└── Logging & Error Handling
    ├── File Logger
    └── Event Log Writer
```

## Components and Interfaces

### 1. Service Host Components

#### TimeSyncService


**Purpose**: Windows service executable that synchronizes time across biometric devices

**Key Responsibilities**:
- Accept command-line parameters for execution mode
- Initialize logging infrastructure
- Load machine configurations
- Orchestrate time synchronization workflow
- Handle graceful shutdown and cleanup

**Interface**:
```csharp
public class TimeSyncService : ServiceBase
{
    protected override void OnStart(string[] args);
    protected override void OnStop();
    private void ExecuteTimeSynchronization();
}
```

#### DataCollectionService

**Purpose**: Windows service executable that collects and processes attendance data

**Key Responsibilities**:
- Accept command-line parameters (1 = batch 1, 2 = manual mode)
- Initialize database connections
- Manage multi-stage processing (machines 1-4, then 5-6)
- Coordinate data collection and processing workflows
- Chain execution to second batch if needed

**Interface**:
```csharp
public class DataCollectionService : ServiceBase
{
    protected override void OnStart(string[] args);
    protected override void OnStop();
    private void ExecuteDataCollection(string commandParam);
    private void LaunchSecondBatch();
}
```

### 2. Configuration Management

#### MachineConfiguration

**Purpose**: Represents biometric device connection parameters

**Data Model**:
```csharp
public class MachineConfiguration
{
    public int MachineNumber { get; set; }
    public string IPAddress { get; set; }
    public int Port { get; set; }
    public int NetworkPassword { get; set; }
    public string InOutFlag { get; set; } // "I" or "O"
}
```

#### MachineConfigurationProvider

**Purpose**: Provides hardcoded machine configurations

**Key Responsibilities**:
- Return list of 6 machine configurations
- Support batch filtering (machines 1-4 vs 5-6)

**Interface**:
```csharp
public interface IMachineConfigurationProvider
{
    List<MachineConfiguration> GetMachines(int batch); // batch 1 or 2
    List<MachineConfiguration> GetAllMachines();
}
```

#### DatabaseConnectionManager

**Purpose**: Manages database connection strings and connection lifecycle

**Key Responsibilities**:
- Provide Access database connection with password
- Provide SQL Server connection via DSN file
- Handle connection open/close operations
- Implement connection retry logic for known SQL Server errors

**Interface**:
```csharp
public interface IDatabaseConnectionManager
{
    IDbConnection GetAccessConnection();
    IDbConnection GetSqlServerConnection();
    void CloseConnection(IDbConnection connection);
    void ReopenConnection(IDbConnection connection);
}
```

#### SettingsProvider

**Purpose**: Reads and updates system settings from Access database

**Key Responsibilities**:
- Read DeleteAll mode setting
- Update DeleteAll mode after processing
- Handle missing settings gracefully

**Interface**:
```csharp
public interface ISettingsProvider
{
    bool GetDeleteAllMode();
    void SetDeleteAllMode(bool value);
}
```

### 3. Device Communication Layer

#### SdkWrapper

**Purpose**: Wraps the SBXPC ActiveX control with a clean .NET interface

**Key Responsibilities**:
- Initialize COM interop with SBXPC control
- Expose SDK methods with proper parameter marshaling
- Convert COM error codes to .NET exceptions
- Handle COM object lifecycle and cleanup

**Interface**:
```csharp
public interface ISdkWrapper
{
    bool SetIPAddress(string ipAddress, int port, int password);
    bool OpenCommPort(int machineNumber);
    void CloseCommPort();
    bool EnableDevice(int machineNumber, bool enable);
    bool SetDeviceTime(int machineNumber);
    bool ReadGeneralLogData(int machineNumber);
    bool ReadAllGLogData(int machineNumber);
    bool GetGeneralLogData(int machineNumber, out AttendanceLog log);
    bool GetAllGLogData(int machineNumber, out AttendanceLog log);
    int GetLastError();
    void SetReadMark(int value);
}
```

#### DeviceConnectionManager

**Purpose**: Manages connection lifecycle for biometric devices

**Key Responsibilities**:
- Establish TCP/IP connection to device
- Verify connection success
- Handle connection failures gracefully
- Close connections properly

**Interface**:
```csharp
public interface IDeviceConnectionManager
{
    bool Connect(MachineConfiguration config, ISdkWrapper sdk);
    void Disconnect(ISdkWrapper sdk);
}
```

#### LogReader

**Purpose**: Reads attendance logs from connected biometric device

**Key Responsibilities**:
- Set read mark for incremental reading
- Invoke appropriate SDK methods based on DeleteAll mode
- Iterate through log entries until no more data
- Handle SDK errors and log them

**Interface**:
```csharp
public interface ILogReader
{
    IEnumerable<AttendanceLog> ReadLogs(
        ISdkWrapper sdk, 
        MachineConfiguration config, 
        bool deleteAllMode);
}
```

#### TimeSynchronizer

**Purpose**: Synchronizes device time with server time

**Key Responsibilities**:
- Disable device before time update
- Set device time to current server time
- Re-enable device after update
- Log success or failure with error codes

**Interface**:
```csharp
public interface ITimeSynchronizer
{
    bool SynchronizeTime(ISdkWrapper sdk, int machineNumber);
}
```

### 4. Data Processing Layer

#### AttendanceLog

**Purpose**: Represents a single attendance log entry from device

**Data Model**:
```csharp
public class AttendanceLog
{
    public int TMachineNumber { get; set; }
    public int SMachineNumber { get; set; }
    public int SEnrollNumber { get; set; }
    public int VerifyMode { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public int Day { get; set; }
    public int Hour { get; set; }
    public int Minute { get; set; }
    public int Second { get; set; }
    public string InOut { get; set; }
    
    public DateTime GetDateTime() => 
        new DateTime(Year, Month, Day, Hour, Minute, Second);
}
```

#### RawLogProcessor

**Purpose**: Saves raw attendance logs to databases

**Key Responsibilities**:
- Check for duplicate entries before insertion
- Insert to both Access and SQL Server databases
- Filter records based on BackYearBlocked (2023+)
- Log insertion results

**Interface**:
```csharp
public interface IRawLogProcessor
{
    void SaveRawLog(AttendanceLog log, IDbConnection accessConn, IDbConnection sqlConn);
    bool IsDuplicate(AttendanceLog log, IDbConnection connection);
}
```

#### EmployeeMapper

**Purpose**: Maps biometric enrollment numbers to employee IDs

**Key Responsibilities**:
- Query M_Executive table by BioID
- Return EmpID if found
- Return enrollment number if not found or null
- Handle database query errors

**Interface**:
```csharp
public interface IEmployeeMapper
{
    string GetEmployeeId(int enrollNumber, IDbConnection accessConn);
}
```

#### AttendanceRecordProcessor

**Purpose**: Processes raw logs into AttenInfo table

**Key Responsibilities**:
- Read unprocessed raw logs (vtrfFlag = 0 or null)
- Map enrollment numbers to employee IDs
- Check for duplicates in AttenInfo
- Insert new attendance records
- Update vtrfFlag to 1 after successful processing
- Process in chronological order

**Interface**:
```csharp
public interface IAttendanceRecordProcessor
{
    void ProcessRawLogs(IDbConnection accessConn, IDbConnection sqlConn);
}
```

#### AttendanceRecord

**Purpose**: Represents processed attendance record for AttenInfo table

**Data Model**:
```csharp
public class AttendanceRecord
{
    public string EmpCode { get; set; }
    public int TicketNo { get; set; } = 0;
    public DateTime EntryDate { get; set; }
    public string InOutFlag { get; set; }
    public TimeSpan EntryTime { get; set; }
    public int TrfFlag { get; set; } = 0;
    public string UpdateUID { get; set; }
    public string Location { get; set; }
    public string ErrMsg { get; set; }
}
```

### 5. Data Access Layer

#### AccessDatabaseRepository

**Purpose**: Handles all Access database operations

**Key Responsibilities**:
- Execute queries against RCMSBio.mdb
- Manage OLE DB connections
- Handle Jet database password authentication
- Provide CRUD operations for Settings, M_Executive, 0RawLog tables

**Interface**:
```csharp
public interface IAccessDatabaseRepository
{
    DataTable ExecuteQuery(string sql, IDbConnection connection);
    int ExecuteNonQuery(string sql, IDbConnection connection);
    bool RecordExists(string sql, IDbConnection connection);
    void InsertRawLog(AttendanceLog log, IDbConnection connection);
    string GetEmployeeId(int bioId, IDbConnection connection);
}
```

#### SqlServerRepository

**Purpose**: Handles all SQL Server database operations

**Key Responsibilities**:
- Execute queries against SQL Server via DSN
- Manage transaction boundaries
- Provide CRUD operations for 0RawLog and AttenInfo tables
- Handle SQL Server specific connection errors

**Interface**:
```csharp
public interface ISqlServerRepository
{
    DataTable ExecuteQuery(string sql, IDbConnection connection);
    int ExecuteNonQuery(string sql, IDbConnection connection);
    bool RecordExists(string sql, IDbConnection connection);
    void InsertRawLog(AttendanceLog log, IDbConnection connection);
    void InsertAttendanceRecord(AttendanceRecord record, IDbConnection connection);
    void UpdateRawLogTransferFlag(int id, IDbConnection connection);
    IEnumerable<AttendanceLog> GetUnprocessedRawLogs(IDbConnection connection);
}
```

#### TransactionManager

**Purpose**: Manages database transactions for batch operations

**Key Responsibilities**:
- Begin transactions
- Commit successful operations
- Rollback on errors
- Handle nested transaction scenarios

**Interface**:
```csharp
public interface ITransactionManager
{
    IDbTransaction BeginTransaction(IDbConnection connection);
    void CommitTransaction(IDbTransaction transaction);
    void RollbackTransaction(IDbTransaction transaction);
}
```

### 6. Logging and Error Handling

#### FileLogger

**Purpose**: Writes operational logs to timestamped text files

**Key Responsibilities**:
- Create log files with timestamp naming convention
- Write timestamped log entries
- Support per-machine log files
- Handle log file rotation
- Close files properly on shutdown

**Interface**:
```csharp
public interface IFileLogger
{
    void Initialize(string logDirectory, string prefix);
    void Log(string message);
    void LogError(string message, Exception ex);
    void Close();
}
```

#### EventLogWriter

**Purpose**: Writes critical errors to Windows Event Log

**Key Responsibilities**:
- Write to Application event log
- Use appropriate severity levels
- Include error codes and descriptions

**Interface**:
```csharp
public interface IEventLogWriter
{
    void WriteError(string message, int eventId);
    void WriteWarning(string message, int eventId);
    void WriteInformation(string message, int eventId);
}
```

## Data Models

### Database Schema

#### 0RawLog Table (Access & SQL Server)

```sql
CREATE TABLE [0RawLog] (
    [ID] INT IDENTITY(1,1) PRIMARY KEY,
    [vTMachineNumber] NUMERIC(18,0),
    [vSMachineNumber] NUMERIC(18,0),
    [vSEnrollNumber] NUMERIC(18,0),
    [vVerifyMode] NUMERIC(18,0),
    [vYear] NUMERIC(18,0),
    [vMonth] NUMERIC(18,0),
    [vDay] NUMERIC(18,0),
    [vHour] NUMERIC(18,0),
    [vMinute] NUMERIC(18,0),
    [vSecond] NUMERIC(18,0),
    [vInOut] NVARCHAR(1),
    [vtrfFlag] NVARCHAR(1)
)
```

**Purpose**: Stores raw attendance logs directly from biometric devices

**Key Fields**:
- vTMachineNumber: Logical machine number (1-6)
- vSEnrollNumber: Biometric enrollment number
- vYear/vMonth/vDay/vHour/vMinute/vSecond: Timestamp components
- vInOut: 'I' for IN, 'O' for OUT
- vtrfFlag: '0' or NULL = unprocessed, '1' = processed

#### AttenInfo Table (SQL Server)

```sql
CREATE TABLE [AttenInfo] (
    [Srno] INT IDENTITY(1,1) PRIMARY KEY,
    [EmpCode] NVARCHAR(50),
    [TicketNo] INT,
    [EntryDate] DATETIME,
    [InOutFlag] NVARCHAR(1),
    [EntryTime] DATETIME,
    [TrfFlag] INT,
    [UpdateUID] NVARCHAR(50),
    [Location] NVARCHAR(50),
    [ErrMsg] NVARCHAR(255)
)
```

**Purpose**: Stores processed attendance records for HR software consumption

**Key Fields**:
- EmpCode: Employee ID from M_Executive table
- EntryDate: Date portion of attendance
- EntryTime: Time portion of attendance
- InOutFlag: 'I' for IN, 'O' for OUT
- TrfFlag: Always 0 (reserved for HR software)

#### M_Executive Table (Access)

```sql
CREATE TABLE [M_Executive] (
    [EmpID] NVARCHAR(50),
    [BioID] INT,
    -- other employee fields
)
```

**Purpose**: Maps biometric enrollment numbers to employee IDs

**Key Fields**:
- EmpID: Company employee identifier
- BioID: Biometric device enrollment number

#### Settings Table (Access)

```sql
CREATE TABLE [Settings] (
    [SettingName] NVARCHAR(50) PRIMARY KEY,
    [SettingValue] NVARCHAR(255)
)
```

**Purpose**: Stores system configuration settings

**Key Settings**:
- DeleteAll: '1' = read all logs, '0' = read new logs only

## Error Handling

### Error Categories and Handling Strategies

#### 1. Device Connection Errors

**Scenarios**:
- Device unreachable (network issue)
- Invalid IP/port configuration
- Device powered off

**Handling**:
- Log error with device details
- Continue processing remaining devices
- Write to event log if all devices fail
- Do not crash service

#### 2. SDK Errors

**Scenarios**:
- COM interop failures
- SDK method returns false
- Invalid parameters

**Handling**:
- Call GetLastError() to retrieve error code
- Map error code to descriptive message
- Log error with context
- Continue processing if possible

**Error Code Mapping**:
```
0 = SUCCESS
1 = ERR_COMPORT_ERROR
2 = ERR_WRITE_FAIL
3 = ERR_READ_FAIL
4 = ERR_INVALID_PARAM
5 = ERR_NON_CARRYOUT
6 = ERR_LOG_END (normal end of data)
7 = ERR_MEMORY
8 = ERR_MULTIUSER
```

#### 3. Database Connection Errors

**Scenarios**:
- SQL Server connection forcibly closed
- Socket operation error
- Access database locked
- Authentication failure

**Handling**:
- Detect specific error messages
- Close and reopen connection for known transient errors
- Retry operation once after reconnection
- Log error and abort if retry fails
- Use transactions to prevent partial data

**Specific Error Handling**:
```csharp
try {
    // database operation
}
catch (Exception ex) {
    if (ex.Message.Contains("forcibly closed by the remote host") ||
        ex.Message.Contains("not a socket")) {
        connection.Close();
        connection.Open();
        // retry operation
    }
    else {
        throw;
    }
}
```

#### 4. Data Validation Errors

**Scenarios**:
- Invalid date/time components from device
- Missing employee mapping
- Duplicate detection

**Handling**:
- Log validation failure with data details
- Skip invalid record
- Continue processing remaining records
- Do not insert invalid data

#### 5. File System Errors

**Scenarios**:
- Cannot create log file
- Disk full
- Permission denied

**Handling**:
- Attempt to write to event log instead
- Continue service execution
- Alert administrator via event log

### Global Error Handling

**Service Level**:
```csharp
try {
    // main service logic
}
catch (Exception ex) {
    fileLogger.LogError("CRASHED", ex);
    eventLogWriter.WriteError($"CRASHED: {ex.Message}", 1000);
    // Do not rethrow - allow service to exit gracefully
}
```

## Testing Strategy

### Unit Testing

**Scope**: Test individual components in isolation

**Key Test Areas**:
1. **Configuration Providers**
   - Verify machine configurations returned correctly
   - Test batch filtering (1-4 vs 5-6)

2. **Employee Mapper**
   - Test successful mapping
   - Test missing employee
   - Test null/empty EmpID handling

3. **Raw Log Processor**
   - Test duplicate detection logic
   - Test year filtering (BackYearBlocked)
   - Test SQL generation

4. **Attendance Record Processor**
   - Test date/time formatting
   - Test duplicate detection
   - Test transfer flag updates

5. **Data Models**
   - Test AttendanceLog.GetDateTime()
   - Test data validation

**Mocking Strategy**:
- Mock ISdkWrapper for device communication tests
- Mock IDbConnection for database tests
- Mock IFileLogger for logging tests

### Integration Testing

**Scope**: Test component interactions with real dependencies

**Key Test Scenarios**:
1. **Database Integration**
   - Test Access database connectivity
   - Test SQL Server connectivity via DSN
   - Test transaction commit/rollback
   - Test connection retry logic

2. **SDK Integration**
   - Test COM interop with SBXPC control
   - Test device connection workflow
   - Test log reading with test device
   - Test time synchronization

3. **End-to-End Workflows**
   - Test complete data collection cycle
   - Test complete time sync cycle
   - Test multi-stage processing (batch 1 then batch 2)
   - Test DeleteAll mode toggle

### System Testing

**Scope**: Test complete system in production-like environment

**Test Scenarios**:
1. **Windows Service Installation**
   - Install both services
   - Start/stop services
   - Verify service account permissions
   - Test Windows Scheduler integration

2. **Multi-Device Processing**
   - Connect to all 6 devices
   - Verify data from each device
   - Test parallel device processing
   - Test device failure scenarios

3. **Data Integrity**
   - Verify no data loss
   - Verify no duplicate records
   - Verify Access and SQL Server consistency
   - Verify employee mapping accuracy

4. **Performance Testing**
   - Test with large log volumes (1000+ records)
   - Measure processing time per device
   - Test memory usage over time
   - Test concurrent database access

5. **Error Recovery**
   - Test device offline scenarios
   - Test database unavailable scenarios
   - Test partial failure recovery
   - Verify logging completeness

### Acceptance Testing

**Scope**: Validate system meets business requirements

**Test Cases**:
1. Verify AttenInfo table format matches HR software expectations
2. Verify table structures unchanged from VB6 version
3. Verify scheduled execution via Windows Scheduler
4. Verify log files created with correct format
5. Verify time synchronization accuracy across devices
6. Verify IN/OUT designation per device
7. Verify employee ID mapping correctness

## Deployment Considerations

### Prerequisites

1. **.NET Framework 4.7.2 or higher** installed on Windows Server
2. **SBXPC ActiveX control** registered on server
3. **Microsoft Access Database Engine** (for OLE DB provider)
4. **SQL Server ODBC Driver 13** or higher
5. **DSN configuration** (ReadLogsHRMS.dsn) with SQL Server credentials
6. **Network connectivity** to biometric devices (192.168.2.224-229)
7. **Windows service account** with appropriate permissions

### Installation Steps

1. Register SBXPC ActiveX control: `regsvr32 SBXPC.ocx`
2. Create DSN file with SQL Server connection details
3. Deploy Access database (RCMSBio.mdb) to application directory
4. Install Time Sync Service: `sc create TimeSyncService binPath="path\to\TimeSyncService.exe"`
5. Install Data Collection Service: `sc create DataCollectionService binPath="path\to\DataCollectionService.exe"`
6. Configure Windows Scheduler tasks to trigger services
7. Grant service account permissions to databases and log directory

### Configuration Files

**App.config** (both services):
```xml
<configuration>
  <appSettings>
    <add key="AccessDbPath" value="RCMSBio.mdb"/>
    <add key="AccessDbPassword" value="szus"/>
    <add key="SqlDsnFile" value="ReadLogsHRMS.dsn"/>
    <add key="LogDirectory" value="Logs"/>
    <add key="BackYearBlocked" value="2023"/>
  </appSettings>
</configuration>
```

**ReadLogsHRMS.dsn**:
```
[ODBC]
DRIVER=ODBC Driver 13 for SQL Server
SERVER=your_server\instance
DATABASE=anandDB
UID=rely
PWD=your_password
```

### Windows Scheduler Configuration

**Time Sync Task**:
- Trigger: Daily at 2:00 AM
- Action: Start program `TimeSyncService.exe`
- Run whether user is logged on or not
- Run with highest privileges

**Data Collection Task**:
- Trigger: Every 15 minutes during business hours
- Action: Start program `DataCollectionService.exe` with argument `1`
- Run whether user is logged on or not
- Run with highest privileges

### Security Considerations

1. **Service Account Permissions**:
   - Read/write access to log directory
   - Read access to Access database
   - Read/write access to SQL Server database
   - Network access to biometric devices

2. **Database Security**:
   - Use SQL authentication with strong password
   - Encrypt DSN file or use Windows authentication
   - Restrict Access database file permissions

3. **Network Security**:
   - Ensure biometric devices on isolated VLAN
   - Use firewall rules to restrict access to device IPs
   - Monitor for unauthorized access attempts

### Monitoring and Maintenance

1. **Log Monitoring**:
   - Review log files daily for errors
   - Set up alerts for event log errors
   - Archive old log files monthly

2. **Database Maintenance**:
   - Monitor 0RawLog table growth
   - Archive old raw logs periodically
   - Verify AttenInfo data quality

3. **Device Health**:
   - Monitor time sync success rates
   - Track device connection failures
   - Verify device firmware versions

4. **Performance Monitoring**:
   - Track service execution duration
   - Monitor database query performance
   - Watch for memory leaks

This design provides a robust, maintainable replacement for the VB6 system while preserving compatibility with existing database structures and HR software integration.
