function Install-GistGetPackage {
    <#
    .SYNOPSIS
        Installs WinGet packages.

    .DESCRIPTION
        Searches for and installs WinGet packages based on package ID or query.
        Combines the functionality of Find-WinGetPackage and Install-WinGetPackage.

    .PARAMETER Query
        Specifies one or more strings to search for. Matches against PackageIdentifier, PackageName, Moniker, and Tags.

    .PARAMETER AllowHashMismatch
        Allows download even when SHA256 hash for installer or dependencies doesn't match.

    .PARAMETER Architecture
        Specifies processor architecture for the installer.
        Allowed values: Default, X86, Arm, X64, Arm64

    .PARAMETER Custom
        Passes additional arguments to the installer.

    .PARAMETER Force
        Forces installation by skipping normal checks.

    .PARAMETER Header
        Custom HTTP header value passed to WinGet REST sources.

    .PARAMETER Id
        Specifies the package identifier.

    .PARAMETER InstallerType
        Specifies the type of installer to use.
        Allowed values: Default, Inno, Wix, Msi, Nullsoft, Zip, Msix, Exe, Burn, MSStore, Portable

    .PARAMETER Locale
        Specifies installer locale in BCP47 format (e.g. en-US).

    .PARAMETER Location
        Specifies installation path for the package.

    .PARAMETER Log
        Specifies path for installer log file.

    .PARAMETER MatchOption
        Specifies package search match options.
        Allowed values: Equals, EqualsCaseInsensitive, StartsWithCaseInsensitive, ContainsCaseInsensitive

    .PARAMETER Mode
        Specifies installer execution mode.
        Allowed values: Default, Silent, Interactive

    .PARAMETER Moniker
        Specifies package moniker.

    .PARAMETER Name
        Specifies package name.

    .PARAMETER Override
        Overrides existing arguments passed to installer.

    .PARAMETER Scope
        Specifies installation scope.
        Allowed values: Any, User, System, UserOrUnknown, SystemOrUnknown

    .PARAMETER SkipDependencies
        Skips installation of dependencies.

    .PARAMETER Source
        Specifies WinGet source to install package from.

    .PARAMETER Version
        Specifies package version to install.

    .EXAMPLE
        Install-GistGetPackage -Query "Microsoft PowerShell"
        Searches for and installs PowerShell-related packages.

    .EXAMPLE
        Install-GistGetPackage -Id Microsoft.PowerShell -Version 7.4.0
        Installs specific version of PowerShell.

    .NOTES
        Requires Microsoft.WinGet.Client module to be installed.
    #>
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

            # Idパラメーターが指定されている場合、Idが完全に一致しないパッケージは除外する
            if ($Id) {
                $packagesToInstall = $packagesToInstall | Where-Object { $_.Id -eq $Id }
            }

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


                        # 既存のパッケージを削除
                        $gistGetPackages = @($gistGetPackages | Where-Object { $_.id -ne $package.Id })

                        # 新しいパッケージを作成
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

                        # 新しいパッケージを追加
                        $gistGetPackages = @($gistGetPackages) + @([GistGetPackage]::FromHashtable($filteredPackage))
                        $isAppendPackage = $true

                        Write-Verbose "Updated package $($package.Id) in Gist package list"                    }
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