function Confirm-ReplacePackage {
    param(
        [string]$Id,
        [string]$InstalledVersion,
        [string]$GistGetVersion
    )

    Write-Host "Version mismatch: $Id installed version is $InstalledVersion, but GistGet version is $($GistGetVersion)" -ForegroundColor Yellow
    Write-Host "Do you want to replace it? (y/n): " -ForegroundColor Yellow -NoNewline
    $replace = Read-Host
    return [bool]($replace.ToLower() -eq "y")
}