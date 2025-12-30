# Quick Setup Guide - Biometric Attendance System

## Overview

This guide helps you quickly deploy both services with a custom installation path.

## Step 1: Configure Installation Path

Edit the `deployment-config.json` file to set your desired installation path:

```json
{
  "deploymentSettings": {
    "rootInstallPath": "YOUR_CUSTOM_PATH_HERE",
    "dataCollectionServicePath": "DataCollection",
    "timeSyncServicePath": "TimeSync"
  }
}
```

### Example Paths:
- `C:\\BiometricServices` (default)
- `D:\\Services\\Biometric`
- `C:\\Program Files\\BiometricAttendance`
- `E:\\Applications\\AttendanceSystem`

**Important:** Only change the `rootInstallPath` value. The subdirectories will be created automatically.

## Step 2: View Current Configuration

```powershell
.\Get-DeploymentConfig.ps1
```

This shows you the current configuration and computed paths.

## Step 3: Deploy Both Services

Run as Administrator:

```powershell
# Test deployment (no actual installation)
.\Deploy-Both-Services.ps1 -TestOnly

# Actual deployment
.\Deploy-Both-Services.ps1
```

### Advanced Options:

```powershell
# Skip prerequisites check
.\Deploy-Both-Services.ps1 -SkipPrerequisites

# Show configuration only
.\Deploy-Both-Services.ps1 -ShowConfig

# Test with custom config file
.\Deploy-Both-Services.ps1 -ConfigPath "custom-config.json" -TestOnly
```

## Step 4: Verify Installation

After deployment, your services will be installed at:
- **TimeSyncService:** `{rootInstallPath}\TimeSync\`
- **DataCollectionService:** `{rootInstallPath}\DataCollection\`

Check services:
```powershell
Get-Service TimeSyncService, DataCollectionService
Get-ScheduledTask -TaskName "*Biometric*"
```

## Step 5: Configure Database Connection

Edit the DSN file in the DataCollectionService directory:
```
{rootInstallPath}\DataCollection\ReadLogsHRMS.dsn
```

See `DSN-SETUP.md` for detailed instructions.

## Step 6: Test Services

```powershell
# Test TimeSyncService
cd "{rootInstallPath}\TimeSync"
.\TimeSyncService.exe --console

# Test DataCollectionService  
cd "{rootInstallPath}\DataCollection"
.\DataCollectionService.exe 1
```

## Troubleshooting

### Common Issues:

1. **Permission Denied**
   - Run PowerShell as Administrator
   - Check User Account Control (UAC) settings

2. **Path Not Found**
   - Verify the root path exists and is accessible
   - Check drive letter and folder permissions

3. **Service Installation Failed**
   - Ensure .NET Framework 4.7.2+ is installed
   - Check Windows Event Log for details

4. **COM Registration Failed**
   - Verify SBXPC files are present in service directories
   - Run `regsvr32` manually if needed

### Get Help:

```powershell
# Show deployment configuration
.\Get-DeploymentConfig.ps1

# Test deployment without installing
.\Deploy-Both-Services.ps1 -TestOnly

# Check individual service installation scripts
.\DataCollectionService\Install-DataCollectionService.ps1 -?
.\TimeSyncService\Install-TimeSyncService.ps1 -?
```

## File Structure After Installation

```
{rootInstallPath}\
├── DataCollection\
│   ├── DataCollectionService.exe
│   ├── BiometricAttendance.Common.dll
│   ├── RCMSBio.mdb
│   ├── ReadLogsHRMS.dsn
│   ├── Logs\
│   └── Installation scripts
└── TimeSync\
    ├── TimeSyncService.exe
    ├── BiometricAttendance.Common.dll
    ├── SBXPC.ocx
    ├── TLogs\
    └── Installation scripts
```

## Scheduled Tasks Created

- **TimeSyncService_Daily:** Runs daily at 2:00 AM
- **BiometricDataCollection:** Runs every 15 minutes, 6:00 AM - 8:00 PM, weekdays

## Next Steps

1. Configure SQL Server connection in DSN file
2. Verify employee data in Access database
3. Test device connectivity
4. Monitor logs for first 24 hours
5. Set up monitoring and maintenance procedures

---

**Need Help?** Check the individual deployment checklists in each service folder for detailed troubleshooting steps.