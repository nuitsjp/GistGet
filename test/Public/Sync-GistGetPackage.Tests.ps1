# テスト対象のモジュールをインポート
Import-Module -Name "$PSScriptRoot\..\..\src\GistGet.psd1" -Force

InModuleScope GistGet {
    Describe "Sync-GistGetPackage Installed Tests" {
        BeforeAll {
            # モックの準備
            Mock Get-WinGetPackage { 
                return @(
                    [PSCustomObject]@{ Id = 'NuitsJp.ClaudeToZenn' }
                )
            }
    
            Mock Uninstall-WinGetPackage {}
            Mock Install-WinGetPackage {}
            Mock Write-Host {}
        }
    
        It "インストール -> インストール" {
            # Arrange: テストの準備

            # Act: 関数を実行
            Sync-GistGetPackage -Path "$PSScriptRoot\assets\test-install.yaml"

            # Assert: 結果が期待通りか確認
            Should -Invoke Get-WinGetPackage
            Should -Not -Invoke Install-WinGetPackage
            Should -Not -Invoke Uninstall-WinGetPackage
        }

        It "インストール -> アンインストール" {
            # Arrange: テストの準備

            # Act: 関数を実行
            Sync-GistGetPackage -Path "$PSScriptRoot\assets\test-uninstall.yaml"

            # Assert: 結果が期待通りか確認
            Should -Invoke Get-WinGetPackage
            Should -Not -Invoke Install-WinGetPackage
            Should -Invoke Uninstall-WinGetPackage -ParameterFilter {
                $Id -eq 'NuitsJp.ClaudeToZenn'
            }
        }
    }

    Describe "Sync-GistGetPackage Not Installed Tests" {
        BeforeAll {
            # モックの準備
            Mock Get-WinGetPackage {}
            Mock Uninstall-WinGetPackage {}
            Mock Install-WinGetPackage {}
            Mock Write-Host {}
        }

        It "未インストール -> インストール" {
            # Arrange: テストの準備

            # Act: 関数を実行
            Sync-GistGetPackage -Path "$PSScriptRoot\assets\test-install.yaml"

            # Assert: 結果が期待通りか確認
            Should -Invoke Get-WinGetPackage
            Should -Invoke Install-WinGetPackage -ParameterFilter {
                $AllowHashMismatch -eq $true -and
                $Architecture -eq 'x64' -and
                $Custom -eq "custom parameters" -and
                $Force -eq $true -and
                $Id -eq 'NuitsJp.ClaudeToZenn' -and
                $InstallerType -eq 'Exe' -and
                $Locale -eq 'en-US' -and
                $Log -eq 'log.txt' -and
                $Mode -eq 'Silent' -and
                $Override -eq 'override parameters' -and
                $Scope -eq 'User' -and
                $Version -eq '1.0.0' -and
                $Confirm -eq $true -and
                $WhatIf -eq $true
            }
            Should -Not -Invoke Uninstall-WinGetPackage
        }

        It "未インストール -> アンインストール" {
            # Arrange: テストの準備
            if ((Get-WinGetPackage -Id NuitsJp.ClaudeToZenn).Length -eq 1) {
                Uninstall-WinGetPackage -id NuitsJp.ClaudeToZenn
            }

            # Act: 関数を実行
            Sync-GistGetPackage -Path "$PSScriptRoot\assets\test-uninstall.yaml"

            # Assert: 結果が期待通りか確認
            Should -Invoke Get-WinGetPackage
            Should -Not -Invoke Install-WinGetPackage
            Should -Not -Invoke Uninstall-WinGetPackage
        }
    }
}
