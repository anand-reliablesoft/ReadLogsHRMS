# Biometric Attendance System

A modern .NET Framework replacement for the legacy VB6 biometric attendance system.

## Project Structure

### TimeSyncService
Windows service that synchronizes time across all biometric devices.

### BiometricAttendance.Common
Shared class library containing:
- **Models**: Data models and entities
- **Interfaces**: Interface definitions for services and components
- **Services**: Shared service implementations
- **Utilities**: Helper classes and utilities

## Requirements

- .NET Framework 4.7.2 or higher
- Visual Studio 2017 or higher
- Windows Server OS
- SBXPC ActiveX control (biometric device SDK)

## Build Instructions

1. Open `BiometricAttendanceSystem.sln` in Visual Studio
2. Restore NuGet packages (if any)
3. Build solution (F6)
4. For COM interop compatibility, use x86 platform target for Release builds

## Configuration

Edit `TimeSyncService/App.config` to configure:
- Log directory path
- Other service-specific settings

## Deployment

See the design document in `.kiro/specs/biometric-attendance-system/design.md` for detailed deployment instructions.
