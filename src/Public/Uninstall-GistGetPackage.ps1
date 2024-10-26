<#
.SYNOPSIS
    Uninstalls packages and updates GistGet tracking information.

.DESCRIPTION
    This function uninstalls packages using WinGet and updates the GistGet tracking information.
    It performs two main tasks:
    1. Uninstalls specified packages from the system using WinGet
    2. Updates the GistGet configuration to mark these packages for uninstallation

    If no matching packages are found installed on the system but an ID is specified,
    it will still update the GistGet configuration to mark those packages for uninstallation.

.PARAMETER Query
    Specifies search terms to find packages. Can accept multiple values as a string array.
    
.PARAMETER GistId
    The ID of the Gist containing package information.

.PARAMETER GistFileName
    The name of the file within the Gist containing package information.

.PARAMETER Command
    Specifies a command to search for in packages.

.PARAMETER Count
    Specifies the maximum number of results to return.

.PARAMETER Id
    Specifies the exact package identifier to search for.

.PARAMETER MatchOption
    Specifies the matching algorithm to use when searching for packages.
    Valid values: 'Equals', 'EqualsCaseInsensitive', 'StartsWithCaseInsensitive', 'ContainsCaseInsensitive'

.PARAMETER Moniker
    Specifies the moniker to search for in packages.

.PARAMETER Name
    Specifies the name to search for in packages.

.PARAMETER Source
    Specifies the source to search for packages.

.PARAMETER Tag
    Specifies the tag to search for in packages.

.PARAMETER Force
    Forces the uninstallation of packages without prompting for confirmation.

.PARAMETER Mode
    Specifies the uninstallation mode.
    Valid values: 'Default', 'Silent', 'Interactive'

.EXAMPLE
    PS> Uninstall-GistGetPackage -Id "Microsoft.VisualStudioCode"
    Uninstalls VS Code and updates the GistGet configuration to mark it for uninstallation.

.EXAMPLE
    PS> Uninstall-GistGetPackage -Query "vscode" -Force -Mode Silent
    Silently uninstalls packages matching "vscode" without confirmation and updates GistGet configuration.

.NOTES
    Behavior details:
    - If a package is found installed, it will be uninstalled and marked in GistGet
    - If no matching installed package is found but ID is specified, only GistGet configuration is updated
    - Changes to GistGet configuration are saved back to the Gist
    
    Requirements:
    - WinGet must be installed and accessible
    - GitHub authentication must be configured for GistGet operations
    - Appropriate permissions for package uninstallation

.INPUTS
    System.String[]
    System.String
    System.UInt32
    System.Switch

.OUTPUTS
    None

.LINK
    Get-WinGetPackage
    Uninstall-WinGetPackage
    Get-GistGetPackage
    Set-GistGetPackages
#>
function Uninstall-GistGetPackage {
    [CmdletBinding()]
    param(
        [Parameter(Position = 0, ValueFromPipelineByPropertyName = $true)]
        [string[]]$Query,
        [Parameter(ValueFromPipelineByPropertyName = $true)]
        [string] $GistId,
        [Parameter(ValueFromPipelineByPropertyName = $true)]
        [string] $GistFileName,
        [Parameter(ValueFromPipelineByPropertyName = $true)]
        [string]$Command,
        [Parameter(ValueFromPipelineByPropertyName = $true)]
        [uint32]$Count,
        [Parameter(ValueFromPipelineByPropertyName = $true)]
        [string]$Id,
        [Parameter(ValueFromPipelineByPropertyName = $true)]
        [ValidateSet('Equals', 'EqualsCaseInsensitive', 'StartsWithCaseInsensitive', 'ContainsCaseInsensitive')]
        [string]$MatchOption,
        [Parameter(ValueFromPipelineByPropertyName = $true)]
        [string]$Moniker,
        [Parameter(ValueFromPipelineByPropertyName = $true)]
        [string]$Name,
        [Parameter(ValueFromPipelineByPropertyName = $true)]
        [string]$Source,
        [Parameter(ValueFromPipelineByPropertyName = $true)]
        [string]$Tag,
        [Parameter()]
        [switch]$Force,
        [Parameter()]
        [ValidateSet('Default', 'Silent', 'Interactive')]
        [string]$Mode
    )

    begin {
    }

    process {
        $packageParams = @{}
        if ($GistId) { $packageParams['GistId'] = $GistId }
        if ($GistFileName) { $packageParams['GistFileName'] = $GistFileName }
        $gistGetPackages = Get-GistGetPackage @packageParams

        # Build parameter hashtable for Find-WinGetPackage
        $getParams = @{}
        $parameterList = @(
            'Query', 'Command', 'Count', 'Id', 'MatchOption',
            'Moniker', 'Name', 'Source', 'Tag'
        )

        foreach ($param in $parameterList) {
            if ($PSBoundParameters.ContainsKey($param)) {
                $getParams[$param] = $PSBoundParameters[$param]
            }
        }

        # Find packages
        $packagesToUninstall = Get-WinGetPackage @getParams
            
        # Build parameter hashtable for Install-WinGetPackage
        $uninstallParams = @{}

        if ($Force) {
            $uninstallParams['Force'] = $true
        }

        if ($Mode) {
            $uninstallParams['Mode'] = $Mode
        }

        # Install packages
        [bool]$isRemovedPackage = $false
        foreach ($package in $packagesToUninstall) {
            Uninstall-WinGetPackage -Id $package.Id @uninstallParams
            # $gistGetPackagesに含まれていた場合は削除
            $installedPackage = $gistGetPackages | 
                Where-Object { $_.Id -eq $package.Id } | 
                Select-Object -First 1
            if ($installedPackage) {
                Write-Host "Uninstalling $($installedPackage.Id) from Gist."
                $installedPackage.uninstall = $true
                $isRemovedPackage = $true
            }
        }

        if(-not $packagesToUninstall) {
            Write-Warning "No packages were found matching the specified criteria."
            
            if ($Id) {
                $gistGetPackages = $gistGetPackages | Where-Object { $_.Id -eq $Id }
            }
            foreach ($package in $gistGetPackages) {
                Write-Host "Uninstalling $($package.Id) from Gist."
                $package.Uninstall = $true
                $isRemovedPackage = $true
            }
        }

        if ($isRemovedPackage) {
            Set-GistGetPackages @packageParams -Packages $gistGetPackages
        }
    }

    end {
    }
}