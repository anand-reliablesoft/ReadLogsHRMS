# Data Collection Service

## Quick Start

This is the Biometric Data Collection Service that collects attendance data from biometric devices and stores it in databases.

### Installation

1. **Prerequisites**: Ensure .NET Framework 4.7.2, ODBC Driver 13 for SQL Server, and Microsoft Access Database Engine are installed

2. **Configure DSN**: Copy `ReadLogsHRMS.dsn.template` to `ReadLogsHRMS.dsn` and edit with your SQL Server details

3. **Install Service**: Run PowerShell as Administrator:
   ```powershell
   .\Install-DataCollectionService.ps1
   ```

4. **Test**: Run manually to verify:
   ```cmd
   DataCollectionService.exe 1
   ```

### Documentation

- **DEPLOYMENT.md** - Complete deployment guide with troubleshooting
- **DSN-SETUP.md** - Database connection configuration instructions
- **TEST-CHECKLIST.md** - Comprehensive testing checklist

### Files

- `DataCollectionService.exe` - Main service executable
- `BiometricAttendance.Common.dll` - Shared library
- `SBXPCDLL.dll`, `SBPCCOMM.dll` - Biometric device SDK
- `DataCollectionService.exe.config` - Configuration file
- `ReadLogsHRMS.dsn.template` - DSN template (copy and configure)
- `Install-DataCollectionService.ps1` - Installation script
- `Uninstall-DataCollectionService.ps1` - Uninstallation script

### Support

For issues, check:
1. Log files in `Logs\` directory
2. Windows Event Viewer (Application log)
3. DEPLOYMENT.md troubleshooting section

### Version

- Version: 1.0
- Platform: .NET Framework 4.7.2
- Build: Release x86
- Date: November 2025
