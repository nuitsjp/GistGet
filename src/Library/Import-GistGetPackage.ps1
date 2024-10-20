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

    Import-Module -Name PowerShellForGitHub
    Import-Module -Name powershell-yaml

    if ($GistId -eq $null -and $Uri -eq $null) {
        Write-Error "GistId or Uri must be specified"
        exit 1
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
        $packages = $gist.files.$fileName.content | ConvertFrom-Yaml
    }
    if($Uri)
    {
        # Get file contents
        $packages = Invoke-RestMethod -Uri $Uri | ConvertFrom-Yaml
        $packages
    }

    $packageIds = @{}; Get-WinGetPackage | ForEach-Object { $packageIds[$_.Id] = $true }
    foreach ($package in $packages) {
        $packageId = $package.Id
        if ($packageIds.ContainsKey($packageId)) {
            Write-Verbose "Package $packageId already exists"
        } else {
            Write-Host "Installing package $packageId"
            Install-WinGetPackage -Id $packageId
        }
    }

}
