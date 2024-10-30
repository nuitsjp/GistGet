function Update-GistGetPackage {
    [CmdletBinding()]
    param(
    )

    [GistGetPackage[]] $gistGetPackages = Get-GistGetPackage

    # インストール済みのパッケージを取得
    Write-Host "Getting installed packages..."
    $updatablePackages = Get-WinGetPackage | Where-Object { $_.IsUpdateAvailable }

    $needRebootPackageIds = @()
    
    foreach ($updatablePackage in $updatablePackages) {
        $updatablePackageId = $updatablePackage.Id
        $installedVersion = $updatablePackage.InstalledVersion
        $gistGetPackage = $gistGetPackages | Where-Object { $_.Id -eq $updatablePackageId }
        
        if ($gistGetPackage) {
            # GistGetPackageにパッケージがある場合、バージョンを比較してアップデートするかどうかを判定
            if ($gistGetPackage.Version) {
                # GistGetPackageにバージョンがある場合、バージョンを比較
                if ($gistGetPackage.Version -eq $installedVersion) {
                    # インストール済みのバージョンとGistGetPackageのバージョンが同じ場合はアップデートしない
                    $needUpdate = $false
                } else {
                    # インストール済みのバージョンとGistGetPackageのバージョンが異なる場合は
                    # 置き換えるかどうかを確認する
                    $replace = Confirm-ReplacePackage -Id $updatablePackageId -InstalledVersion $installedVersion -GistGetVersion $gistGetPackage.Version
                    if ($replace) {
                        # アンインストールしてアップデートする
                        $needUpdate = $false
                        Write-Host "Uninstall package $updatablePackageId"
                        $uninstalled = Uninstall-WinGetPackage -Id $updatablePackageId -Force
                        if ($uninstalled.RebootRequired) {
                            $needRebootPackageIds += $updatablePackageId
                        }

                        Write-Host "Installing package $updatablePackageId"
                        $installed = Install-WinGetPackage -Id $updatablePackageId -Version $gistGetPackage.Version -Force
                        if ($installed.RebootRequired) {
                            # needRebootPackageIdsにすでに追加されている場合は追加しない
                            if ($needRebootPackageIds -notcontains $updatablePackageId) {
                                $needRebootPackageIds += $updatablePackageId
                            }
                        }
                    } else {
                        $needUpdate = $false
                    }
                }
            }
            else {
                # GistGetPackageにバージョンがない場合は、無条件でアップデートする
                $needUpdate = $true
            }
        } else {
            # GistGetPackageにパッケージがない場合は、無条件でアップデートする
            $needUpdate = $true
        }

        if ($needUpdate) {
            Write-Host "Updating package $updatablePackageId"
            $updated = Update-WinGetPackage -Id $updatablePackageId
            if ($updated.RebootRequired) {
                $needRebootPackageIds += $updatablePackageId
            }
        }
    }

    Write-Host

    # $needRebootPackages にリブートが必要なパッケージがある場合、パッケージIDをすべて表示
    if ($needRebootPackageIds.Count -gt 0) {
        # リブートするかどうかを確認
        $reboot = Confirm-Reboot
        if ($reboot) {
            Write-Host "Rebooting..."
            Restart-Computer -Force
        }
    }
}
