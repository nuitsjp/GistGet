function Install-GistGetPackage {
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
            if (-not ($gistGetPackages | Where-Object { $_.Id -eq $package.Id })) {
                $gistGetPackages += [GistGetPackage]::new($package.Id, "", $false)
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