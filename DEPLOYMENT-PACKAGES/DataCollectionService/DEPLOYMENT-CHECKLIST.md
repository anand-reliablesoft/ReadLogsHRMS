# DataCollectionService - Deployment Checklist

## Pre-Deployment Checklist

### System Requirements
- [ ] Windows Server 2012 R2 or higher
- [ ] .NET Framework 4.7.2 or higher installed
- [ ] ODBC Driver 13+ for SQL Server installed
- [ ] Microsoft Access Database Engine installed
- [ ] Administrator privileges available

### Network Requirements
- [ ] Network connectivity to biometric devices (192.168.2.224-229)
- [ ] SQL Server accessible (if remote)
- [ ] Firewall rules configured for device communication (port 4370)

### Database Requirements
- [ ] SQL Server database (anandDB) accessible
- [ ] Required tables exist: 0RawLog, AttenInfo, M_Executive
- [ ] Access database (RCMSBio.mdb) available with employee data
- [ ] Database credentials configured

## Deployment Steps

### Step 1: Prepare Deployment Directory
- [ ] Create directory: `C:\BiometricServices\DataCollection\`
- [ ] Copy all files from this package to the directory
- [ ] Verify all files copied successfully

### Step 2: Configure Database Connection
- [ ] Edit `ReadLogsHRMS.dsn` with SQL Server details
- [ ] Test DSN connection (see DSN-SETUP.md)
- [ ] Verify Access database password in App.config
- [ ] Test Access database connection

### Step 3: Register COM Components
- [ ] Run as Administrator: `regsvr32 SBXPCDLL.dll`
- [ ] Run as Administrator: `regsvr32 SBPCCOMM.dll`
- [ ] Verify no error messages during registration

### Step 4: Install Service
- [ ] Run PowerShell as Administrator
- [ ] Execute: `.\Install-DataCollectionService.ps1`
- [ ] Verify service installed: `Get-Service DataCollectionService`
- [ ] Verify scheduled task created: `Get-ScheduledTask -TaskName "BiometricDataCollection"`

### Step 5: Test Installation
- [ ] Run manual test: `DataCollectionService.exe 1`
- [ ] Check for log files in `Logs\` directory
- [ ] Verify no errors in console output
- [ ] Check data in SQL Server tables

## Post-Deployment Verification

### Service Status
- [ ] Service status: `Get-Service DataCollectionService`
- [ ] Scheduled task status: `Get-ScheduledTask -TaskName "BiometricDataCollection"`
- [ ] Windows Event Log entries (no errors)

### Database Verification
- [ ] Raw logs in 0RawLog table
- [ ] Processed records in AttenInfo table
- [ ] Employee mapping working correctly
- [ ] DeleteAll mode functioning (Settings table)

### Device Connectivity
- [ ] All 6 devices accessible via ping
- [ ] Service can connect to each device
- [ ] Data collection from each device working
- [ ] No connection errors in logs

### Scheduled Execution
- [ ] Scheduled task runs automatically
- [ ] Service executes every 15 minutes during business hours
- [ ] Logs generated for each execution
- [ ] No recurring errors

## Troubleshooting

### Common Issues
- **Service won't start**: Check COM registration, .NET Framework
- **Database connection fails**: Verify DSN configuration, credentials
- **Device connection fails**: Check network, firewall, device power
- **No data collected**: Check device connectivity, employee mapping

### Log Locations
- Service logs: `C:\BiometricServices\DataCollection\Logs\`
- Windows Event Log: Application log, source "DataCollectionService"
- Scheduled Task history: Task Scheduler

### Support Files
- `DEPLOYMENT.md` - Detailed deployment guide
- `DSN-SETUP.md` - Database connection setup
- `TEST-RESULTS.md` - Testing procedures
- `PATH-FIXES-SUMMARY.md` - Path resolution fixes

## Rollback Plan

If deployment fails:
1. Run: `.\Uninstall-DataCollectionService.ps1`
2. Remove service directory
3. Unregister COM components: `regsvr32 /u SBXPCDLL.dll`
4. Restore previous configuration

## Success Criteria

Deployment is successful when:
- [ ] Service installed and running
- [ ] Scheduled task executing automatically
- [ ] Data flowing from devices to databases
- [ ] No errors in logs for 24 hours
- [ ] All 6 devices responding correctly

---
**Deployment Date:** ___________  
**Deployed By:** ___________  
**Verified By:** ___________