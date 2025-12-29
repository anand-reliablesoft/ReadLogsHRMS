# Data Collection Service Deployment Guide

## Overview

The Data Collection Service is a Windows service that collects attendance data from biometric devices and stores it in both Access and SQL Server databases. The service runs on-demand via Windows Task Scheduler, executing every 15 minutes during business hours.

## Architecture

The service operates in two stages:
1. **Batch 1**: Processes machines 1-4, collects raw data
2. **Batch 2**: Processes machines 5-6, then performs batch processing to transfer raw logs to AttenInfo table

## Prerequisites

### Software Requirements

1. **Windows Server 2012 R2 or higher**
2. **.NET Framework 4.7.2 or higher**
   - Download: https://dotnet.microsoft.com/download/dotnet-framework/net472
3. **ODBC Driver 13 for SQL Server** (or higher)
   - Download: https://www.microsoft.com/en-us/download/details.aspx?id=53339
   - Recommended: ODBC Driver 17 or 18 for better performance
4. **Microsoft Access Database Engine** (for OLE DB provider)
   - Download: https://www.microsoft.com/en-us/download/details.aspx?id=54920
   - Required for connecting to .mdb files
5. **SBXPC ActiveX Control** (biometric device SDK)
   - Provided by device manufacturer
   - Must be registered on the server

### Network Requirements

- Network connectivity to biometric devices (192.168.2.224-229)
- Firewall rules allowing TCP connections to device ports (4370)
- SQL Server accessible via network (if remote)

### Database Requirements

**SQL Server Database (anandDB):**
- `0RawLog` table - stores raw attendance logs
- `AttenInfo` table - stores processed attendance records
- `M_Executive` table - employee master data (optional, can sync from Access)

**Access Database (RCMSBio.mdb):**
- `M_Executive` table - employee master data with BioID mapping
- `Settings` table - system configuration (DeleteAll mode)
- `0RawLog` table - local copy of raw logs

### Permissions

Service account must have:
- Read/write access to log directory
- Read access to Access database file
- Read/write access to SQL Server database
- Network access to biometric devices (TCP/IP)

## Installation Steps

### Step 1: Build the Solution

1. Open `BiometricAttendanceSystem.sln` in Visual Studio
2. Select **Release** configuration
3. Set platform to **x86** (required for COM interop with SBXPC)
4. Build Solution (Ctrl+Shift+B)
5. Verify build output in `DataCollectionService\bin\Release\`

### Step 2: Prepare Deployment Directory

Create deployment directory (e.g., `C:\BiometricServices\DataCollection\`) and copy:

**Required Files:**
- `DataCollectionService.exe`
- `BiometricAttendance.Common.dll`
- `DataCollectionService.exe.config`
- `SBXPCDLL.dll`
- `SBPCCOMM.dll`
- `RCMSBio.mdb` (Access database)

**Configuration Files:**
- `ReadLogsHRMS.dsn.template` (rename to .dsn and configure)
- `DSN-SETUP.md` (reference documentation)

**Installation Scripts:**
- `Install-DataCollectionService.ps1`
- `Uninstall-DataCollectionService.ps1`
- `Install-Using-InstallUtil.ps1` (alternative)

### Step 3: Register SBXPC ActiveX Control

Open Command Prompt as Administrator:

```cmd
cd C:\BiometricServices\DataCollection
regsvr32 SBXPCDLL.dll
regsvr32 SBPCCOMM.dll
```

Verify registration:
```cmd
regsvr32 /s SBXPCDLL.dll
```

If successful, no error message appears.

### Step 4: Configure Database Connection

#### Configure SQL Server DSN

1. Copy `ReadLogsHRMS.dsn.template` to `ReadLogsHRMS.dsn`
2. Edit `ReadLogsHRMS.dsn` with your SQL Server details:

```ini
[ODBC]
DRIVER=ODBC Driver 13 for SQL Server
SERVER=192.168.1.100\SQLEXPRESS
DATABASE=anandDB
UID=rely
PWD=YourSecurePassword
```

3. Test connection (see DSN-SETUP.md for testing instructions)

#### Verify Access Database

1. Ensure `RCMSBio.mdb` is in the deployment directory
2. Verify password matches App.config (default: `szus`)
3. Check that M_Executive table has employee data with BioID field

### Step 5: Configure App.config

Edit `DataCollectionService.exe.config` if needed:

```xml
<appSettings>
  <add key="AccessDbPath" value="RCMSBio.mdb"/>
  <add key="AccessDbPassword" value="szus"/>
  <add key="SqlDsnFile" value="ReadLogsHRMS.dsn"/>
  <add key="LogDirectory" value="Logs"/>
  <add key="BackYearBlocked" value="2023"/>
</appSettings>
```

**Configuration Options:**
- `AccessDbPath`: Relative or absolute path to Access database
- `AccessDbPassword`: Access database password
- `SqlDsnFile`: Path to DSN file (relative or absolute)
- `LogDirectory`: Directory for log files (created automatically)
- `BackYearBlocked`: Minimum year for attendance records (filters old data)

### Step 6: Install Service and Scheduled Task

Run PowerShell as Administrator:

```powershell
cd C:\BiometricServices\DataCollection
.\Install-DataCollectionService.ps1
```

The script will:
1. Install the Windows service
2. Create log directory
3. Create scheduled task (every 15 minutes, 6 AM - 8 PM, weekdays)
4. Configure service recovery options
5. Verify configuration files

**Alternative Installation (InstallUtil):**

```powershell
.\Install-Using-InstallUtil.ps1
```

**Manual Installation:**

```cmd
sc create DataCollectionService binPath="C:\BiometricServices\DataCollection\DataCollectionService.exe" start=demand DisplayName="Biometric Data Collection Service"
sc description DataCollectionService "Collects attendance data from biometric devices"
```

### Step 7: Verify Installation

1. **Check Service:**
   ```powershell
   Get-Service DataCollectionService
   ```

2. **Check Scheduled Task:**
   ```powershell
   Get-ScheduledTask -TaskName "BiometricDataCollection"
   ```

3. **Test Manual Execution:**
   ```cmd
   cd C:\BiometricServices\DataCollection
   DataCollectionService.exe 1
   ```

4. **Check Logs:**
   - Look for log files in `Logs\` directory
   - Format: `LogYYYYMMDDHHMMSS.txt`
   - Check for errors or connection issues

## Configuration

### Machine Configuration

The service is hardcoded with 6 biometric devices:

| Machine | IP Address      | Port | Password | IN/OUT |
|---------|-----------------|------|----------|--------|
| 1       | 192.168.2.224   | 4370 | 0        | IN     |
| 2       | 192.168.2.225   | 4370 | 0        | OUT    |
| 3       | 192.168.2.226   | 4370 | 0        | IN     |
| 4       | 192.168.2.227   | 4370 | 0        | OUT    |
| 5       | 192.168.2.228   | 4370 | 0        | IN     |
| 6       | 192.168.2.229   | 4370 | 0        | OUT    |

To modify, edit `MachineConfigurationProvider.cs` and rebuild.

### Delete All Mode

The `DeleteAll` setting in the Access database Settings table controls log reading:
- `1` = Read all logs from devices (full refresh)
- `0` = Read only new logs (incremental)

After successful processing, the service automatically sets DeleteAll to `0`.

To force a full refresh:
```sql
UPDATE Settings SET SettingValue = '1' WHERE SettingName = 'DeleteAll'
```

### Scheduled Task Configuration

Default schedule: Every 15 minutes from 6:00 AM to 8:00 PM on weekdays

To modify:
1. Open Task Scheduler
2. Find "BiometricDataCollection" task
3. Edit triggers and schedule as needed

**Recommended Settings:**
- Run whether user is logged on or not
- Run with highest privileges
- Start when available
- Run only if network is available
- Do not start new instance if already running

## Operation

### Normal Operation Flow

1. **Scheduled Task Triggers** (every 15 minutes)
2. **Service Starts** with parameter "1" (batch 1)
3. **Batch 1 Processing:**
   - Reads DeleteAll mode from Settings table
   - Connects to machines 1-4 sequentially
   - Reads attendance logs from each device
   - Saves raw logs to both Access and SQL Server
   - Closes connections
4. **Batch 2 Launch:**
   - If part2 flag is false, launches second executable for machines 5-6
   - If part2 flag is true, processes raw logs into AttenInfo table
5. **Batch Processing:**
   - Reads unprocessed raw logs (vtrfFlag = 0 or null)
   - Maps enrollment numbers to employee IDs
   - Inserts records into AttenInfo table
   - Updates vtrfFlag to '1' for processed records
   - Sets DeleteAll mode to '0'
6. **Service Exits**

### Log Files

**Main Log File:**
- Format: `LogYYYYMMDDHHMMSS.txt`
- Contains overall execution flow and errors

**Per-Machine Log Files:**
- Format: `Log_Machine1YYYYMMDDHHMMSS.txt`
- Contains device-specific operations and data

**Log Content:**
- Timestamps for all operations
- Connection status for each device
- Number of records read/processed
- Errors with error codes and descriptions
- Duplicate detection messages

### Manual Execution

**Test Batch 1 (machines 1-4):**
```cmd
DataCollectionService.exe 1
```

**Manual Mode (configuration form):**
```cmd
DataCollectionService.exe 2
```

**Run as Console (for debugging):**
- Build in Debug mode
- Run from Visual Studio or command line
- Logs output to console and files

## Testing

### Pre-Deployment Testing

1. **Test Database Connections:**
   ```powershell
   # Test SQL Server
   $conn = New-Object System.Data.Odbc.OdbcConnection("DSN=ReadLogsHRMS")
   $conn.Open()
   $conn.Close()
   
   # Test Access
   $conn = New-Object System.Data.OleDb.OleDbConnection("Provider=Microsoft.Jet.OLEDB.4.0;Data Source=RCMSBio.mdb;Jet OLEDB:Database Password=szus")
   $conn.Open()
   $conn.Close()
   ```

2. **Test Device Connectivity:**
   ```powershell
   Test-NetConnection -ComputerName 192.168.2.224 -Port 4370
   ```

3. **Verify Table Structures:**
   ```sql
   -- SQL Server
   SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '0RawLog'
   SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AttenInfo'
   ```

### Post-Deployment Testing

1. **Manual Execution Test:**
   - Run `DataCollectionService.exe 1`
   - Check for errors in console output
   - Verify log files created
   - Check data in 0RawLog table

2. **Database Verification:**
   ```sql
   -- Check raw logs
   SELECT TOP 10 * FROM [0RawLog] ORDER BY ID DESC
   
   -- Check processed records
   SELECT TOP 10 * FROM [AttenInfo] ORDER BY Srno DESC
   
   -- Check transfer flags
   SELECT COUNT(*) as Unprocessed FROM [0RawLog] WHERE vtrfFlag IS NULL OR vtrfFlag = '0'
   ```

3. **Scheduled Task Test:**
   - Right-click task in Task Scheduler
   - Select "Run"
   - Monitor execution in Task Scheduler history
   - Check logs for successful execution

4. **End-to-End Test:**
   - Set DeleteAll to '1' in Settings table
   - Run service manually
   - Verify data flows from devices → 0RawLog → AttenInfo
   - Verify employee ID mapping
   - Verify IN/OUT flags correct per device
   - Verify DeleteAll reset to '0'

## Troubleshooting

### Service Installation Issues

**Error: "Access is denied"**
- Run PowerShell as Administrator
- Check User Account Control (UAC) settings

**Error: "Service already exists"**
- Uninstall first: `.\Uninstall-DataCollectionService.ps1`
- Or reinstall when prompted

**Error: "InstallUtil not found"**
- Verify .NET Framework 4.7.2 installed
- Check path: `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\`

### Database Connection Issues

**Error: "Login failed for user"**
- Verify SQL Server credentials in DSN file
- Check SQL Server allows SQL Authentication
- Verify user has database permissions

**Error: "Cannot open database"**
- Verify database name correct (anandDB)
- Check SQL Server is running
- Test connection with SQL Server Management Studio

**Error: "Could not find installable ISAM"**
- Install Microsoft Access Database Engine
- Verify Access database path in App.config
- Check file permissions

**Error: "Connection forcibly closed"**
- Service handles this automatically with retry
- Check network stability
- Verify SQL Server max connections not exceeded

### Device Communication Issues

**Error: "Connection failed to device"**
- Verify device IP address and port
- Test network connectivity: `ping 192.168.2.224`
- Check device is powered on and network cable connected
- Verify firewall not blocking connections

**Error: "COM object not registered"**
- Register SBXPC: `regsvr32 SBXPCDLL.dll`
- Verify DLL files present in service directory
- Check platform target is x86 (not x64)

**Error: "SDK error code 1"**
- COM port error - device not responding
- Check device network settings
- Verify device firmware version

### Data Processing Issues

**No data in AttenInfo table**
- Check vtrfFlag in 0RawLog (should be '1' after processing)
- Verify batch processing executed (part2 flag true)
- Check for errors in log files
- Verify M_Executive table has employee data

**Duplicate records**
- Service automatically detects and skips duplicates
- Check log files for "duplicate" messages
- Normal behavior, not an error

**Missing employee IDs**
- Check M_Executive table has BioID values
- Service falls back to enrollment number if not found
- Verify employee data synced from HR system

**Old records not filtered**
- Check BackYearBlocked setting in App.config
- Default is 2023 (only records from 2023 onwards)
- Modify if different year range needed

## Maintenance

### Regular Maintenance Tasks

**Daily:**
- Review log files for errors
- Check Windows Event Log for service errors
- Verify scheduled task running successfully

**Weekly:**
- Verify data in AttenInfo table
- Check for unprocessed records in 0RawLog
- Monitor database growth

**Monthly:**
- Archive old log files
- Archive old raw logs (0RawLog)
- Review device connection success rates
- Update employee master data if needed

### Database Maintenance

**Archive Old Raw Logs:**
```sql
-- Archive records older than 90 days
INSERT INTO [0RawLog_Archive]
SELECT * FROM [0RawLog]
WHERE DATEFROMPARTS(vYear, vMonth, vDay) < DATEADD(day, -90, GETDATE())

DELETE FROM [0RawLog]
WHERE DATEFROMPARTS(vYear, vMonth, vDay) < DATEADD(day, -90, GETDATE())
```

## Uninstallation

Run PowerShell as Administrator:

```powershell
cd C:\BiometricServices\DataCollection
.\Uninstall-DataCollectionService.ps1
```

**Remove logs as well:**
```powershell
.\Uninstall-DataCollectionService.ps1 -RemoveLogs
```

## Security Considerations

- Use dedicated service account (not LocalSystem for production)
- Grant minimum required permissions
- Use strong passwords for SQL Server
- Consider Windows Authentication instead of SQL Authentication
- Encrypt DSN file or use secure credential storage
- Restrict Access database file permissions

## Support

For technical support:
- Review log files in Logs directory
- Check Windows Event Log (Application)
- Refer to design.md and requirements.md
- Contact system administrator

## File Locations

- Service Executable: `DataCollectionService.exe`
- Configuration: `DataCollectionService.exe.config`
- DSN File: `ReadLogsHRMS.dsn`
- Access Database: `RCMSBio.mdb`
- Log Files: `Logs\`

## Service Details

- Service Name: `DataCollectionService`
- Display Name: `Biometric Data Collection Service`
- Startup Type: Manual (triggered by Task Scheduler)
- Recovery: Restart on failure (3 attempts)

## Scheduled Task Details

- Task Name: `BiometricDataCollection`
- Trigger: Every 15 minutes, 6:00 AM - 8:00 PM, weekdays
- Action: Run `DataCollectionService.exe` with argument `1`
- Account: SYSTEM (or configured service account)
