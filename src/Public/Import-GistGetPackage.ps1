function Import-GistGetPackage {
    [CmdletBinding()]
    param(
        [string] $GistId,
        [string] $GistFileName,
        [string] $Uri
    )

    # Administrator Authority Check
    if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
        Write-Error "This script must be run with administrative privileges; run PowerShell as an administrator and run the script again."
        exit 1
    }

    if (-not $GistId -and -not $Uri) {
        # Get GistId from environment variable
        Write-Verbose "Getting GistId from environment variable"
        . $PSScriptRoot\Get-GistGetGistId.ps1
        $GistId = Get-GistGetGistId
        Write-Verbose "Environment variable GistId: $GistId"

        # If GistId is not set, get it from user input
        if (-not $GistId) {
            $GistId = Read-Host "Enter GistId"
            if (-not $GistId) {
                Write-Error "GistId or Uri must be specified"
                exit 1
            }

            # Set GistId to environment variable
            . $PSScriptRoot\Set-GistGetGistId.ps1
            Set-GistGetGistId -GistId $GistId
        }
    }

    if ($GistId)
    {
        # Get Gist information
        Write-Verbose "Getting Gist for $GistId"
        $gist = Get-GitHubGist -Gist $GistId

        $fileName = $GistFileName
        if (-not $fileName) {
            # Get the first file if GistFileName is not specified
            $fileName = $gist.files.PSObject.Properties.Name | Select-Object -First 1
        }

        # Get file contents
        $yaml = $gist.files.$fileName.content
    }
    if($Uri)
    {
        # Get file contents
        Write-Verbose "Getting Gist from $Uri"
        $yaml = Invoke-RestMethod -Uri $Uri
    }

    $packages = ConvertTo-GistGetPackageFromYaml -yaml $yaml

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
