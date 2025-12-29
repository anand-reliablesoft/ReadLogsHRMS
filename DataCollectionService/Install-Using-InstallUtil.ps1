# Install Data Collection Service using InstallUtil
# Alternative installation method using .NET Framework InstallUtil.exe

param(
    [string]$ServicePath = "$PSScriptRoot\DataCollectionService.exe"
)

# Check if running as Administrator
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host "ERROR: This script must be run as Administrator" -ForegroundColor Red
    exit 1
}

Write-Host "Installing Data Collection Service using InstallUtil..." -ForegroundColor Cyan

# Find InstallUtil.exe
$installUtil = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\InstallUtil.exe"
if (-not (Test-Path $installUtil)) {
    $installUtil = "C:\Windows\Microsoft.NET\Framework\v4.0.30319\InstallUtil.exe"
}

if (-not (Test-Path $installUtil)) {
    Write-Host "ERROR: InstallUtil.exe not found" -ForegroundColor Red
    Write-Host "Please ensure .NET Framework 4.x is installed" -ForegroundColor Yellow
    exit 1
}

# Verify service executable exists
if (-not (Test-Path $ServicePath)) {
    Write-Host "ERROR: Service executable not found at: $ServicePath" -ForegroundColor Red
    exit 1
}

# Install the service
Write-Host "Running InstallUtil..." -ForegroundColor Yellow
& $installUtil $ServicePath

if ($LASTEXITCODE -eq 0) {
    Write-Host "Service installed successfully" -ForegroundColor Green
    Write-Host ""
    Write-Host "To start the service:" -ForegroundColor Cyan
    Write-Host "  net start DataCollectionService" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Or use the main installation script to also create scheduled tasks:" -ForegroundColor Cyan
    Write-Host "  .\Install-DataCollectionService.ps1" -ForegroundColor Gray
} else {
    Write-Host "ERROR: Installation failed" -ForegroundColor Red
    exit 1
}
