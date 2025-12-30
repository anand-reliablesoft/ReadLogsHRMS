# Data Collection Service - Test Checklist

## Build Verification

- [x] Solution builds successfully in Release mode
- [x] No compilation errors or warnings
- [x] All required DLLs present in output directory:
  - [x] DataCollectionService.exe
  - [x] BiometricAttendance.Common.dll
  - [x] SBXPCDLL.dll
  - [x] SBPCCOMM.dll
  - [x] DataCollectionService.exe.config

## Pre-Deployment Tests

### Configuration Files

- [ ] App.config has all required settings:
  - [ ] AccessDbPath
  - [ ] AccessDbPassword
  - [ ] SqlDsnFile
  - [ ] LogDirectory
  - [ ] BackYearBlocked
- [ ] ReadLogsHRMS.dsn.template exists
- [ ] DSN-SETUP.md documentation exists
- [ ] Installation scripts exist:
  - [ ] Install-DataCollectionService.ps1
  - [ ] Uninstall-DataCollectionService.ps1
  - [ ] Install-Using-InstallUtil.ps1

### Database Connectivity Tests

#### Access Database
- [ ] RCMSBio.mdb file accessible
- [ ] Can connect with configured password
- [ ] M_Executive table exists with BioID field
- [ ] Settings table exists
- [ ] 0RawLog table exists with correct structure

#### SQL Server Database
- [ ] DSN file configured with correct credentials
- [ ] Can connect to SQL Server via DSN
- [ ] 0RawLog table exists with correct structure:
  - [ ] ID (INT, Identity)
  - [ ] vTMachineNumber (NUMERIC 18,0)
  - [ ] vSMachineNumber (NUMERIC 18,0)
  - [ ] vSEnrollNumber (NUMERIC 18,0)
  - [ ] vVerifyMode (NUMERIC 18,0)
  - [ ] vYear, vMonth, vDay, vHour, vMinute, vSecond (NUMERIC 18,0)
  - [ ] vInOut (NVARCHAR 1)
  - [ ] vtrfFlag (NVARCHAR 1)
- [ ] AttenInfo table exists with correct structure:
  - [ ] Srno (INT, Identity)
  - [ ] EmpCode (NVARCHAR 50)
  - [ ] TicketNo (INT)
  - [ ] EntryDate (DATETIME)
  - [ ] InOutFlag (NVARCHAR 1)
  - [ ] EntryTime (DATETIME)
  - [ ] TrfFlag (INT)
  - [ ] UpdateUID, Location, ErrMsg (NVARCHAR)

### Device Connectivity Tests

- [ ] Network connectivity to device 1 (192.168.2.224:4370)
- [ ] Network connectivity to device 2 (192.168.2.225:4370)
- [ ] Network connectivity to device 3 (192.168.2.226:4370)
- [ ] Network connectivity to device 4 (192.168.2.227:4370)
- [ ] Network connectivity to device 5 (192.168.2.228:4370)
- [ ] Network connectivity to device 6 (192.168.2.229:4370)
- [ ] SBXPC SDK registered (regsvr32 SBXPCDLL.dll)

## Installation Tests

### Service Installation

- [ ] Service installs successfully
- [ ] Service appears in Services list
- [ ] Service description set correctly
- [ ] Service recovery options configured
- [ ] Log directory created automatically

### Scheduled Task Creation

- [ ] Scheduled task created successfully
- [ ] Task name: BiometricDataCollection
- [ ] Trigger: Every 15 minutes, 6 AM - 8 PM, weekdays
- [ ] Action: DataCollectionService.exe with argument "1"
- [ ] Task runs with highest privileges
- [ ] Task configured to run whether user logged on or not

## Functional Tests

### Manual Execution Test

- [ ] Run: `DataCollectionService.exe 1`
- [ ] Service starts without errors
- [ ] Log files created in Logs directory:
  - [ ] Main log file (LogYYYYMMDDHHMMSS.txt)
  - [ ] Machine-specific logs (Log_Machine1YYYYMMDDHHMMSS.txt, etc.)
- [ ] Service completes and exits gracefully

### Data Collection Test

- [ ] Service connects to each device successfully
- [ ] Attendance logs read from devices
- [ ] Raw logs inserted into Access 0RawLog table
- [ ] Raw logs inserted into SQL Server 0RawLog table
- [ ] Duplicate detection working (no duplicate inserts)
- [ ] Year filtering working (BackYearBlocked >= 2023)
- [ ] IN/OUT flags correct per device:
  - [ ] Device 1: IN (I)
  - [ ] Device 2: OUT (O)
  - [ ] Device 3: IN (I)
  - [ ] Device 4: OUT (O)
  - [ ] Device 5: IN (I)
  - [ ] Device 6: OUT (O)

### Employee ID Mapping Test

- [ ] Enrollment numbers mapped to employee IDs
- [ ] M_Executive table queried correctly
- [ ] Falls back to enrollment number if not found
- [ ] Handles null/empty EmpID correctly

### Attendance Record Processing Test

- [ ] Unprocessed raw logs identified (vtrfFlag = 0 or null)
- [ ] Records processed in chronological order
- [ ] Employee IDs mapped correctly
- [ ] Attendance records inserted into AttenInfo table
- [ ] Duplicate detection working in AttenInfo
- [ ] vtrfFlag updated to '1' after processing
- [ ] Date/time formatting correct:
  - [ ] EntryDate contains date portion
  - [ ] EntryTime contains time portion
- [ ] TicketNo set to 0
- [ ] TrfFlag set to 0

### Batch Processing Test

- [ ] Batch 1 processes machines 1-4
- [ ] part2 flag logic working correctly
- [ ] Batch processing executes when part2 = true
- [ ] Second executable launched when part2 = false
- [ ] DeleteAll mode read from Settings table
- [ ] DeleteAll mode set to 0 after successful processing

### Delete All Mode Test

#### Test with DeleteAll = 1 (Read All)
- [ ] Set DeleteAll to 1 in Settings table
- [ ] Run service
- [ ] All logs read from devices (ReadAllGLogData used)
- [ ] DeleteAll reset to 0 after processing

#### Test with DeleteAll = 0 (Read New Only)
- [ ] Set DeleteAll to 0 in Settings table
- [ ] Run service
- [ ] Only new logs read from devices (ReadGeneralLogData used)
- [ ] Read mark functionality working

### Error Handling Tests

#### Device Offline Test
- [ ] Disconnect one device
- [ ] Run service
- [ ] Error logged for offline device
- [ ] Other devices processed successfully
- [ ] Service completes without crashing

#### Database Connection Error Test
- [ ] Temporarily break SQL Server connection
- [ ] Run service
- [ ] Connection retry logic activates
- [ ] Error logged appropriately
- [ ] Service handles gracefully

#### Invalid Data Test
- [ ] Test with invalid date/time components
- [ ] Invalid records skipped
- [ ] Error logged
- [ ] Processing continues

### Logging Tests

- [ ] Log files created with correct naming convention
- [ ] Timestamps in log entries
- [ ] Connection status logged
- [ ] Record counts logged
- [ ] Errors logged with details
- [ ] Duplicate detection logged
- [ ] Windows Event Log entries for critical errors

## Performance Tests

### Volume Test
- [ ] Test with 100+ records per device
- [ ] Processing completes in reasonable time
- [ ] Memory usage acceptable
- [ ] No memory leaks

### Concurrent Access Test
- [ ] Multiple database connections handled correctly
- [ ] Transactions commit successfully
- [ ] No deadlocks or blocking

## Integration Tests

### End-to-End Test
1. [ ] Set DeleteAll to 1
2. [ ] Run service manually
3. [ ] Verify data flow:
   - [ ] Devices → 0RawLog (Access)
   - [ ] Devices → 0RawLog (SQL Server)
   - [ ] 0RawLog → AttenInfo
4. [ ] Verify employee ID mapping
5. [ ] Verify IN/OUT flags
6. [ ] Verify DeleteAll reset to 0
7. [ ] Check all log files

### Scheduled Task Test
- [ ] Enable scheduled task
- [ ] Wait for scheduled execution
- [ ] Verify task runs automatically
- [ ] Check Task Scheduler history
- [ ] Verify logs created
- [ ] Verify data collected

### Multi-Execution Test
- [ ] Run service multiple times
- [ ] Verify no duplicate data
- [ ] Verify incremental reading works
- [ ] Verify vtrfFlag prevents reprocessing

## Table Structure Verification

### 0RawLog Table
- [ ] Structure matches requirements exactly
- [ ] No columns added or removed
- [ ] Data types correct
- [ ] Identity column working

### AttenInfo Table
- [ ] Structure matches requirements exactly
- [ ] No columns added or removed
- [ ] Data types correct
- [ ] Identity column working
- [ ] Compatible with existing HR software

### M_Executive Table
- [ ] BioID field exists
- [ ] EmpID field exists
- [ ] Mapping data present

## Security Tests

- [ ] Service account has minimum required permissions
- [ ] Database credentials secured
- [ ] DSN file permissions restricted
- [ ] Log files not world-readable

## Documentation Tests

- [ ] DEPLOYMENT.md complete and accurate
- [ ] DSN-SETUP.md complete and accurate
- [ ] Installation scripts documented
- [ ] Troubleshooting section helpful
- [ ] Configuration options explained

## Uninstallation Test

- [ ] Run Uninstall-DataCollectionService.ps1
- [ ] Service removed successfully
- [ ] Scheduled task removed successfully
- [ ] Log files preserved (unless -RemoveLogs used)
- [ ] No orphaned registry entries

## Sign-Off

### Build Status
- Build Date: _______________
- Build Configuration: Release x86
- Build Status: ✓ Success
- Diagnostics: ✓ No Errors

### Test Results Summary
- Total Tests: _______________
- Passed: _______________
- Failed: _______________
- Skipped: _______________

### Deployment Readiness
- [ ] All critical tests passed
- [ ] Documentation complete
- [ ] Installation scripts tested
- [ ] Rollback procedure documented
- [ ] Support team notified

### Approvals
- Developer: _______________ Date: _______________
- QA: _______________ Date: _______________
- System Administrator: _______________ Date: _______________

## Notes

Use this section to document any issues, workarounds, or special considerations:

_______________________________________________________________________________
_______________________________________________________________________________
_______________________________________________________________________________
_______________________________________________________________________________
