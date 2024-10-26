<#
.SYNOPSIS
    Installs packages using WinGet and manages them through GistGet.

.DESCRIPTION
    This function combines WinGet package installation with GistGet package management. It searches for packages using specified criteria, 
    installs them through WinGet, and automatically adds them to GistGet package tracking if they're not already present.

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
    Forces the installation of packages without prompting for confirmation.

.PARAMETER Mode
    Specifies the installation mode.
    Valid values: 'Default', 'Silent', 'Interactive'

.EXAMPLE
    PS> Install-GistGetPackage -Query "vscode" -Force
    Searches for and installs VS Code using WinGet, and adds it to GistGet tracking if not already present.

.EXAMPLE
    PS> Install-GistGetPackage -Id "Microsoft.VisualStudioCode" -Mode Silent
    Silently installs VS Code using its exact package ID and adds it to GistGet tracking if not already present.

.NOTES
    - This function requires both WinGet and GistGet to be properly configured
    - Package installations are performed using WinGet
    - Package tracking is managed through GistGet and stored in GitHub Gists
    - The function automatically updates the GistGet package list when new packages are installed

.INPUTS
    System.String[]
    System.String
    System.UInt32
    System.Switch

.OUTPUTS
    None

.LINK
    Find-WinGetPackage
    Install-WinGetPackage
    Get-GistGetPackage
    Set-GistGetPackages
#>
function Install-GistGetPackage {
    [CmdletBinding()]
    param(
        [Parameter(Position = 0, ValueFromPipelineByPropertyName = $true)]
        [string[]]$Query,
        [Parameter(ValueFromPipelineByPropertyName = $true)]
        [string] $Id,
        [Parameter(ValueFromPipelineByPropertyName = $true)]
        [string] $Name,
        [Parameter(ValueFromPipelineByPropertyName = $true)]
        [string] $Source,
        [Parameter(ValueFromPipelineByPropertyName = $true)]
        [string] $Moniker,
        [Parameter(ValueFromPipelineByPropertyName = $true)]
        [ValidateSet('Equals', 'EqualsCaseInsensitive', 'StartsWithCaseInsensitive', 'ContainsCaseInsensitive')]
        [string] $MatchOption,
        [Parameter(ValueFromPipelineByPropertyName = $true)]
        [string] $Command,
        [Parameter(ValueFromPipelineByPropertyName = $true)]
        [uint32] $Count,
        [Parameter(ValueFromPipelineByPropertyName = $true)]
        [string] $Tag,
        [Parameter(ValueFromPipelineByPropertyName = $true)]
        [string] $GistId,
        [Parameter(ValueFromPipelineByPropertyName = $true)]
        [string] $GistFileName,
        [Parameter()]
        [switch] $Force,
        [Parameter()]
        [ValidateSet('Default', 'Silent', 'Interactive')]
        [string] $Mode
    )

    begin {
    }

    process {
        $packageParams = @{}
        if ($GistId) { $packageParams['GistId'] = $GistId }
        if ($GistFileName) { $packageParams['GistFileName'] = $GistFileName }
        $gistGetPackages = Get-GistGetPackage @packageParams

        # Build parameter hashtable for Find-WinGetPackage
        $findParams = @{}
        $parameterList = @(
            'Query', 'Command', 'Count', 'Id', 'MatchOption',
            'Moniker', 'Name', 'Source', 'Tag'
        )

        foreach ($param in $parameterList) {
            if ($PSBoundParameters.ContainsKey($param)) {
                $findParams[$param] = $PSBoundParameters[$param]
            }
        }

        # Find packages
        $packagesToInstall = Find-WinGetPackage @findParams
            
        if (-not $packagesToInstall) {
            Write-Warning "No packages were found matching the specified criteria."
            return
        }

        # Build parameter hashtable for Install-WinGetPackage
        $installParams = @{}

        if ($Force) {
            $installParams['Force'] = $true
        }

        if ($Mode) {
            $installParams['Mode'] = $Mode
        }

        # Install packages
        [bool]$isAppendPackage = $false
        foreach ($package in $packagesToInstall) {
            Install-WinGetPackage -Id $package.Id @installParams
            # $gistGetPackagesに含まれていなかった場合は追加
            if (-not ($gistGetPackages | Where-Object { $_.id -eq $package.Id })) {
                $gistGetPackages += [PSCustomObject]@{id = $package.Id}
                $isAppendPackage = $true
            }
        }

        if ($isAppendPackage) {
            Set-GistGetPackages @packageParams -Packages $gistGetPackages
        }
    }

    end {
    }
}