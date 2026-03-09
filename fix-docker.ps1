# Fix Docker Desktop: Enable required Windows features
# Run as Administrator

Write-Host "Enabling Virtual Machine Platform..." -ForegroundColor Cyan
Enable-WindowsOptionalFeature -Online -FeatureName VirtualMachinePlatform -All -NoRestart

Write-Host "Enabling Containers feature (for HNS networking)..." -ForegroundColor Cyan
Enable-WindowsOptionalFeature -Online -FeatureName Containers -All -NoRestart

Write-Host ""
Write-Host "Done! Please restart your computer for changes to take effect." -ForegroundColor Green
Write-Host "After restart, Docker Desktop should start normally." -ForegroundColor Green
Read-Host "Press Enter to exit"
