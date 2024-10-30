function Confirm-Reboot {
    param(
        [string[]]$PackageIds
    )

    Write-Host "Reboot is required for the following packages:" -ForegroundColor Red
    $PackageIds | ForEach-Object { Write-Host $_ -ForegroundColor Red }

    Write-Host "Do you want to reboot now? (y/n): " -ForegroundColor Yellow -NoNewline
    $reboot = Read-Host
    return [bool]($reboot.ToLower() -eq "y")
}