# Uninstall Data Collection Service
# This script removes the DataCollectionService Windows Service and scheduled task

param(
    [string]$ServiceName = "DataCollectionService",
    [string]$TaskName = "BiometricDataCollection",
    [switch]$RemoveLogs = $false
)

# Check if running as Administrator
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host "ERROR: This script must be run as Administrator" -ForegroundColor Red
    Write-Host "Right-click PowerShell and select 'Run as Administrator'" -ForegroundColor Yellow
    exit 1
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Data Collection Service Uninstallation" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Stop and remove the service
$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($service) {
    Write-Host "Stopping service '$ServiceName'..." -ForegroundColor Yellow
    Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
    
    Write-Host "Removing service '$ServiceName'..." -ForegroundColor Yellow
    sc.exe delete $ServiceName
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Service removed successfully" -ForegroundColor Green
    } else {
        Write-Host "WARNING: Failed to remove service" -ForegroundColor Yellow
    }
} else {
    Write-Host "Service '$ServiceName' not found (already removed)" -ForegroundColor Gray
}

# Remove scheduled task
$task = Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue
if ($task) {
    Write-Host "Removing scheduled task '$TaskName'..." -ForegroundColor Yellow
    Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false
    Write-Host "Scheduled task removed successfully" -ForegroundColor Green
} else {
    Write-Host "Scheduled task '$TaskName' not found (already removed)" -ForegroundColor Gray
}

# Optionally remove log files
if ($RemoveLogs) {
    $logDir = Join-Path $PSScriptRoot "Logs"
    if (Test-Path $logDir) {
        Write-Host "Removing log directory..." -ForegroundColor Yellow
        Remove-Item -Path $logDir -Recurse -Force
        Write-Host "Log directory removed" -ForegroundColor Green
    }
} else {
    Write-Host ""
    Write-Host "Log files preserved in: $(Join-Path $PSScriptRoot 'Logs')" -ForegroundColor Gray
    Write-Host "To remove logs, run: .\Uninstall-DataCollectionService.ps1 -RemoveLogs" -ForegroundColor Gray
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Uninstallation Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
