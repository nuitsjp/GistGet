function Sync-GistGetPackage {
    <#
    .SYNOPSIS
        Synchronizes WinGet packages with GistGet package configuration.

    .DESCRIPTION
        By default, synchronizes packages using the first found Gist with "GistGet" in its description.
        Optionally, you can specify a custom Gist URL or local file path.

    .PARAMETER Uri
        Optional: Specifies a custom Gist URL containing the package configuration.

    .PARAMETER Path
        Optional: Specifies a custom local file path to the package configuration.

    .EXAMPLE
        Sync-GistGetPackage
        Uses the default Gist with "GistGet" in its description.

    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$false)]
        [string] $Uri,
        [Parameter(Mandatory=$false)]
        [string] $Path
    )

    # Get package configuration
    $packageParams = @{}
    
    # Optional: Use custom URI or Path
    if ($Uri) { 
        $packageParams['Uri'] = $Uri 
    }
    elseif ($Path) { 
        $packageParams['Path'] = $Path 
    }
    # Default behavior: Use Gist with "GistGet" in description
    else {
        $packageParams['GistFile'] = Get-GistFile
    }

    $packages = Get-GistGetPackage @packageParams

    # インストール済みのパッケージを取得
    Write-Host "Getting installed packages..."
    $installedPackageIds = @{}
    Get-WinGetPackage | ForEach-Object { $installedPackageIds[$_.Id] = $true }

    $installs = @()
    $uninstalls = @()
    $needRebootPackages = @()
    
    foreach ($package in $packages) {
        $packageId = $package.Id
        if ($package.Uninstall) {
            if ($installedPackageIds.ContainsKey($packageId)) {
                # Uninstall the package if it exists
                Write-Host "Uninstalling package $packageId"
                $uninstalled = Uninstall-WinGetPackage -Id $packageId
                $uninstalls += $packageId
                if ($uninstalled.RebootRequired) {
                    $needRebootPackages += $packageId
                }
            } else {
                Write-Host "Package $packageId is not installed"
            }
        } else {
            if ($installedPackageIds.ContainsKey($packageId)) {
                # Do nothing if the package already exists
                Write-Host "Package $packageId already installed"
            } else {
                # Install the package if it does not exist
                Write-Host "Installing package $packageId"
                $findParams = $package.ToHashtable()
        
                $installed += Install-WinGetPackage @findParams
                $installs += $packageId
                if ($installed.RebootRequired) {
                    $needRebootPackages += $packageId
                }
            }
        }
    }

    # $installedPackageIds にインストールしたパッケージがある場合、パッケージIDをすべて表示
    if ($installs.Count -gt 0) {
        Write-Host
        Write-Host "Installed the following packages:" -ForegroundColor Cyan
        $installs | ForEach-Object { Write-Host " - $_" -ForegroundColor Cyan }
    }

    # $uninstalledPackageIds にアンインストールしたパッケージがある場合、パッケージIDをすべて表示
    if ($uninstalls.Count -gt 0) {
        Write-Host
        Write-Host "Uninstalled the following packages:" -ForegroundColor Cyan
        $uninstalls | ForEach-Object { Write-Host " - $_" -ForegroundColor Cyan }
    }

    Write-Host

    # $needRebootPackages にリブートが必要なパッケージがある場合、パッケージIDをすべて表示
    if ($needRebootPackages.Count -gt 0) {
        Write-Host "Reboot is required for the following packages:" -ForegroundColor Red
        $needRebootPackages | ForEach-Object { Write-Host $_ -ForegroundColor Red }

        # リブートするかどうかを確認
        $reboot = Read-Host "Do you want to reboot now? (y/n)" -ForegroundColor Red
        if ($reboot -eq "y") {
            Write-Host "Rebooting..."
            Restart-Computer -Force
        }
    }
}
