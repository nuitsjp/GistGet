# テスト対象のモジュールをインポート
Import-Module -Name "$PSScriptRoot\..\..\src\GistGet.psd1" -Force

InModuleScope GistGet {
    Describe "Update-GistGetPackage Update, Exists, Exists, NotEqual, True, False, False" {
        BeforeAll {
            # モックの準備
            Mock Get-GistGetPackage {
                $package = [GistGetPackage]::new('NuitsJp.ClaudeToZenn')
                $package.Version = '1.0.0'
                return @(
                    $package
                )
            }

            Mock Get-WinGetPackage { 
                return @(
                    [PSCustomObject]@{ 
                        Id = 'NuitsJp.ClaudeToZenn'
                        IsUpdateAvailable = $true
                        InstalledVersion = '0.9.0'
                    },
                    [PSCustomObject]@{ 
                        Id = 'Foo'
                        IsUpdateAvailable = $false
                        InstalledVersion = '0.9.0'
                    }
                )
            }

            Mock Confirm-ReplacePackage { 
                return $true
            }
    
            Mock Uninstall-WinGetPackage { 
                return [PSCustomObject]@{ RebootRequired = $false }
            }
            Mock Install-WinGetPackage { 
            }
            Mock Update-WinGetPackage { 
            }
            Mock Confirm-Reboot{
            }
            Mock Restart-Computer {
            }
        }

        It "アップデート 
        GistGetPackage:あり 
        GistGetPackage.Version:あり 
        GistGetPackage.Version≠WinGetPackage.InstalledVersion 
        Confirm-ReplacePackage:True 
        Uninstall.RebootRequired:False
        Install.RebootRequired:False" {
            # Arrange: テストの準備

            # Act: 関数を実行
            Update-GistGetPackage

            # Assert: 結果が期待通りか確認
            Should -Invoke Get-GistGetPackage
            Should -Invoke Get-WinGetPackage
            Should -Invoke Confirm-ReplacePackage -ParameterFilter {
                $Id -eq 'NuitsJp.ClaudeToZenn' -and
                $InstalledVersion -eq '0.9.0' -and
                $GistGetVersion -eq '1.0.0'
            }
            Should -Invoke Uninstall-WinGetPackage -ParameterFilter {
                $Id -eq 'NuitsJp.ClaudeToZenn' -and
                $Force -eq $true
            }
            Should -Invoke Install-WinGetPackage -ParameterFilter {
                $Id -eq 'NuitsJp.ClaudeToZenn' -and
                $Version -eq '1.0.0' -and
                $Force -eq $true
            }
            Should -Not -Invoke Update-WinGetPackage
            Should -Not -Invoke Confirm-Reboot
            Should -Not -Invoke Restart-Computer
        }
    }

    Describe "Update-GistGetPackage Update, Exists, Exists, Equal" {
        BeforeAll {
            # モックの準備
            Mock Get-GistGetPackage {
                $package = [GistGetPackage]::new('NuitsJp.ClaudeToZenn')
                $package.Version = '1.0.0'
                return @(
                    $package
                )
            }

            Mock Get-WinGetPackage { 
                return @(
                    [PSCustomObject]@{ 
                        Id = 'NuitsJp.ClaudeToZenn'
                        IsUpdateAvailable = $true
                        InstalledVersion = '1.0.0'
                    },
                    [PSCustomObject]@{ 
                        Id = 'Foo'
                        IsUpdateAvailable = $false
                        InstalledVersion = '0.9.0'
                    }
                )
            }

            Mock Confirm-ReplacePackage { 
                return $true
            }
    
            Mock Uninstall-WinGetPackage { 
                return [PSCustomObject]@{ RebootRequired = $false }
            }
            Mock Install-WinGetPackage { 
            }
            Mock Update-WinGetPackage { 
            }
            Mock Confirm-Reboot{
            }
            Mock Restart-Computer {
            }
        }

        It "アップデート 
        GistGetPackage:あり 
        GistGetPackage.Version:あり 
        GistGetPackage.Version=WinGetPackage.InstalledVersion" {
            # Arrange: テストの準備

            # Act: 関数を実行
            Update-GistGetPackage

            # Assert: 結果が期待通りか確認
            Should -Invoke Get-GistGetPackage
            Should -Invoke Get-WinGetPackage
            Should -Not -Invoke Confirm-ReplacePackage
            Should -Not -Invoke Uninstall-WinGetPackage
            Should -Not -Invoke Install-WinGetPackage
            Should -Not -Invoke Update-WinGetPackage
            Should -Not -Invoke Confirm-Reboot
            Should -Not -Invoke Restart-Computer
        }
    }
}
