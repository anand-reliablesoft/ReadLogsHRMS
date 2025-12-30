# TimeSyncService - Deployment Checklist

## Pre-Deployment Checklist

### System Requirements
- [ ] Windows Server 2012 R2 or higher
- [ ] .NET Framework 4.7.2 or higher installed
- [ ] Administrator privileges available

### Network Requirements
- [ ] Network connectivity to biometric devices (192.168.2.224-229)
- [ ] Firewall rules configured for device communication (port 5005)

### Hardware Requirements
- [ ] SBXPC ActiveX Control (SBXPC.ocx) available
- [ ] Biometric devices powered on and network accessible

## Deployment Steps

### Step 1: Prepare Deployment Directory
- [ ] Create directory: `C:\BiometricServices\TimeSync\`
- [ ] Copy all files from this package to the directory
- [ ] Verify all files copied successfully

### Step 2: Register COM Components
- [ ] Run as Administrator: `regsvr32 SBXPC.ocx`
- [ ] Verify no error messages during registration
- [ ] Test COM registration: `reg query "HKEY_CLASSES_ROOT\SBXPC.SBXPC"`

### Step 3: Install Service
- [ ] Run PowerShell as Administrator
- [ ] Execute: `.\Install-TimeSyncService.ps1`
- [ ] Verify service installed: `Get-Service TimeSyncService`
- [ ] Verify scheduled task created: `Get-ScheduledTask -TaskName "TimeSyncService_Daily"`

### Step 4: Test Installation
- [ ] Run console test: `TimeSyncService.exe --console`
- [ ] Check for log files in `TLogs\` directory
- [ ] Verify no errors in console output
- [ ] Check device time synchronization

## Post-Deployment Verification

### Service Status
- [ ] Service status: `Get-Service TimeSyncService`
- [ ] Scheduled task status: `Get-ScheduledTask -TaskName "TimeSyncService_Daily"`
- [ ] Windows Event Log entries (no errors)

### Device Connectivity
- [ ] All 6 devices accessible via ping
- [ ] Service can connect to each device
- [ ] Time synchronization working for each device
- [ ] No connection errors in logs

### Time Synchronization
- [ ] Device times match server time after sync
- [ ] All devices synchronized within acceptable tolerance
- [ ] Sync process completes without errors
- [ ] Log files show successful synchronization

### Scheduled Execution
- [ ] Scheduled task runs daily at 2:00 AM
- [ ] Service executes automatically
- [ ] Logs generated for each execution
- [ ] No recurring errors

## Testing Procedures

### Manual Console Test
```cmd
cd C:\BiometricServices\TimeSync
TimeSyncService.exe --console
```
Expected: Service runs, connects to devices, synchronizes time, exits cleanly

### Service Test
```powershell
Start-Service TimeSyncService
Get-Service TimeSyncService
Stop-Service TimeSyncService
```
Expected: Service starts and stops without errors

### Scheduled Task Test
```powershell
Start-ScheduledTask -TaskName "TimeSyncService_Daily"
Get-ScheduledTaskInfo -TaskName "TimeSyncService_Daily"
```
Expected: Task runs successfully, shows last run result

### Device Connectivity Test
```cmd
ping 192.168.2.224
ping 192.168.2.225
ping 192.168.2.226
ping 192.168.2.227
ping 192.168.2.228
ping 192.168.2.229
```
Expected: All devices respond to ping

## Troubleshooting

### Common Issues
- **Service won't start**: Check COM registration, .NET Framework
- **Device connection fails**: Check network, firewall, device power
- **COM errors**: Re-register SBXPC.ocx, verify x86 build
- **Time sync fails**: Check device settings, administrator access

### Log Locations
- Service logs: `C:\BiometricServices\TimeSync\TLogs\`
- Windows Event Log: Application log, source "TimeSyncService"
- Scheduled Task history: Task Scheduler

### Support Files
- `DEPLOYMENT.md` - Detailed deployment guide
- Installation scripts with built-in help

## Rollback Plan

If deployment fails:
1. Run: `.\Uninstall-TimeSyncService.ps1`
2. Remove service directory
3. Unregister COM component: `regsvr32 /u SBXPC.ocx`
4. Restore previous configuration

## Success Criteria

Deployment is successful when:
- [ ] Service installed and running
- [ ] Scheduled task executing daily at 2:00 AM
- [ ] All devices synchronizing time correctly
- [ ] No errors in logs for 48 hours
- [ ] All 6 devices responding correctly

## Coordination with DataCollectionService

If deploying both services:
- [ ] TimeSyncService scheduled before DataCollectionService (2:00 AM vs 6:00 AM start)
- [ ] Both services use same device configurations
- [ ] No scheduling conflicts
- [ ] Time sync completes before data collection begins

---
**Deployment Date:** ___________  
**Deployed By:** ___________  
**Verified By:** ___________