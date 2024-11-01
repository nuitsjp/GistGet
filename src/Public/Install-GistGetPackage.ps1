function Install-GistGetPackage {
    [CmdletBinding(SupportsShouldProcess = $true, DefaultParameterSetName = 'Query')]
    param(
        [Parameter(Position = 0, ParameterSetName = 'Query')]
        [string[]]$Query,
        
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

            # 再起動が必要なパッケージを追跡するための配列
            $packagesNeedingReboot = [System.Collections.ArrayList]@()

            if ($PSCmdlet.ShouldProcess($actionDescription, "Install")) {
                # 新しく追加されたパッケージを追跡
                [bool]$isAppendPackage = $false

                # 各パッケージをインストール
                foreach ($package in $packagesToInstall) {
                    Write-Verbose "Installing package: $($package.Name) ($($package.Id)) version $($package.Version)"
                    
                    try {
                        # パッケージをインストールして結果を取得
                        $installResult = Install-WinGetPackage -Id $package.Id @installParams

                        # 再起動が必要な場合はリストに追加
                        if ($installResult.RebootRequired) {
                            [void]$packagesNeedingReboot.Add($package.Id)
                        }


                        # Gistパッケージリストに含まれていない場合は追加
                        if (-not ($gistGetPackages | Where-Object { $_.id -eq $package.Id })) {
                            # Install-GistGetPackageの該当部分
                            $gistGetPackage = @{
                                id = $package.Id
                                allowHashMismatch = $installParams.ContainsKey('AllowHashMismatch') -and $installParams['AllowHashMismatch']
                                architecture = $installParams['Architecture']
                                custom = $installParams['Custom']
                                force = $installParams.ContainsKey('Force') -and $installParams['Force']
                                header = $installParams['Header']
                                installerType = $installParams['InstallerType']
                                locale = $installParams['Locale']
                                location = $installParams['Location']
                                log = $installParams['Log']
                                mode = $installParams['Mode']
                                override = $installParams['Override']
                                scope = $installParams['Scope']
                                skipDependencies = $installParams.ContainsKey('SkipDependencies') -and $installParams['SkipDependencies']
                                version = $installParams['Version']
                            }

                            # nullの値を持つキーを削除
                            $filteredPackage = @{}
                            foreach ($key in $gistGetPackage.Keys) {
                                if ($null -ne $gistGetPackage[$key]) {
                                    $filteredPackage[$key] = $gistGetPackage[$key]
                                }
                            }

                            $gistGetPackages += [GistGetPackage]::FromHashtable($filteredPackage)

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

                # 再起動が必要なパッケージがある場合、確認して再起動
                if ($packagesNeedingReboot.Count -gt 0) {
                    if (Confirm-Reboot -PackageIds $packagesNeedingReboot) {
                        Restart-Computer -Force
                    }
                }
            }
        }
        catch {
            Write-Error "An error occurred: $_"
        }
    }
}

Export-ModuleMember -Function Install-GistGetPackage