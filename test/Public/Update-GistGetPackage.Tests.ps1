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
                return [PSCustomObject]@{ RebootRequired = $false }
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

    Describe "Update-GistGetPackage Update, Exists, Exists, NotEqual, True, True, False, True" {
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
                return [PSCustomObject]@{ RebootRequired = $true }
            }
            Mock Install-WinGetPackage { 
                return [PSCustomObject]@{ RebootRequired = $false }
            }
            Mock Update-WinGetPackage { 
            }
            Mock Confirm-Reboot{
                return $true
            }
            Mock Restart-Computer {
            }
        }

        It "アップデート 
        GistGetPackage:あり 
        GistGetPackage.Version:あり 
        GistGetPackage.Version≠WinGetPackage.InstalledVersion 
        Confirm-ReplacePackage:True 
        Uninstall.RebootRequired:True
        Install.RebootRequired:False
        Confirm-Reboot:True" {
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
            Should -Invoke Confirm-Reboot
            Should -Invoke Restart-Computer
        }
    }

    Describe "Update-GistGetPackage Update, Exists, Exists, NotEqual, True, False, True, True" {
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
                return [PSCustomObject]@{ RebootRequired = $true }
            }
            Mock Update-WinGetPackage { 
            }
            Mock Confirm-Reboot{
                return $true
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
        Install.RebootRequired:True
        Confirm-Reboot:True" {
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
            Should -Invoke Confirm-Reboot
            Should -Invoke Restart-Computer
        }
    }

    Describe "Update-GistGetPackage Update, Exists, Exists, NotEqual, False" {
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
                return $false
            }
    
            Mock Uninstall-WinGetPackage {}
            Mock Install-WinGetPackage {}
            Mock Update-WinGetPackage {}
            Mock Confirm-Reboot {}
            Mock Restart-Computer {}
        }

        It "アップデート 
        GistGetPackage:あり 
        GistGetPackage.Version:あり 
        GistGetPackage.Version≠WinGetPackage.InstalledVersion 
        Confirm-ReplacePackage:False" {
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
            Should -Not -Invoke Uninstall-WinGetPackage
            Should -Not -Invoke Install-WinGetPackage
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

    Describe "Update-GistGetPackage Update, Exists, NotExists, False" {
        BeforeAll {
            # モックの準備
            Mock Get-GistGetPackage {
                $package = [GistGetPackage]::new('NuitsJp.ClaudeToZenn')
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

            Mock Confirm-ReplacePackage {}
    
            Mock Uninstall-WinGetPackage {}
            Mock Install-WinGetPackage {}
            Mock Update-WinGetPackage {
                return [PSCustomObject]@{ RebootRequired = $false }
            }
            Mock Confirm-Reboot {}
            Mock Restart-Computer {}
        }

        It "アップデート 
        GistGetPackage:あり 
        GistGetPackage.Version:なし 
        Update.RebootRequired:False" {
            # Arrange: テストの準備

            # Act: 関数を実行
            Update-GistGetPackage

            # Assert: 結果が期待通りか確認
            Should -Invoke Get-GistGetPackage
            Should -Invoke Get-WinGetPackage
            Should -Not -Invoke Confirm-ReplacePackage
            Should -Not -Invoke Uninstall-WinGetPackage
            Should -Not -Invoke Install-WinGetPackage
            Should -Invoke Update-WinGetPackage -ParameterFilter {
                $Id -eq 'NuitsJp.ClaudeToZenn'
            }
            Should -Not -Invoke Confirm-Reboot
            Should -Not -Invoke Restart-Computer
        }
    }


    Describe "Update-GistGetPackage Update, Exists, NotExists, True, False" {
        BeforeAll {
            # モックの準備
            Mock Get-GistGetPackage {
                $package = [GistGetPackage]::new('NuitsJp.ClaudeToZenn')
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

            Mock Confirm-ReplacePackage {}
    
            Mock Uninstall-WinGetPackage {}
            Mock Install-WinGetPackage {}
            Mock Update-WinGetPackage {
                return [PSCustomObject]@{ RebootRequired = $true }
            }
            Mock Confirm-Reboot {
                return $false
            }
            Mock Restart-Computer {}
        }

        It "アップデート 
        GistGetPackage:あり 
        GistGetPackage.Version:なし 
        Update.RebootRequired:True
        Confirm-Reboot:False" {
            # Arrange: テストの準備

            # Act: 関数を実行
            Update-GistGetPackage

            # Assert: 結果が期待通りか確認
            Should -Invoke Get-GistGetPackage
            Should -Invoke Get-WinGetPackage
            Should -Not -Invoke Confirm-ReplacePackage
            Should -Not -Invoke Uninstall-WinGetPackage
            Should -Not -Invoke Install-WinGetPackage
            Should -Invoke Update-WinGetPackage -ParameterFilter {
                $Id -eq 'NuitsJp.ClaudeToZenn'
            }
            Should -Invoke Confirm-Reboot -ParameterFilter {
                $PackageIds -eq @('NuitsJp.ClaudeToZenn')
            }
            Should -Not -Invoke Restart-Computer
        }
    }
}
