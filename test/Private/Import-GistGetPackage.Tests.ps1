# テスト対象のモジュールをインポート
Import-Module -Name "$PSScriptRoot\..\..\src\GistGet.psd1" -Force

InModuleScope GistGet {
    Describe "Import-GistGetPackage Tests" {
        It "未インストール -> インストール" {
            # Arrange: テストの準備
            if ((Get-WinGetPackage -Id NuitsJp.ClaudeToZenn).Length -eq 1) {
                Uninstall-WinGetPackage -id NuitsJp.ClaudeToZenn
            }

            # Act: 関数を実行
            Import-GistGetPackage -Path "$PSScriptRoot\assets\test-install.yaml"

            # Assert: 結果が期待通りか確認
            Get-WinGetPackage -Id NuitsJp.ClaudeToZenn | Should -Not -BeNullOrEmpty
        }
    }

    Describe "Import-GistGetPackage Tests" {
        It "インストール -> インストール" {
            # Arrange: テストの準備
            if ((Get-WinGetPackage -Id NuitsJp.ClaudeToZenn).Length -eq 0) {
                Install-WinGetPackage -id NuitsJp.ClaudeToZenn
            }

            # Act: 関数を実行
            Import-GistGetPackage -Path "$PSScriptRoot\assets\test-install.yaml"

            # Assert: 結果が期待通りか確認
            Get-WinGetPackage -Id NuitsJp.ClaudeToZenn | Should -Not -BeNullOrEmpty
        }
    }

    Describe "Import-GistGetPackage Tests" {
        It "未インストール -> アンインストール" {
            # Arrange: テストの準備
            if ((Get-WinGetPackage -Id NuitsJp.ClaudeToZenn).Length -eq 1) {
                Uninstall-WinGetPackage -id NuitsJp.ClaudeToZenn
            }

            # Act: 関数を実行
            Import-GistGetPackage -Path "$PSScriptRoot\assets\test-uninstall.yaml"

            # Assert: 結果が期待通りか確認
            Get-WinGetPackage -Id NuitsJp.ClaudeToZenn | Should -BeNullOrEmpty
        }
    }

    Describe "Import-GistGetPackage Tests" {
        It "インストール -> アンインストール" {
            # Arrange: テストの準備
            if ((Get-WinGetPackage -Id NuitsJp.ClaudeToZenn).Length -eq 0) {
                Install-WinGetPackage -id NuitsJp.ClaudeToZenn
            }

            # Act: 関数を実行
            Import-GistGetPackage -Path "$PSScriptRoot\assets\test-uninstall.yaml"

            # Assert: 結果が期待通りか確認
            Get-WinGetPackage -Id NuitsJp.ClaudeToZenn | Should -BeNullOrEmpty
        }
    }
}
