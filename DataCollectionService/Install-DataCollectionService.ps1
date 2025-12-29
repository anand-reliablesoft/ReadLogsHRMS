# Install Data Collection Service
# This script installs the DataCollectionService as a Windows Service and creates a scheduled task

param(
    [string]$ServicePath = "$PSScriptRoot\DataCollectionService.exe",
    [string]$ServiceName = "DataCollectionService",
    [string]$DisplayName = "Biometric Data Collection Service",
    [string]$Description = "Collects attendance data from biometric devices and stores in databases",
    [string]$ServiceAccount = "LocalSystem"
)

# Check if running as Administrator
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host "ERROR: This script must be run as Administrator" -ForegroundColor Red
    Write-Host "Right-click PowerShell and select 'Run as Administrator'" -ForegroundColor Yellow
    exit 1
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Data Collection Service Installation" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Verify service executable exists
if (-not (Test-Path $ServicePath)) {
    Write-Host "ERROR: Service executable not found at: $ServicePath" -ForegroundColor Red
    Write-Host "Please build the solution in Release mode first" -ForegroundColor Yellow
    exit 1
}

Write-Host "Service executable found: $ServicePath" -ForegroundColor Green

# Check if service already exists
$existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existingService) {
    Write-Host "WARNING: Service '$ServiceName' already exists" -ForegroundColor Yellow
    $response = Read-Host "Do you want to reinstall? (Y/N)"
    if ($response -ne 'Y' -and $response -ne 'y') {
        Write-Host "Installation cancelled" -ForegroundColor Yellow
        exit 0
    }
    
    Write-Host "Stopping existing service..." -ForegroundColor Yellow
    Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
    
    Write-Host "Removing existing service..." -ForegroundColor Yellow
    sc.exe delete $ServiceName
    Start-Sleep -Seconds 2
}

# Install the service using sc.exe
Write-Host "Installing service..." -ForegroundColor Cyan
$result = sc.exe create $ServiceName binPath= "`"$ServicePath`"" start= demand DisplayName= "$DisplayName"

if ($LASTEXITCODE -eq 0) {
    Write-Host "Service installed successfully" -ForegroundColor Green
    
    # Set service description
    sc.exe description $ServiceName "$Description"
    
    # Configure service recovery options (restart on failure)
    sc.exe failure $ServiceName reset= 86400 actions= restart/60000/restart/60000/restart/60000
    
    Write-Host "Service configured with automatic restart on failure" -ForegroundColor Green
} else {
    Write-Host "ERROR: Failed to install service" -ForegroundColor Red
    exit 1
}

# Create log directory if it doesn't exist
$logDir = Join-Path $PSScriptRoot "Logs"
if (-not (Test-Path $logDir)) {
    New-Item -ItemType Directory -Path $logDir | Out-Null
    Write-Host "Created log directory: $logDir" -ForegroundColor Green
}

# Create scheduled task for batch 1 (machines 1-4)
Write-Host ""
Write-Host "Creating Windows Scheduler task..." -ForegroundColor Cyan

$taskName = "BiometricDataCollection"
$taskDescription = "Collects attendance data from biometric devices every 15 minutes during business hours"

# Check if task already exists
$existingTask = Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
if ($existingTask) {
    Write-Host "Removing existing scheduled task..." -ForegroundColor Yellow
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false
}

# Create task action - run service with parameter "1" for batch 1
$action = New-ScheduledTaskAction -Execute $ServicePath -Argument "1"

# Create trigger - every 15 minutes from 6 AM to 8 PM on weekdays
$trigger = New-ScheduledTaskTrigger -Daily -At "06:00AM"
$trigger.Repetition = New-ScheduledTaskTrigger -Once -At "06:00AM" -RepetitionInterval (New-TimeSpan -Minutes 15) -RepetitionDuration (New-TimeSpan -Hours 14)

# Additional triggers for each weekday
$triggers = @()
$triggers += New-ScheduledTaskTrigger -Weekly -DaysOfWeek Monday,Tuesday,Wednesday,Thursday,Friday -At "06:00AM"

# Create task settings
$settings = New-ScheduledTaskSettingsSet `
    -AllowStartIfOnBatteries `
    -DontStopIfGoingOnBatteries `
    -StartWhenAvailable `
    -RunOnlyIfNetworkAvailable `
    -MultipleInstances IgnoreNew

# Create task principal (run with highest privileges)
$principal = New-ScheduledTaskPrincipal -UserId "SYSTEM" -LogonType ServiceAccount -RunLevel Highest

# Register the scheduled task
try {
    Register-ScheduledTask `
        -TaskName $taskName `
        -Description $taskDescription `
        -Action $action `
        -Trigger $triggers[0] `
        -Settings $settings `
        -Principal $principal `
        -Force | Out-Null
    
    # Configure repetition interval using schtasks (more reliable for 15-minute intervals)
    schtasks /Change /TN $taskName /RI 15 /DU 14:00 /ST 06:00 /ET 20:00 | Out-Null
    
    Write-Host "Scheduled task created successfully" -ForegroundColor Green
    Write-Host "  Task Name: $taskName" -ForegroundColor Gray
    Write-Host "  Schedule: Every 15 minutes from 6:00 AM to 8:00 PM (weekdays)" -ForegroundColor Gray
} catch {
    Write-Host "WARNING: Failed to create scheduled task: $_" -ForegroundColor Yellow
    Write-Host "You can create the task manually using Task Scheduler" -ForegroundColor Yellow
}

# Verify DSN file exists
Write-Host ""
Write-Host "Checking configuration files..." -ForegroundColor Cyan

$dsnFile = Join-Path $PSScriptRoot "ReadLogsHRMS.dsn"
if (-not (Test-Path $dsnFile)) {
    Write-Host "WARNING: DSN file not found: $dsnFile" -ForegroundColor Yellow
    Write-Host "Please configure ReadLogsHRMS.dsn before running the service" -ForegroundColor Yellow
    Write-Host "See DSN-SETUP.md for instructions" -ForegroundColor Yellow
} else {
    Write-Host "DSN file found: $dsnFile" -ForegroundColor Green
}

# Verify Access database exists
$accessDbPath = Join-Path $PSScriptRoot "RCMSBio.mdb"
if (-not (Test-Path $accessDbPath)) {
    Write-Host "WARNING: Access database not found: $accessDbPath" -ForegroundColor Yellow
    Write-Host "Please copy RCMSBio.mdb to the service directory" -ForegroundColor Yellow
} else {
    Write-Host "Access database found: $accessDbPath" -ForegroundColor Green
}

# Summary
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Installation Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "1. Configure ReadLogsHRMS.dsn with your SQL Server connection details" -ForegroundColor White
Write-Host "   See DSN-SETUP.md for instructions" -ForegroundColor Gray
Write-Host ""
Write-Host "2. Ensure RCMSBio.mdb is in the service directory" -ForegroundColor White
Write-Host ""
Write-Host "3. Test the service manually:" -ForegroundColor White
Write-Host "   $ServicePath 1" -ForegroundColor Gray
Write-Host ""
Write-Host "4. The scheduled task will run automatically every 15 minutes" -ForegroundColor White
Write-Host "   during business hours (6 AM - 8 PM, weekdays)" -ForegroundColor Gray
Write-Host ""
Write-Host "5. Check logs in: $logDir" -ForegroundColor White
Write-Host ""
Write-Host "To uninstall, run: .\Uninstall-DataCollectionService.ps1" -ForegroundColor Yellow
Write-Host ""
