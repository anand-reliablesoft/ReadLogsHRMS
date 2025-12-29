# Time Sync Service - Deployment and Testing Guide

## Overview

The Time Sync Service is a Windows service that synchronizes the system time across all biometric attendance devices. This guide covers building, testing, and deploying the service.

## Prerequisites

Before installation, ensure you have:

1. **Windows Server** (or Windows 10/11 for testing)
2. **.NET Framework 4.7.2** or higher installed
3. **SBXPC ActiveX Control** (SBXPC.ocx) from the biometric device manufacturer
4. **Administrator privileges** for service installation
5. **Network connectivity** to biometric devices (default: 192.168.2.224-229)

## Building the Service

### Build for Release (x86)

The service must be built for x86 platform to ensure COM interop compatibility with the SBXPC ActiveX control.

```powershell
# Using dotnet CLI
dotnet build BiometricAttendanceSystem.sln -c Release /p:Platform=x86

# Or using MSBuild
msbuild BiometricAttendanceSystem.sln /p:Configuration=Release /p:Platform=x86
```

**Output Location:** `TimeSyncService\bin\x86\Release\`

**Files Generated:**
- `TimeSyncService.exe` - Main service executable
- `TimeSyncService.exe.config` - Configuration file
- `BiometricAttendance.Common.dll` - Shared library
- `*.pdb` - Debug symbols (optional for production)

## Installation

### Automated Installation

Run the PowerShell installation script with administrator privileges:

```powershell
# Navigate to the TimeSyncService directory
cd TimeSyncService

# Run the installation script
.\Install-TimeSyncService.ps1
```

The script will:
1. Register the SBXPC ActiveX control (if SBXPC.ocx is in the same directory)
2. Install the TimeSyncService Windows service
3. Create the TLogs directory for log files
4. Create a Windows Scheduler task to run daily at 2:00 AM

### Manual Installation

If you prefer manual installation:

#### 1. Register SBXPC ActiveX Control

```cmd
regsvr32 "path\to\SBXPC.ocx"
```

#### 2. Install the Service

```cmd
sc create TimeSyncService binPath= "F:\Path\To\TimeSyncService.exe" start= demand DisplayName= "Biometric Time Sync Service"
sc description TimeSyncService "Synchronizes time across biometric attendance devices"
```

#### 3. Create Log Directory

```powershell
New-Item -ItemType Directory -Path "F:\Path\To\TimeSyncService\bin\x86\Release\TLogs" -Force
```

#### 4. Create Scheduled Task

Use Task Scheduler GUI or PowerShell:

```powershell
$action = New-ScheduledTaskAction -Execute "F:\Path\To\TimeSyncService.exe"
$trigger = New-ScheduledTaskTrigger -Daily -At "2:00AM"
$principal = New-ScheduledTaskPrincipal -UserId "SYSTEM" -LogonType ServiceAccount -RunLevel Highest
$settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable

Register-ScheduledTask -TaskName "TimeSyncService_Daily" -Action $action -Trigger $trigger -Principal $principal -Settings $settings -Description "Daily time synchronization for biometric devices"
```

## Configuration

### App.config Settings

The service reads configuration from `TimeSyncService.exe.config`:

```xml
<appSettings>
  <add key="LogDirectory" value="TLogs"/>
</appSettings>
```

**Settings:**
- `LogDirectory` - Directory for log files (relative to executable location)

### Machine Configuration

Device configurations are hardcoded in `MachineConfigurationProvider.cs`:

```csharp
Machine 1: 192.168.2.224:5005 (IN)
Machine 2: 192.168.2.225:5005 (OUT)
Machine 3: 192.168.2.226:5005 (IN)
Machine 4: 192.168.2.227:5005 (OUT)
Machine 5: 192.168.2.228:5005 (IN)
Machine 6: 192.168.2.229:5005 (OUT)
```

To modify device configurations, edit the `GetAllMachines()` method in the source code and rebuild.

## Testing

### Test 0: Console Mode Testing (Recommended First)

Before installing as a service, test the executable in console mode:

```cmd
# Navigate to the Release directory
cd TimeSyncService\bin\x86\Release

# Run in console mode (no installation required)
TimeSyncService.exe --console
```

This will:
- Run the service logic without requiring Windows service installation
- Display output directly in the console
- Allow you to see errors immediately
- Exit when complete (press any key)

**Benefits:**
- No administrator privileges required
- Easy debugging
- Quick testing of device connectivity
- Immediate feedback

**Note:** In console mode, the service will execute once and exit. This is perfect for testing before deploying as a Windows service.

### Test 1: Verify Build Output

```powershell
# Check that all required files exist
Test-Path "TimeSyncService\bin\x86\Release\TimeSyncService.exe"
Test-Path "TimeSyncService\bin\x86\Release\BiometricAttendance.Common.dll"
Test-Path "TimeSyncService\bin\x86\Release\TimeSyncService.exe.config"
```

All should return `True`.

### Test 2: Verify Service Installation

```powershell
# Check service status
Get-Service -Name "TimeSyncService"
```

Expected output:
```
Status   Name               DisplayName
------   ----               -----------
Stopped  TimeSyncService    Biometric Time Sync Service
```

### Test 3: Test COM Interop with SBXPC

Before testing with real devices, verify the SBXPC control is registered:

```cmd
# Check registry for SBXPC
reg query "HKEY_CLASSES_ROOT\SBXPC.SBXPC" /s
```

If not found, register it:
```cmd
regsvr32 "path\to\SBXPC.ocx"
```

### Test 4: Manual Service Execution

Run the service manually to test functionality:

```cmd
# Start the service
sc start TimeSyncService

# Check service status
sc query TimeSyncService

# Stop the service
sc stop TimeSyncService
```

### Test 5: Verify Log File Creation

After running the service, check for log files:

```powershell
# List log files
Get-ChildItem "TimeSyncService\bin\x86\Release\TLogs" -Filter "TLog*.txt"
```

Expected: Files named like `TLog20241111020000.txt` and `TLog20241111020000_1.txt` (per machine)

### Test 6: Review Log Content

```powershell
# View the main log file (most recent)
Get-Content "TimeSyncService\bin\x86\Release\TLogs\TLog*.txt" | Select-Object -Last 50
```

Expected log entries:
```
[2024-11-11 02:00:00] Time Sync Service started
[2024-11-11 02:00:01] Processing machine 1 (192.168.2.224:4370)
[2024-11-11 02:00:02] Connected to machine 1
[2024-11-11 02:00:03] Time synchronized successfully for machine 1
[2024-11-11 02:00:04] Disconnected from machine 1
...
[2024-11-11 02:00:30] Time Sync Service completed successfully
```

### Test 7: Verify Windows Event Log Entries

```powershell
# Check Application event log for service entries
Get-EventLog -LogName Application -Source "TimeSyncService" -Newest 10
```

Errors should be logged here if any occur.

### Test 8: Test Connection to Biometric Device

To test connectivity to a specific device, you can temporarily modify the code or use the VB6 test application. Ensure:

1. Device is powered on
2. Device is on the network (ping test)
3. Port 5005 is accessible (firewall rules)

```cmd
# Test network connectivity
ping 192.168.2.224
ping 192.168.2.225
ping 192.168.2.226
ping 192.168.2.227
ping 192.168.2.228
ping 192.168.2.229
```

### Test 9: Test Time Synchronization

After successful execution:

1. Check device display to verify time matches server time
2. Review log files for "Time synchronized successfully" messages
3. Verify no error codes in logs

### Test 10: Test Scheduled Task

```powershell
# Run the scheduled task immediately for testing
Start-ScheduledTask -TaskName "TimeSyncService_Daily"

# Check task history
Get-ScheduledTask -TaskName "TimeSyncService_Daily" | Get-ScheduledTaskInfo
```

## Troubleshooting

### Issue: Service fails to start

**Error:** "Service cannot be started. The service process could not connect to the service controller"

**Possible Causes:**
- Running the .exe directly instead of through service controller
- SBXPC ActiveX control not registered
- Missing dependencies (.NET Framework)
- Insufficient permissions

**Solution:**
1. **For testing:** Run in console mode instead: `TimeSyncService.exe --console`
2. **For service mode:** Use `sc start TimeSyncService` or Services.msc, not double-clicking the .exe
3. Check Windows Event Log for error details
4. Verify SBXPC registration: `regsvr32 SBXPC.ocx`
5. Run as Administrator
6. Check service account permissions

### Issue: Cannot connect to devices

**Possible Causes:**
- Network connectivity issues
- Incorrect IP addresses
- Firewall blocking port 5005
- Device powered off

**Solution:**
1. Ping device IP addresses
2. Check firewall rules
3. Verify device power and network connection
4. Review device configuration

### Issue: COM Interop errors

**Possible Causes:**
- SBXPC not registered
- Platform mismatch (AnyCPU vs x86)
- Missing COM dependencies

**Solution:**
1. Ensure service is built for x86 platform
2. Re-register SBXPC: `regsvr32 SBXPC.ocx`
3. Check for missing DLL dependencies

### Issue: Log files not created

**Possible Causes:**
- TLogs directory doesn't exist
- Insufficient write permissions
- Disk full

**Solution:**
1. Create TLogs directory manually
2. Grant write permissions to service account
3. Check disk space

### Issue: Time not synchronizing

**Possible Causes:**
- Device time locked by administrator
- SDK error codes
- Device firmware issues

**Solution:**
1. Check log files for SDK error codes
2. Review device settings
3. Update device firmware if needed

## Uninstallation

### Automated Uninstallation

```powershell
.\Uninstall-TimeSyncService.ps1
```

### Manual Uninstallation

```cmd
# Stop the service
sc stop TimeSyncService

# Delete the service
sc delete TimeSyncService

# Remove scheduled task
schtasks /delete /tn "TimeSyncService_Daily" /f

# Optionally unregister SBXPC
regsvr32 /u "path\to\SBXPC.ocx"
```

## Production Deployment Checklist

- [ ] Build solution in Release|x86 mode
- [ ] Copy executable and dependencies to production server
- [ ] Register SBXPC ActiveX control
- [ ] Install Windows service
- [ ] Create TLogs directory with appropriate permissions
- [ ] Configure scheduled task (daily at 2:00 AM)
- [ ] Test connectivity to all 6 devices
- [ ] Run service manually and verify logs
- [ ] Verify time synchronization on devices
- [ ] Monitor Windows Event Log for errors
- [ ] Document any configuration changes

## Monitoring and Maintenance

### Daily Checks
- Review log files for errors
- Verify scheduled task execution

### Weekly Checks
- Check log file disk usage
- Verify all devices are synchronizing

### Monthly Checks
- Archive old log files
- Review Windows Event Log
- Test manual service execution

## Support

For issues or questions:
1. Check log files in TLogs directory
2. Review Windows Event Log (Application)
3. Verify device connectivity
4. Contact system administrator

---

**Version:** 1.0  
**Last Updated:** November 11, 2024  
**Platform:** Windows Server, .NET Framework 4.7.2, x86
