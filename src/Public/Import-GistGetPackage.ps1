function Import-GistGetPackage {
    [CmdletBinding()]
    param(
        [string] $GistId,
        [string] $GistFileName,
        [string] $Uri,
        [string] $Path
    )

    # Administrator Authority Check
    # if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    #     Write-Error "This script must be run with administrative privileges; run PowerShell as an administrator and run the script again."
    #     exit 1
    # }

    $packageParams = @{}
    if ($GistId) { $packageParams['GistId'] = $GistId }
    if ($GistFileName) { $packageParams['GistFileName'] = $GistFileName }
    if ($Uri) { $packageParams['Uri'] = $Uri }
    if ($Path) { $packageParams['Path'] = $Path }

    $packages = Get-GistGetPackages @packageParams

    # インストール済みのパッケージを取得
    $installedPackageIds = @{}; Get-WinGetPackage | ForEach-Object { $installedPackageIds[$_.Id] = $true }
    
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
                Install-WinGetPackage -Id $packageId
            }
        }
    }

}
