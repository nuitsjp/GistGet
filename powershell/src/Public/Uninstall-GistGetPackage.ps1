function Uninstall-GistGetPackage {
    <#
    .SYNOPSIS
    Uninstalls packages based on specified criteria and updates the GistGet package list.

    .DESCRIPTION
    The Uninstall-GistGetPackage function uninstalls packages that match the specified criteria.
    It also updates the GistGet package list by marking the uninstalled packages.

    .PARAMETER Query
    Specifies the query string to search for packages.

    .PARAMETER Command
    Specifies the command associated with the package.

    .PARAMETER Count
    Specifies the number of packages to return.

    .PARAMETER Id
    Specifies the ID of the package to uninstall.

    .PARAMETER MatchOption
    Specifies the match option for the query. Valid values are 'Equals', 'EqualsCaseInsensitive', 'StartsWithCaseInsensitive', and 'ContainsCaseInsensitive'.

    .PARAMETER Moniker
    Specifies the moniker of the package.

    .PARAMETER Name
    Specifies the name of the package.

    .PARAMETER Source
    Specifies the source of the package.

    .PARAMETER Tag
    Specifies the tag associated with the package.

    .PARAMETER Force
    Forces the uninstallation of the package.

    .PARAMETER Mode
    Specifies the mode of uninstallation. Valid values are 'Default', 'Silent', and 'Interactive'.

    .EXAMPLE
    Uninstall-GistGetPackage -Query "example" -Force

    This command uninstalls packages that match the query "example" and forces the uninstallation.

    .EXAMPLE
    Uninstall-GistGetPackage -Id "packageId" -Mode "Silent"

    This command uninstalls the package with the specified ID in silent mode.

    #>
    [CmdletBinding()]
    param(
        [Parameter(Position = 0, ValueFromPipelineByPropertyName = $true)]
        [string[]]$Query,
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
        $gist = Get-GistFile
        $gistGetPackages = Get-GistGetPackage -Gist $gist

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
            Set-GistGetPackages -Gist $gist -Packages $gistGetPackages
        }
    }

    end {
    }
}