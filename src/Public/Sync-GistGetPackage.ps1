<#
.SYNOPSIS
    Synchronizes installed packages with GistGet package configuration.

.DESCRIPTION
    This function synchronizes the system's installed packages with a GistGet package configuration. 
    It performs the following operations:
    - Installs packages that are in the configuration but not installed
    - Uninstalls packages marked for removal in the configuration
    - Skips packages that are already in the desired state
    The configuration can be loaded from a GitHub Gist, URL, or local file.

.PARAMETER GistId
    The ID of the Gist containing the package configuration.

.PARAMETER GistFileName
    The specific filename within the Gist to use for configuration.

.PARAMETER Uri
    The URL from which to retrieve the package configuration.

.PARAMETER Path
    The local file path containing the package configuration.

.EXAMPLE
    PS> Sync-GistGetPackage
    Synchronizes packages using the GistId from environment variables.

.EXAMPLE
    PS> Sync-GistGetPackage -GistId "1234567890abcdef" -GistFileName "packages.yaml"
    Synchronizes packages using configuration from a specific Gist file.

.EXAMPLE
    PS> Sync-GistGetPackage -Path "C:\packages.yaml"
    Synchronizes packages using configuration from a local file.

.NOTES
    Package synchronization behavior:
    - New packages: Installed if not present
    - Existing packages: Left unchanged
    - Packages marked for uninstall: Removed if present
    
    Requirements:
    - WinGet must be installed and accessible
    - Appropriate permissions for package installation/uninstallation
    - Valid package configuration in YAML format

.OUTPUTS
    None. Progress information is displayed through Write-Host and Write-Verbose.

.LINK
    Get-GistGetPackage
    Install-WinGetPackage
    Uninstall-WinGetPackage
#>
function Sync-GistGetPackage {
    [CmdletBinding()]
    param(
        [string] $GistId,
        [string] $GistFileName,
        [string] $Uri,
        [string] $Path
    )

    $packageParams = @{}
    if ($GistId) { $packageParams['GistId'] = $GistId }
    if ($GistFileName) { $packageParams['GistFileName'] = $GistFileName }
    if ($Uri) { $packageParams['Uri'] = $Uri }
    if ($Path) { $packageParams['Path'] = $Path }

    $packages = Get-GistGetPackage @packageParams

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

                $findParams = @{}
                $parameterList = @(
                    'AllowHashMismatch',
                    'Architecture',
                    'Custom',
                    'Force',
                    'InstallerType',
                    'Locale',
                    'Log',
                    'Mode',
                    'Id', 
                    'Override',
                    'Scope',
                    'Version',
                    'Confirm',
                    'WhatIf'
                )
        
                foreach ($param in $parameterList) {
                    if ($package.ContainsKey($param)) {
                        $findParams[$param] = $package[$param]
                    }
                }
        
                Install-WinGetPackage @findParams
            }
        }
    }

}
