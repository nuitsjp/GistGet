function Sync-GistGetPackage {
    [CmdletBinding()]
    param(
        [string] $Uri,
        [string] $Path
    )

    $packageParams = @{}
    if ($Uri) { $packageParams['Uri'] = $Uri }
    if ($Path) { $packageParams['Path'] = $Path }

    $packages = Get-GistGetPackage @packageParams

    # インストール済みのパッケージを取得
    $installedPackageIds = @{}
    Get-WinGetPackage | ForEach-Object { $installedPackageIds[$_.Id] = $true }
    
    foreach ($package in $packages) {
        $packageId = $package.Id
        if ($package.Uninstall) {
            if ($installedPackageIds.ContainsKey($packageId)) {
                # Uninstall the package if it exists
                Write-Host "Uninstalling package $packageId"
                Uninstall-WinGetPackage -Id $packageId
            } else {
                # Do nothing if the package does not exist
                Write-Verbose "Package $packageId does not exist"
            }
        } else {
            if ($installedPackageIds.ContainsKey($packageId)) {
                # Do nothing if the package already exists
                Write-Verbose "Package $packageId already exists"
            } else {
                # Install the package if it does not exist
                Write-Host "Installing package $packageId"
                $findParams = $package.ToHashtable()
        
                Install-WinGetPackage @findParams
            }
        }
    }

}
