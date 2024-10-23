function Import-GistGetPackage {
    [CmdletBinding()]
    param(
        [string] $GistId,
        [string] $GistFileName,
        [string] $Uri,
        [string] $Path
    )

    # Administrator Authority Check
    if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
        Write-Error "This script must be run with administrative privileges; run PowerShell as an administrator and run the script again."
        exit 1
    }

    if (-not $GistId -and -not $Uri -and -not $Path) {
        # Get GistId from environment variable
        Write-Verbose "Getting GistId from environment variable"
        $GistId = Get-GistGetGistId
        Write-Verbose "Environment variable GistId: $GistId"

        # If GistId is not set, get it from user input
        if (-not $GistId) {
            throw "GistId, Uri, or Path, or the GistId must be registered in advance with Set-GistGetGistId."
        }
    }

    $packageParams = @{}
    if ($GistId) { $packageParams['GistId'] = $GistId }
    if ($GistFileName) { $packageParams['GistFileName'] = $GistFileName }
    if ($Uri) { $packageParams['Uri'] = $Uri }
    if ($Path) { $packageParams['Path'] = $Path }

    $packages = Get-GistGetPackages @packageParams

    $packageIds = @{}; Get-WinGetPackage | ForEach-Object { $packageIds[$_.Id] = $true }
    foreach ($package in $packages) {
        $packageId = $package.Id
        if ($package.Uninstall) {
            if ($packageIds.ContainsKey($packageId)) {
                # Uninstall the package if it exists
                Write-Host "Uninstalling package $packageId"
                Uninstall-WinGetPackage -Id $packageId
            } else {
                # Do nothing if the package does not exist
                Write-Verbose "Package $packageId does not exist"
            }
        } else {
            if ($packageIds.ContainsKey($packageId)) {
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
