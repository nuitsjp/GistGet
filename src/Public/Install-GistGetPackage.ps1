function Install-GistGetPackage {
    [CmdletBinding(SupportsShouldProcess = $true, DefaultParameterSetName = 'Query')]
    param(
        [Parameter(Position = 0, ParameterSetName = 'Query')]
        [string[]]$Query,
        
        [Parameter(Position = 0, ParameterSetName = 'PSCatalogPackage', ValueFromPipeline = $true)]
        [Microsoft.WinGet.Client.Engine.PSObjects.PSCatalogPackage]$PSCatalogPackage,
        
        [switch]$AllowHashMismatch,
        [ValidateSet('Default', 'X86', 'Arm', 'X64', 'Arm64')]
        [string]$Architecture,
        [string]$Custom,
        [switch]$Force,
        [string]$Header,
        [Parameter(ParameterSetName = 'Query')]
        [string]$Id,
        [ValidateSet('Default', 'Inno', 'Wix', 'Msi', 'Nullsoft', 'Zip', 'Msix', 'Exe', 'Burn', 'MSStore', 'Portable')]
        [string]$InstallerType,
        [string]$Locale,
        [string]$Location,
        [string]$Log,
        [Parameter(ParameterSetName = 'Query')]
        [ValidateSet('Equals', 'EqualsCaseInsensitive', 'StartsWithCaseInsensitive', 'ContainsCaseInsensitive')]
        [string]$MatchOption,
        [ValidateSet('Default', 'Silent', 'Interactive')]
        [string]$Mode,
        [Parameter(ParameterSetName = 'Query')]
        [string]$Moniker,
        [Parameter(ParameterSetName = 'Query')]
        [string]$Name,
        [string]$Override,
        [ValidateSet('Any', 'User', 'System', 'UserOrUnknown', 'SystemOrUnknown')]
        [string]$Scope,
        [switch]$SkipDependencies,
        [Parameter(ParameterSetName = 'Query')]
        [string]$Source,
        [string]$Version
    )

    begin {
        if (-not (Get-Module -Name Microsoft.WinGet.Client -ListAvailable)) {
            throw "Microsoft.WinGet.Client module is not installed. Please install it first."
        }

        # Find-WinGetPackage用のパラメーター
        $findParameterList = @(
            'Query', 'Command', 'Count', 'Id', 'MatchOption',
            'Moniker', 'Name', 'Source', 'Tag'
        )
        
        # Install-WinGetPackage用のパラメーター
        $installParameterList = @(
            'AllowHashMismatch', 'Architecture', 'Custom', 'Force',
            'Header', 'InstallerType', 'Locale', 'Location', 'Log',
            'Mode', 'Override', 'Scope', 'SkipDependencies', 'Version'
        )
    }

    process {
        try {
            # Gist関連の情報を取得
            $gist = Get-GistFile
            $gistGetPackages = Get-GistGetPackage -Gist $gist

            # Find-WinGetPackageのパラメーターを設定
            $findParams = @{}
            foreach ($param in $findParameterList) {
                if ($PSBoundParameters.ContainsKey($param)) {
                    $findParams[$param] = $PSBoundParameters[$param]
                }
            }

            # Install-WinGetPackageのパラメーターを設定
            $installParams = @{}
            foreach ($param in $installParameterList) {
                if ($PSBoundParameters.ContainsKey($param)) {
                    $installParams[$param] = $PSBoundParameters[$param]
                }
            }

            # パッケージを検索
            $packagesToInstall = Find-WinGetPackage @findParams

            if (-not $packagesToInstall) {
                Write-Warning "No packages found matching the specified criteria."
                return
            }

            # パッケージ情報の表示
            $packageInfo = $packagesToInstall | ForEach-Object {
                "- $($_.Name) ($($_.Id)) Version: $($_.Version)"
            }

            # インストール対象の表示と確認
            if ($packagesToInstall.Count -gt 1) {
                Write-Warning "Multiple packages found:"
                $packageInfo | Write-Warning
            }

            # パッケージ全体に対してShouldProcessを実行
            $actionDescription = if ($packagesToInstall.Count -gt 1) {
                "Install multiple packages ($($packagesToInstall.Count) packages)"
            } else {
                "Install package $($packagesToInstall[0].Name) ($($packagesToInstall[0].Id))"
            }

            if ($PSCmdlet.ShouldProcess($actionDescription, "Install")) {
                # 新しく追加されたパッケージを追跡
                [bool]$isAppendPackage = $false

                # 各パッケージをインストール
                foreach ($package in $packagesToInstall) {
                    Write-Verbose "Installing package: $($package.Name) ($($package.Id)) version $($package.Version)"
                    
                    try {
                        # パッケージIDを設定してインストール
                        Install-WinGetPackage -Id $package.Id @installParams

                        # Gistパッケージリストに含まれていない場合は追加
                        if (-not ($gistGetPackages | Where-Object { $_.id -eq $package.Id })) {
                            $gistGetPackages += [GistGetPackage]::new($package.Id)
                            
                            $isAppendPackage = $true
                            Write-Verbose "Added package $($package.Id) to Gist package list"
                        }
                    }
                    catch {
                        Write-Error "Failed to install package $($package.Name) ($($package.Id)): $_"
                        continue
                    }
                }

                # パッケージリストが更新された場合、Gistを更新
                if ($isAppendPackage) {
                    Write-Verbose "Updating Gist with new package information"
                    Set-GistGetPackages -Gist $gist -Packages $gistGetPackages
                }
            }
        }
        catch {
            Write-Error "An error occurred: $_"
        }
    }
}

Export-ModuleMember -Function Install-GistGetPackage