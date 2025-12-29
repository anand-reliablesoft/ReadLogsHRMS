# Data Collection Service - Complete Testing Guide

## Step-by-Step Testing Instructions

### Phase 1: Prepare the Test Environment

#### 1.1 Copy Required Files to Release Directory

```powershell
# Navigate to the Release directory
cd DataCollectionService\bin\Release

# Create a list of what you need
Write-Host "Required files checklist:" -ForegroundColor Cyan
Write-Host "✓ DataCollectionService.exe (already present)"
Write-Host "✓ BiometricAttendance.Common.dll (already present)"
Write-Host "✓ SBXPCDLL.dll (already present)"
Write-Host "✓ SBPCCOMM.dll (already present)"
Write-Host "✓ DataCollectionService.exe.config (already present)"
Write-Host ""
Write-Host "Files you need to add:" -ForegroundColor Yellow
Write-Host "  1. RCMSBio.mdb (Access database)"
Write-Host "  2. ReadLogsHRMS.dsn (SQL Server connection file)"
```

#### 1.2 Place the Access Database

**Copy your RCMSBio.mdb file to:**
```
DataCollectionService\bin\Release\RCMSBio.mdb
```

**Verify it's there:**
```powershell
Test-Path .\RCMSBio.mdb
# Should return: True
```

#### 1.3 Place the DSN File

**Copy your existing ReadLogsHRMS.dsn file to:**
```
DataCollectionService\bin\Release\ReadLogsHRMS.dsn
```

**Or create it from template:**
```powershell
# Copy the template
Copy-Item ..\..\..\ReadLogsHRMS.dsn.template .\ReadLogsHRMS.dsn

# Edit with your SQL Server details
notepad .\ReadLogsHRMS.dsn
```

**Your DSN file should look like:**
```ini
[ODBC]
DRIVER=ODBC Driver 13 for SQL Server
SERVER=your_server_ip\SQLEXPRESS
DATABASE=anandDB
UID=rely
PWD=your_password
```

**Verify it's there:**
```powershell
Test-Path .\ReadLogsHRMS.dsn
# Should return: True

# View the content (be careful - contains password)
Get-Content .\ReadLogsHRMS.dsn
```

#### 1.4 Verify All Files Present

```powershell
# Run this checklist
$files = @(
    "DataCollectionService.exe",
    "BiometricAttendance.Common.dll",
    "SBXPCDLL.dll",
    "SBPCCOMM.dll",
    "DataCollectionService.exe.config",
    "RCMSBio.mdb",
    "ReadLogsHRMS.dsn"
)

Write-Host "`nFile Verification:" -ForegroundColor Cyan
foreach ($file in $files) {
    if (Test-Path $file) {
        Write-Host "  ✓ $file" -ForegroundColor Green
    } else {
        Write-Host "  ✗ $file (MISSING)" -ForegroundColor Red
    }
}
```

---

## Phase 2: Test Database Connections

### 2.1 Test SQL Server Connection

```powershell
Write-Host "`nTesting SQL Server Connection..." -ForegroundColor Cyan

$dsnPath = ".\ReadLogsHRMS.dsn"
$connectionString = "FILEDSN=$dsnPath"

try {
    $conn = New-Object System.Data.Odbc.OdbcConnection($connectionString)
    $conn.Open()
    Write-Host "✓ SQL Server connection: SUCCESS" -ForegroundColor Green
    
    # Test if tables exist
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '0RawLog'"
    $result = $cmd.ExecuteScalar()
    
    if ($result -eq 1) {
        Write-Host "✓ Table [0RawLog] exists" -ForegroundColor Green
    } else {
        Write-Host "✗ Table [0RawLog] NOT FOUND" -ForegroundColor Red
    }
    
    $cmd.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AttenInfo'"
    $result = $cmd.ExecuteScalar()
    
    if ($result -eq 1) {
        Write-Host "✓ Table [AttenInfo] exists" -ForegroundColor Green
    } else {
        Write-Host "✗ Table [AttenInfo] NOT FOUND" -ForegroundColor Red
    }
    
    $conn.Close()
} catch {
    Write-Host "✗ SQL Server connection: FAILED" -ForegroundColor Red
    Write-Host "  Error: $_" -ForegroundColor Red
    Write-Host "`nTroubleshooting:" -ForegroundColor Yellow
    Write-Host "  1. Check DSN file has correct server/database/credentials"
    Write-Host "  2. Verify SQL Server is running"
    Write-Host "  3. Test connection with SQL Server Management Studio"
    Write-Host "  4. Check firewall allows SQL Server connections"
}
```

### 2.2 Test Access Database Connection

```powershell
Write-Host "`nTesting Access Database Connection..." -ForegroundColor Cyan

$accessPath = ".\RCMSBio.mdb"
$accessPassword = "szus"  # Default password from App.config

try {
    $connectionString = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=$accessPath;Jet OLEDB:Database Password=$accessPassword"
    $conn = New-Object System.Data.OleDb.OleDbConnection($connectionString)
    $conn.Open()
    Write-Host "✓ Access database connection: SUCCESS" -ForegroundColor Green
    
    # Test if tables exist
    $tables = $conn.GetSchema("Tables")
    $tableNames = $tables | Where-Object { $_.TABLE_TYPE -eq "TABLE" } | Select-Object -ExpandProperty TABLE_NAME
    
    if ($tableNames -contains "M_Executive") {
        Write-Host "✓ Table [M_Executive] exists" -ForegroundColor Green
    } else {
        Write-Host "✗ Table [M_Executive] NOT FOUND" -ForegroundColor Red
    }
    
    if ($tableNames -contains "Settings") {
        Write-Host "✓ Table [Settings] exists" -ForegroundColor Green
    } else {
        Write-Host "✗ Table [Settings] NOT FOUND" -ForegroundColor Red
    }
    
    if ($tableNames -contains "0RawLog") {
        Write-Host "✓ Table [0RawLog] exists" -ForegroundColor Green
    } else {
        Write-Host "✗ Table [0RawLog] NOT FOUND" -ForegroundColor Red
    }
    
    $conn.Close()
} catch {
    Write-Host "✗ Access database connection: FAILED" -ForegroundColor Red
    Write-Host "  Error: $_" -ForegroundColor Red
    Write-Host "`nTroubleshooting:" -ForegroundColor Yellow
    Write-Host "  1. Check RCMSBio.mdb file is in the Release directory"
    Write-Host "  2. Verify password is correct (default: szus)"
    Write-Host "  3. Install Microsoft Access Database Engine if not installed"
    Write-Host "  4. Check file is not corrupted"
}
```

### 2.3 Check Database Table Structures

```powershell
Write-Host "`nChecking SQL Server Table Structures..." -ForegroundColor Cyan

try {
    $conn = New-Object System.Data.Odbc.OdbcConnection("FILEDSN=.\ReadLogsHRMS.dsn")
    $conn.Open()
    
    # Check 0RawLog structure
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = @"
SELECT COLUMN_NAME, DATA_TYPE 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = '0RawLog' 
ORDER BY ORDINAL_POSITION
"@
    $reader = $cmd.ExecuteReader()
    
    Write-Host "`n[0RawLog] Table Structure:" -ForegroundColor White
    while ($reader.Read()) {
        Write-Host "  - $($reader['COLUMN_NAME']) ($($reader['DATA_TYPE']))"
    }
    $reader.Close()
    
    # Check AttenInfo structure
    $cmd.CommandText = @"
SELECT COLUMN_NAME, DATA_TYPE 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'AttenInfo' 
ORDER BY ORDINAL_POSITION
"@
    $reader = $cmd.ExecuteReader()
    
    Write-Host "`n[AttenInfo] Table Structure:" -ForegroundColor White
    while ($reader.Read()) {
        Write-Host "  - $($reader['COLUMN_NAME']) ($($reader['DATA_TYPE']))"
    }
    $reader.Close()
    
    $conn.Close()
} catch {
    Write-Host "Could not retrieve table structures: $_" -ForegroundColor Yellow
}
```

---

## Phase 3: Test Service Execution

### 3.1 First Test Run (Console Mode)

```powershell
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Running Data Collection Service (Test)" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "This will attempt to:" -ForegroundColor White
Write-Host "  1. Connect to biometric devices (192.168.2.224-227)"
Write-Host "  2. Read attendance logs"
Write-Host "  3. Save to Access and SQL Server databases"
Write-Host "  4. Process raw logs into attendance records"
Write-Host ""
Write-Host "Press any key to start..." -ForegroundColor Yellow
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

# Run the service
.\DataCollectionService.exe 1

Write-Host "`nService execution completed!" -ForegroundColor Green
```

### 3.2 Check Log Files

```powershell
Write-Host "`nChecking Log Files..." -ForegroundColor Cyan

if (Test-Path ".\Logs") {
    Write-Host "✓ Logs directory created" -ForegroundColor Green
    
    $logFiles = Get-ChildItem ".\Logs" -Filter "*.txt" | Sort-Object LastWriteTime -Descending
    
    if ($logFiles.Count -gt 0) {
        Write-Host "✓ Found $($logFiles.Count) log file(s)" -ForegroundColor Green
        Write-Host "`nMost recent log files:" -ForegroundColor White
        $logFiles | Select-Object -First 5 | ForEach-Object {
            Write-Host "  - $($_.Name) ($([math]::Round($_.Length/1KB, 2)) KB)"
        }
        
        Write-Host "`nViewing most recent log file:" -ForegroundColor Cyan
        Write-Host "----------------------------------------"
        Get-Content $logFiles[0].FullName
        Write-Host "----------------------------------------"
    } else {
        Write-Host "✗ No log files found" -ForegroundColor Red
    }
} else {
    Write-Host "✗ Logs directory not created" -ForegroundColor Red
}
```

### 3.3 Verify Data in Databases

```powershell
Write-Host "`nVerifying Data in SQL Server..." -ForegroundColor Cyan

try {
    $conn = New-Object System.Data.Odbc.OdbcConnection("FILEDSN=.\ReadLogsHRMS.dsn")
    $conn.Open()
    
    # Check 0RawLog
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT COUNT(*) FROM [0RawLog]"
    $count = $cmd.ExecuteScalar()
    Write-Host "  [0RawLog] records: $count" -ForegroundColor $(if ($count -gt 0) { "Green" } else { "Yellow" })
    
    if ($count -gt 0) {
        $cmd.CommandText = "SELECT TOP 5 * FROM [0RawLog] ORDER BY ID DESC"
        $reader = $cmd.ExecuteReader()
        Write-Host "`n  Recent records:" -ForegroundColor White
        while ($reader.Read()) {
            $date = "$($reader['vYear'])-$($reader['vMonth'].ToString().PadLeft(2,'0'))-$($reader['vDay'].ToString().PadLeft(2,'0'))"
            $time = "$($reader['vHour'].ToString().PadLeft(2,'0')):$($reader['vMinute'].ToString().PadLeft(2,'0')):$($reader['vSecond'].ToString().PadLeft(2,'0'))"
            Write-Host "    ID: $($reader['ID']), Enroll: $($reader['vSEnrollNumber']), DateTime: $date $time, InOut: $($reader['vInOut'])"
        }
        $reader.Close()
    }
    
    # Check AttenInfo
    $cmd.CommandText = "SELECT COUNT(*) FROM [AttenInfo]"
    $count = $cmd.ExecuteScalar()
    Write-Host "`n  [AttenInfo] records: $count" -ForegroundColor $(if ($count -gt 0) { "Green" } else { "Yellow" })
    
    if ($count -gt 0) {
        $cmd.CommandText = "SELECT TOP 5 * FROM [AttenInfo] ORDER BY Srno DESC"
        $reader = $cmd.ExecuteReader()
        Write-Host "`n  Recent records:" -ForegroundColor White
        while ($reader.Read()) {
            Write-Host "    Srno: $($reader['Srno']), EmpCode: $($reader['EmpCode']), Date: $($reader['EntryDate']), InOut: $($reader['InOutFlag'])"
        }
        $reader.Close()
    }
    
    $conn.Close()
} catch {
    Write-Host "  Error checking data: $_" -ForegroundColor Red
}
```

---

## Phase 4: Full Deployment Test

### 4.1 Prepare for Installation

```powershell
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Full Deployment Test" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "This will:" -ForegroundColor White
Write-Host "  1. Install the Windows service"
Write-Host "  2. Create a scheduled task"
Write-Host "  3. Configure automatic execution"
Write-Host ""
Write-Host "Prerequisites:" -ForegroundColor Yellow
Write-Host "  - You must run PowerShell as Administrator"
Write-Host "  - Database connections must be working"
Write-Host "  - All files must be in place"
Write-Host ""

# Check if running as admin
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
$isAdmin = $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "✗ NOT running as Administrator" -ForegroundColor Red
    Write-Host "`nPlease:" -ForegroundColor Yellow
    Write-Host "  1. Close this PowerShell window"
    Write-Host "  2. Right-click PowerShell"
    Write-Host "  3. Select 'Run as Administrator'"
    Write-Host "  4. Navigate back to this directory"
    Write-Host "  5. Run this script again"
    exit
} else {
    Write-Host "✓ Running as Administrator" -ForegroundColor Green
}
```

### 4.2 Copy Installation Scripts

```powershell
Write-Host "`nCopying installation scripts..." -ForegroundColor Cyan

# Copy scripts from project root to Release directory
$scripts = @(
    "..\..\..\Install-DataCollectionService.ps1",
    "..\..\..\Uninstall-DataCollectionService.ps1",
    "..\..\..\Install-Using-InstallUtil.ps1"
)

foreach ($script in $scripts) {
    if (Test-Path $script) {
        $fileName = Split-Path $script -Leaf
        Copy-Item $script . -Force
        Write-Host "  ✓ Copied $fileName" -ForegroundColor Green
    } else {
        Write-Host "  ✗ Not found: $script" -ForegroundColor Yellow
    }
}
```

### 4.3 Run Installation

```powershell
Write-Host "`nReady to install!" -ForegroundColor Cyan
Write-Host "Press any key to start installation..." -ForegroundColor Yellow
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

# Run the installation script
.\Install-DataCollectionService.ps1
```

### 4.4 Verify Installation

```powershell
Write-Host "`nVerifying Installation..." -ForegroundColor Cyan

# Check service
$service = Get-Service -Name "DataCollectionService" -ErrorAction SilentlyContinue
if ($service) {
    Write-Host "✓ Service installed: $($service.DisplayName)" -ForegroundColor Green
    Write-Host "  Status: $($service.Status)" -ForegroundColor White
    Write-Host "  Startup Type: $($service.StartType)" -ForegroundColor White
} else {
    Write-Host "✗ Service not found" -ForegroundColor Red
}

# Check scheduled task
$task = Get-ScheduledTask -TaskName "BiometricDataCollection" -ErrorAction SilentlyContinue
if ($task) {
    Write-Host "✓ Scheduled task created: $($task.TaskName)" -ForegroundColor Green
    Write-Host "  State: $($task.State)" -ForegroundColor White
    
    $trigger = $task.Triggers[0]
    Write-Host "  Schedule: Every 15 minutes, 6 AM - 8 PM, weekdays" -ForegroundColor White
} else {
    Write-Host "✗ Scheduled task not found" -ForegroundColor Red
}
```

### 4.5 Test Scheduled Task

```powershell
Write-Host "`nTesting Scheduled Task..." -ForegroundColor Cyan
Write-Host "This will manually trigger the scheduled task" -ForegroundColor White

Start-ScheduledTask -TaskName "BiometricDataCollection"
Write-Host "✓ Task triggered" -ForegroundColor Green

Write-Host "`nWaiting 10 seconds for execution..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

# Check task history
$taskInfo = Get-ScheduledTaskInfo -TaskName "BiometricDataCollection"
Write-Host "`nTask Info:" -ForegroundColor White
Write-Host "  Last Run Time: $($taskInfo.LastRunTime)" -ForegroundColor White
Write-Host "  Last Result: $($taskInfo.LastTaskResult)" -ForegroundColor $(if ($taskInfo.LastTaskResult -eq 0) { "Green" } else { "Red" })
Write-Host "  Next Run Time: $($taskInfo.NextRunTime)" -ForegroundColor White

# Check for new log files
Write-Host "`nChecking for new log files..." -ForegroundColor Cyan
$recentLogs = Get-ChildItem ".\Logs" -Filter "*.txt" | Where-Object { $_.LastWriteTime -gt (Get-Date).AddMinutes(-5) }
if ($recentLogs) {
    Write-Host "✓ Found $($recentLogs.Count) recent log file(s)" -ForegroundColor Green
    $recentLogs | ForEach-Object {
        Write-Host "  - $($_.Name)" -ForegroundColor White
    }
} else {
    Write-Host "✗ No recent log files found" -ForegroundColor Yellow
}
```

---

## Phase 5: End-to-End Testing

### 5.1 Complete Data Flow Test

```powershell
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "End-to-End Data Flow Test" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Step 1: Set DeleteAll mode to 1
Write-Host "`nStep 1: Setting DeleteAll mode to 1..." -ForegroundColor Cyan
try {
    $conn = New-Object System.Data.Odbc.OdbcConnection("FILEDSN=.\ReadLogsHRMS.dsn")
    $conn.Open()
    $cmd = $conn.CreateCommand()
    
    # Note: This assumes Settings table is in SQL Server
    # If it's only in Access, we'll need to update Access instead
    Write-Host "  (Updating Settings in Access database)" -ForegroundColor Yellow
    $conn.Close()
    
    # Update Access database
    $accessConn = New-Object System.Data.OleDb.OleDbConnection("Provider=Microsoft.Jet.OLEDB.4.0;Data Source=.\RCMSBio.mdb;Jet OLEDB:Database Password=szus")
    $accessConn.Open()
    $accessCmd = $accessConn.CreateCommand()
    $accessCmd.CommandText = "UPDATE Settings SET SettingValue = '1' WHERE SettingName = 'DeleteAll'"
    $accessCmd.ExecuteNonQuery() | Out-Null
    Write-Host "✓ DeleteAll set to 1" -ForegroundColor Green
    $accessConn.Close()
} catch {
    Write-Host "✗ Error setting DeleteAll: $_" -ForegroundColor Red
}

# Step 2: Run service
Write-Host "`nStep 2: Running service..." -ForegroundColor Cyan
.\DataCollectionService.exe 1

# Step 3: Verify data flow
Write-Host "`nStep 3: Verifying data flow..." -ForegroundColor Cyan

try {
    $conn = New-Object System.Data.Odbc.OdbcConnection("FILEDSN=.\ReadLogsHRMS.dsn")
    $conn.Open()
    $cmd = $conn.CreateCommand()
    
    # Check 0RawLog
    $cmd.CommandText = "SELECT COUNT(*) FROM [0RawLog]"
    $rawCount = $cmd.ExecuteScalar()
    Write-Host "  ✓ [0RawLog] has $rawCount records" -ForegroundColor $(if ($rawCount -gt 0) { "Green" } else { "Yellow" })
    
    # Check AttenInfo
    $cmd.CommandText = "SELECT COUNT(*) FROM [AttenInfo]"
    $attenCount = $cmd.ExecuteScalar()
    Write-Host "  ✓ [AttenInfo] has $attenCount records" -ForegroundColor $(if ($attenCount -gt 0) { "Green" } else { "Yellow" })
    
    # Check transfer flags
    $cmd.CommandText = "SELECT COUNT(*) FROM [0RawLog] WHERE vtrfFlag = '1'"
    $processedCount = $cmd.ExecuteScalar()
    Write-Host "  ✓ $processedCount records marked as processed" -ForegroundColor Green
    
    $conn.Close()
} catch {
    Write-Host "  ✗ Error verifying data: $_" -ForegroundColor Red
}

# Step 4: Check DeleteAll reset
Write-Host "`nStep 4: Checking DeleteAll reset..." -ForegroundColor Cyan
try {
    $accessConn = New-Object System.Data.OleDb.OleDbConnection("Provider=Microsoft.Jet.OLEDB.4.0;Data Source=.\RCMSBio.mdb;Jet OLEDB:Database Password=szus")
    $accessConn.Open()
    $accessCmd = $accessConn.CreateCommand()
    $accessCmd.CommandText = "SELECT SettingValue FROM Settings WHERE SettingName = 'DeleteAll'"
    $deleteAllValue = $accessCmd.ExecuteScalar()
    
    if ($deleteAllValue -eq "0") {
        Write-Host "  ✓ DeleteAll reset to 0" -ForegroundColor Green
    } else {
        Write-Host "  ✗ DeleteAll is still: $deleteAllValue" -ForegroundColor Yellow
    }
    
    $accessConn.Close()
} catch {
    Write-Host "  ✗ Error checking DeleteAll: $_" -ForegroundColor Red
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "End-to-End Test Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
```

---

## Summary and Next Steps

```powershell
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Testing Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "What to check:" -ForegroundColor White
Write-Host "  1. All database connections working"
Write-Host "  2. Service runs without errors"
Write-Host "  3. Log files created with details"
Write-Host "  4. Data appears in 0RawLog table"
Write-Host "  5. Data processed into AttenInfo table"
Write-Host "  6. Service installed successfully"
Write-Host "  7. Scheduled task created and runs"
Write-Host ""
Write-Host "If devices are not accessible:" -ForegroundColor Yellow
Write-Host "  - Service will log connection errors (this is normal)"
Write-Host "  - You can still test database operations"
Write-Host "  - Insert test data manually to verify processing"
Write-Host ""
Write-Host "For production deployment:" -ForegroundColor Cyan
Write-Host "  1. Ensure all 6 devices are accessible"
Write-Host "  2. Verify employee data in M_Executive table"
Write-Host "  3. Test during business hours"
Write-Host "  4. Monitor logs for first few executions"
Write-Host "  5. Verify data accuracy in AttenInfo"
Write-Host ""
Write-Host "Documentation:" -ForegroundColor White
Write-Host "  - DEPLOYMENT.md - Full deployment guide"
Write-Host "  - DSN-SETUP.md - Database configuration"
Write-Host "  - TEST-CHECKLIST.md - Comprehensive test checklist"
Write-Host ""
```

## Troubleshooting Quick Reference

### Device Connection Errors
- **Normal if devices not accessible** - service will log and continue
- Check network connectivity: `ping 192.168.2.224`
- Verify devices are powered on
- Check firewall rules

### Database Connection Errors
- Verify DSN file configuration
- Test SQL Server with Management Studio
- Check Access database password
- Ensure ODBC driver installed

### No Data Collected
- Check DeleteAll setting in Settings table
- Verify devices have new logs
- Review log files for errors
- Check date filtering (BackYearBlocked)

### Service Won't Install
- Run PowerShell as Administrator
- Check .NET Framework 4.7.2 installed
- Verify no existing service with same name
- Check Windows Event Log for details

### Scheduled Task Not Running
- Verify task is enabled
- Check trigger conditions
- Review Task Scheduler history
- Ensure service account has permissions

---

## Quick Test Commands

```powershell
# Quick database connection test
$conn = New-Object System.Data.Odbc.OdbcConnection("FILEDSN=.\ReadLogsHRMS.dsn"); $conn.Open(); Write-Host "SQL: OK"; $conn.Close()

# Quick service run
.\DataCollectionService.exe 1

# View latest log
Get-ChildItem .\Logs | Sort LastWriteTime -Desc | Select -First 1 | Get-Content

# Check data count
$conn = New-Object System.Data.Odbc.OdbcConnection("FILEDSN=.\ReadLogsHRMS.dsn"); $conn.Open(); $cmd = $conn.CreateCommand(); $cmd.CommandText = "SELECT COUNT(*) FROM [0RawLog]"; Write-Host "0RawLog: $($cmd.ExecuteScalar())"; $cmd.CommandText = "SELECT COUNT(*) FROM [AttenInfo]"; Write-Host "AttenInfo: $($cmd.ExecuteScalar())"; $conn.Close()

# Check service status
Get-Service DataCollectionService

# Check scheduled task
Get-ScheduledTask BiometricDataCollection | Select TaskName, State, @{N='LastRun';E={(Get-ScheduledTaskInfo $_).LastRunTime}}
```
