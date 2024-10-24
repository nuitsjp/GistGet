# テスト対象のモジュールをインポート
Import-Module -Name "$PSScriptRoot\..\..\src\GistGet.psd1" -Force

InModuleScope GistGet {
    Describe "Import-GistGetPackage Installed Tests" {
        BeforeAll {
            # モックの準備
            Mock Get-WinGetPackage { 
                return @(
                    [PSCustomObject]@{ Id = 'NuitsJp.ClaudeToZenn' }
                )
            }
    
            Mock Uninstall-WinGetPackage { 
            }
            Mock Install-WinGetPackage { 
            }
        }
    
        It "インストール -> インストール" {
            # Arrange: テストの準備

            # Act: 関数を実行
            Import-GistGetPackage -Path "$PSScriptRoot\assets\test-install.yaml"

            # Assert: 結果が期待通りか確認
            Should -Invoke Get-WinGetPackage
            Should -Not -Invoke Install-WinGetPackage
            Should -Not -Invoke Uninstall-WinGetPackage
        }

        It "インストール -> アンインストール" {
            # Arrange: テストの準備

            # Act: 関数を実行
            Import-GistGetPackage -Path "$PSScriptRoot\assets\test-uninstall.yaml"

            # Assert: 結果が期待通りか確認
            Should -Invoke Get-WinGetPackage
            Should -Not -Invoke Install-WinGetPackage
            Should -Invoke Uninstall-WinGetPackage -ParameterFilter {
                $Id -eq 'NuitsJp.ClaudeToZenn'
            }
        }
    }

    Describe "Import-GistGetPackage Not Installed Tests" {
        BeforeAll {
            # モックの準備
            Mock Get-WinGetPackage { 
            }
    
            Mock Uninstall-WinGetPackage { 
            }
            Mock Install-WinGetPackage { 
            }
        }

        It "未インストール -> インストール" {
            # Arrange: テストの準備

            # Act: 関数を実行
            Import-GistGetPackage -Path "$PSScriptRoot\assets\test-install.yaml"

            # Assert: 結果が期待通りか確認
            Should -Invoke Get-WinGetPackage
            Should -Invoke Install-WinGetPackage -ParameterFilter {
                $Id -eq 'NuitsJp.ClaudeToZenn'
            }
            Should -Not -Invoke Uninstall-WinGetPackage
        }

        It "未インストール -> アンインストール" {
            # Arrange: テストの準備
            if ((Get-WinGetPackage -Id NuitsJp.ClaudeToZenn).Length -eq 1) {
                Uninstall-WinGetPackage -id NuitsJp.ClaudeToZenn
            }

            # Act: 関数を実行
            Import-GistGetPackage -Path "$PSScriptRoot\assets\test-uninstall.yaml"

            # Assert: 結果が期待通りか確認
            Should -Invoke Get-WinGetPackage
            Should -Not -Invoke Install-WinGetPackage
            Should -Not -Invoke Uninstall-WinGetPackage
        }
    }
}
