# Biometric Attendance System - Deployment Packages

## Overview

This directory contains complete deployment packages for both services in the Biometric Attendance System:

1. **DataCollectionService** - Collects attendance data from biometric devices
2. **TimeSyncService** - Synchronizes time across all biometric devices

## Package Contents

### DataCollectionService Package
- Service executable and dependencies
- Configuration files and templates
- Installation/uninstallation scripts
- Documentation and setup guides
- Sample database and DSN files

### TimeSyncService Package
- Service executable and dependencies
- Configuration files
- Installation/uninstallation scripts
- Documentation and setup guides

## Deployment Instructions

1. Copy the appropriate package to your production server
2. Follow the DEPLOYMENT.md guide in each package
3. Run the installation scripts as Administrator
4. Verify installation using the provided test procedures

## System Requirements

- Windows Server 2012 R2 or higher
- .NET Framework 4.7.2 or higher
- ODBC Driver 13+ for SQL Server (DataCollectionService only)
- Microsoft Access Database Engine (DataCollectionService only)
- Administrator privileges for installation
- Network connectivity to biometric devices (192.168.2.224-229)

## Build Information

- **Build Configuration:** Release
- **Platform:** x86 (required for COM interop)
- **Build Date:** $(Get-Date)
- **Framework:** .NET Framework 4.7.2

## Support

For deployment assistance, refer to:
- Individual DEPLOYMENT.md files in each package
- Installation scripts with built-in help
- Log files generated during installation

---
**Note:** Both services are designed to work together but can be deployed independently if needed.