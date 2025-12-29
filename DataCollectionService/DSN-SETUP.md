# DSN Configuration Instructions

## Overview

The Data Collection Service requires an ODBC DSN file to connect to the SQL Server database. This file contains the connection parameters for the remote database.

## Prerequisites

1. **ODBC Driver 13 for SQL Server** (or higher) must be installed on the server
   - Download from: https://www.microsoft.com/en-us/download/details.aspx?id=53339
   - Or use a newer version (Driver 17 or 18)

## Setup Instructions

### Step 1: Copy and Rename Template

1. Copy `ReadLogsHRMS.dsn.template` to `ReadLogsHRMS.dsn`
2. Place the file in the same directory as the DataCollectionService executable

### Step 2: Configure Connection Parameters

Edit `ReadLogsHRMS.dsn` and replace the placeholders:

```
[ODBC]
DRIVER=ODBC Driver 13 for SQL Server
SERVER=<YOUR_SERVER>\<INSTANCE>
DATABASE=anandDB
UID=rely
PWD=<YOUR_PASSWORD>
```

**Replace the following:**

- `<YOUR_SERVER>` - Your SQL Server hostname or IP address
- `<INSTANCE>` - Your SQL Server instance name (e.g., SQLEXPRESS)
  - If using default instance, just use the server name without `\<INSTANCE>`
- `<YOUR_PASSWORD>` - The password for the SQL Server user 'rely'

**Example:**

```
[ODBC]
DRIVER=ODBC Driver 13 for SQL Server
SERVER=192.168.1.100\SQLEXPRESS
DATABASE=anandDB
UID=rely
PWD=MySecurePassword123
```

### Step 3: Verify Database Exists

Ensure the following database objects exist in the SQL Server database:

**Required Tables:**
- `0RawLog` - Stores raw attendance logs from devices
- `AttenInfo` - Stores processed attendance records
- `M_Executive` - Employee master data (synced from Access)

**Table Structures:**

```sql
-- 0RawLog Table
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

-- AttenInfo Table
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

### Step 4: Test Connection

You can test the DSN connection using PowerShell:

```powershell
$connectionString = "DSN=ReadLogsHRMS"
$connection = New-Object System.Data.Odbc.OdbcConnection($connectionString)
try {
    $connection.Open()
    Write-Host "Connection successful!" -ForegroundColor Green
    $connection.Close()
} catch {
    Write-Host "Connection failed: $_" -ForegroundColor Red
}
```

## Security Considerations

1. **File Permissions**: Restrict access to the DSN file since it contains database credentials
   - Right-click file → Properties → Security
   - Grant read access only to the service account

2. **Password Security**: Consider using Windows Authentication instead of SQL Authentication
   - Change `UID` and `PWD` to `Trusted_Connection=Yes`
   - Ensure service account has SQL Server permissions

3. **Encryption**: For production environments, consider encrypting the DSN file or storing credentials in a secure vault

## Troubleshooting

### Error: "Data source name not found"
- Verify the DSN file is in the correct directory
- Check the file name matches exactly: `ReadLogsHRMS.dsn`
- Ensure App.config has correct SqlDsnFile setting

### Error: "Login failed for user"
- Verify username and password are correct
- Check SQL Server allows SQL Authentication (not just Windows Authentication)
- Verify user 'rely' has permissions on the anandDB database

### Error: "Driver not found"
- Install ODBC Driver 13 for SQL Server (or higher)
- Update DRIVER line in DSN file to match installed driver version
- Check available drivers: `Get-OdbcDriver` in PowerShell

### Error: "Cannot open database"
- Verify database name is correct (anandDB)
- Check user has access to the database
- Ensure SQL Server is running and accessible

## Alternative: Using Windows Authentication

For better security, configure Windows Authentication:

1. Edit DSN file:
```
[ODBC]
DRIVER=ODBC Driver 13 for SQL Server
SERVER=<YOUR_SERVER>\<INSTANCE>
DATABASE=anandDB
Trusted_Connection=Yes
```

2. Grant SQL Server permissions to the service account:
```sql
USE [anandDB]
CREATE LOGIN [DOMAIN\ServiceAccount] FROM WINDOWS
CREATE USER [DOMAIN\ServiceAccount] FOR LOGIN [DOMAIN\ServiceAccount]
ALTER ROLE [db_datareader] ADD MEMBER [DOMAIN\ServiceAccount]
ALTER ROLE [db_datawriter] ADD MEMBER [DOMAIN\ServiceAccount]
```

## Support

For additional help, refer to:
- Microsoft ODBC Driver documentation
- SQL Server connection string reference
- DataCollectionService DEPLOYMENT.md
