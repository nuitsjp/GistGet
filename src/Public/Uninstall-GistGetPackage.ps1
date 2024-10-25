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
        [GistGetPackage[]]$gistGetPackages = Get-GistGetPackage @packageParams

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