# Install-TimeSyncService.ps1
# PowerShell script to install and configure the Time Sync Service

# Requires Administrator privileges
#Requires -RunAsAdministrator

param(
    [string]$ServicePath = "$PSScriptRoot\bin\Release\TimeSyncService.exe",
    [string]$SbxpcPath = "$PSScriptRoot\SBXPC.ocx"
)

Write-Host "=== Time Sync Service Installation ===" -ForegroundColor Cyan
Write-Host ""

# Step 1: Register SBXPC ActiveX Control
Write-Host "Step 1: Registering SBXPC ActiveX control..." -ForegroundColor Yellow
if (Test-Path $SbxpcPath) {
    try {
        $regsvr32 = "regsvr32.exe"
        $arguments = "/s `"$SbxpcPath`""
        Start-Process -FilePath $regsvr32 -ArgumentList $arguments -Wait -NoNewWindow
        Write-Host "  [SUCCESS] SBXPC ActiveX control registered" -ForegroundColor Green
    }
    catch {
        Write-Host "  [ERROR] Failed to register SBXPC control: $_" -ForegroundColor Red
        exit 1
    }
}
else {
    Write-Host "  [WARNING] SBXPC.ocx not found at $SbxpcPath" -ForegroundColor Yellow
    Write-Host "  Please register the SBXPC ActiveX control manually using:" -ForegroundColor Yellow
    Write-Host "  regsvr32 `"path\to\SBXPC.ocx`"" -ForegroundColor Yellow
}

Write-Host ""

# Step 2: Check if service executable exists
Write-Host "Step 2: Checking service executable..." -ForegroundColor Yellow
if (-not (Test-Path $ServicePath)) {
    Write-Host "  [ERROR] Service executable not found at: $ServicePath" -ForegroundColor Red
    Write-Host "  Please build the solution in Release mode first" -ForegroundColor Red
    exit 1
}
Write-Host "  [SUCCESS] Service executable found" -ForegroundColor Green
Write-Host ""

# Step 3: Stop and remove existing service if it exists
Write-Host "Step 3: Checking for existing service..." -ForegroundColor Yellow
$existingService = Get-Service -Name "TimeSyncService" -ErrorAction SilentlyContinue
if ($existingService) {
    Write-Host "  Existing service found. Stopping and removing..." -ForegroundColor Yellow
    
    if ($existingService.Status -eq 'Running') {
        Stop-Service -Name "TimeSyncService" -Force
        Start-Sleep -Seconds 2
    }
    
    sc.exe delete "TimeSyncService"
    Start-Sleep -Seconds 2
    Write-Host "  [SUCCESS] Existing service removed" -ForegroundColor Green
}
else {
    Write-Host "  No existing service found" -ForegroundColor Gray
}
Write-Host ""

# Step 4: Install the service using sc.exe
Write-Host "Step 4: Installing TimeSyncService..." -ForegroundColor Yellow
try {
    $serviceName = "TimeSyncService"
    $displayName = "Biometric Time Sync Service"
    $description = "Synchronizes time across biometric attendance devices"
    
    # Create the service
    sc.exe create $serviceName binPath= "`"$ServicePath`"" start= demand DisplayName= "$displayName"
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  [SUCCESS] Service installed successfully" -ForegroundColor Green
        
        # Set service description
        sc.exe description $serviceName "$description"
        
        # Configure service to run as LocalSystem (default)
        Write-Host "  Service configured to run as LocalSystem account" -ForegroundColor Gray
    }
    else {
        Write-Host "  [ERROR] Failed to install service (Exit code: $LASTEXITCODE)" -ForegroundColor Red
        exit 1
    }
}
catch {
    Write-Host "  [ERROR] Failed to install service: $_" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Step 5: Create log directory
Write-Host "Step 5: Creating log directory..." -ForegroundColor Yellow
$logDir = Join-Path (Split-Path $ServicePath -Parent) "TLogs"
if (-not (Test-Path $logDir)) {
    New-Item -ItemType Directory -Path $logDir -Force | Out-Null
    Write-Host "  [SUCCESS] Log directory created at: $logDir" -ForegroundColor Green
}
else {
    Write-Host "  Log directory already exists at: $logDir" -ForegroundColor Gray
}
Write-Host ""

# Step 6: Create Windows Scheduler task
Write-Host "Step 6: Creating Windows Scheduler task..." -ForegroundColor Yellow
try {
    $taskName = "TimeSyncService_Daily"
    
    # Remove existing task if it exists
    $existingTask = Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
    if ($existingTask) {
        Unregister-ScheduledTask -TaskName $taskName -Confirm:$false
        Write-Host "  Removed existing scheduled task" -ForegroundColor Gray
    }
    
    # Create task action
    $action = New-ScheduledTaskAction -Execute $ServicePath
    
    # Create task trigger (daily at 2:00 AM)
    $trigger = New-ScheduledTaskTrigger -Daily -At "2:00AM"
    
    # Create task principal (run with highest privileges)
    $principal = New-ScheduledTaskPrincipal -UserId "SYSTEM" -LogonType ServiceAccount -RunLevel Highest
    
    # Create task settings
    $settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable
    
    # Register the task
    Register-ScheduledTask -TaskName $taskName -Action $action -Trigger $trigger -Principal $principal -Settings $settings -Description "Daily time synchronization for biometric devices" | Out-Null
    
    Write-Host "  [SUCCESS] Scheduled task created: $taskName" -ForegroundColor Green
    Write-Host "  Task will run daily at 2:00 AM" -ForegroundColor Gray
}
catch {
    Write-Host "  [ERROR] Failed to create scheduled task: $_" -ForegroundColor Red
    Write-Host "  You can create the task manually using Task Scheduler" -ForegroundColor Yellow
}
Write-Host ""

# Summary
Write-Host "=== Installation Complete ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Service Name: TimeSyncService" -ForegroundColor White
Write-Host "Service Path: $ServicePath" -ForegroundColor White
Write-Host "Log Directory: $logDir" -ForegroundColor White
Write-Host "Scheduled Task: TimeSyncService_Daily (runs daily at 2:00 AM)" -ForegroundColor White
Write-Host ""
Write-Host "To start the service manually, run:" -ForegroundColor Yellow
Write-Host "  sc.exe start TimeSyncService" -ForegroundColor Gray
Write-Host ""
Write-Host "To run the scheduled task immediately for testing, run:" -ForegroundColor Yellow
Write-Host "  Start-ScheduledTask -TaskName TimeSyncService_Daily" -ForegroundColor Gray
Write-Host ""
