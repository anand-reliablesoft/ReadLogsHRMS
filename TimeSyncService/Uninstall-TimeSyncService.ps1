# Uninstall-TimeSyncService.ps1
# PowerShell script to uninstall the Time Sync Service

# Requires Administrator privileges
#Requires -RunAsAdministrator

Write-Host "=== Time Sync Service Uninstallation ===" -ForegroundColor Cyan
Write-Host ""

# Step 1: Remove Windows Scheduler task
Write-Host "Step 1: Removing scheduled task..." -ForegroundColor Yellow
$taskName = "TimeSyncService_Daily"
$existingTask = Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
if ($existingTask) {
    try {
        Unregister-ScheduledTask -TaskName $taskName -Confirm:$false
        Write-Host "  [SUCCESS] Scheduled task removed" -ForegroundColor Green
    }
    catch {
        Write-Host "  [ERROR] Failed to remove scheduled task: $_" -ForegroundColor Red
    }
}
else {
    Write-Host "  No scheduled task found" -ForegroundColor Gray
}
Write-Host ""

# Step 2: Stop and remove the service
Write-Host "Step 2: Removing TimeSyncService..." -ForegroundColor Yellow
$existingService = Get-Service -Name "TimeSyncService" -ErrorAction SilentlyContinue
if ($existingService) {
    try {
        # Stop the service if running
        if ($existingService.Status -eq 'Running') {
            Write-Host "  Stopping service..." -ForegroundColor Yellow
            Stop-Service -Name "TimeSyncService" -Force
            Start-Sleep -Seconds 2
        }
        
        # Delete the service
        sc.exe delete "TimeSyncService"
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  [SUCCESS] Service removed successfully" -ForegroundColor Green
        }
        else {
            Write-Host "  [ERROR] Failed to remove service (Exit code: $LASTEXITCODE)" -ForegroundColor Red
        }
    }
    catch {
        Write-Host "  [ERROR] Failed to remove service: $_" -ForegroundColor Red
    }
}
else {
    Write-Host "  No service found" -ForegroundColor Gray
}
Write-Host ""

# Step 3: Optionally unregister SBXPC ActiveX control
Write-Host "Step 3: SBXPC ActiveX control..." -ForegroundColor Yellow
Write-Host "  [INFO] The SBXPC ActiveX control has NOT been unregistered" -ForegroundColor Gray
Write-Host "  If you want to unregister it, run manually:" -ForegroundColor Yellow
Write-Host "  regsvr32 /u `"path\to\SBXPC.ocx`"" -ForegroundColor Gray
Write-Host ""

# Step 4: Log directory cleanup
Write-Host "Step 4: Log directory..." -ForegroundColor Yellow
Write-Host "  [INFO] Log files have NOT been deleted" -ForegroundColor Gray
Write-Host "  Log directory location: TLogs\" -ForegroundColor Gray
Write-Host "  Delete manually if needed" -ForegroundColor Gray
Write-Host ""

# Summary
Write-Host "=== Uninstallation Complete ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "The TimeSyncService has been removed from your system." -ForegroundColor White
Write-Host ""
