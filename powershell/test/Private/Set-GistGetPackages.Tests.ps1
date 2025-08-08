# テスト対象のモジュールをインポート
Import-Module -Name "$PSScriptRoot\..\..\src\GistGet.psd1" -Force

Describe "Set-GistGetPackages Tests" {
    InModuleScope GistGet {
        BeforeAll {
            # モックの設定とパラメータの検証を追加
            Mock Set-GistContent {
                # グローバル変数に結果を保存（検証用）
                $script:lastContent = $Content
            } -Verifiable

            # テスト用の共通GistFileオブジェクト
            $script:testGist = [GistFile]::new("TestGistId", "TestGistFileName")

            # 改行コードを統一する関数
            function NormalizeLineEndings([string]$text) {
                return $text.Replace("`r`n", "`n").TrimEnd()
            }
        }

        AfterEach {
            # 各テスト後にモックが呼び出されたか検証
            Should -InvokeVerifiable
        }

        Context "基本機能テスト" {
            It "単一パッケージを正しく処理できる" {
                # Arrange
                $package = [GistGetPackage]::new("TestPackage")
                $package.Version = "1.0.0"

                # Act
                Set-GistGetPackages -GistFile $testGist -Packages @($package)

                # Assert
                Should -Invoke Set-GistContent -Times 1 -Scope It
                $actual = NormalizeLineEndings $script:lastContent
                $expected = "TestPackage:`n  version: 1.0.0"
                $actual | Should -Be $expected
            }
        }

        Context "YAML形式のテスト" {
            It "すべてのプロパティが設定されたパッケージを正しいYAML形式で出力できる" {
                # Arrange
                $package = [GistGetPackage]::new("FullPackage")
                $package.AllowHashMismatch = $true
                $package.Architecture = "x64"
                $package.Custom = "CustomValue"
                $package.Force = $true
                $package.Header = "HeaderValue"
                $package.InstallerType = "msi"
                $package.Locale = "ja-JP"
                $package.Location = "C:\temp"
                $package.Log = "log.txt"
                $package.Mode = "silent"
                $package.Override = "/silent"
                $package.Scope = "user"
                $package.SkipDependencies = $true
                $package.Version = "1.0.0"
                $package.Confirm = $true
                $package.WhatIf = $true
                $package.Uninstall = $true

                # Act
                Set-GistGetPackages -GistFile $testGist -Packages @($package)

                # Assert
                Should -Invoke Set-GistContent -Times 1 -Scope It
                $expected = @(
                    "FullPackage:",
                    "  allowHashMismatch: true",
                    "  architecture: x64",
                    "  custom: CustomValue",
                    "  force: true",
                    "  header: HeaderValue",
                    "  installerType: msi",
                    "  locale: ja-JP",
                    "  location: C:\temp",
                    "  log: log.txt",
                    "  mode: silent",
                    "  override: /silent",
                    "  scope: user",
                    "  skipDependencies: true",
                    "  version: 1.0.0",
                    "  confirm: true",
                    "  whatIf: true",
                    "  uninstall: true"
                ) -join "`n"
                $actual = NormalizeLineEndings $script:lastContent
                $actual | Should -Be $expected
            }
        }

        Context "複数パッケージの処理テスト" {
            It "複数の空パッケージを正しくソートして処理できる" {
                # Arrange
                $packages = @(
                    [GistGetPackage]::new("ZPackage"),
                    [GistGetPackage]::new("APackage"),
                    [GistGetPackage]::new("MPackage")
                )

                # Act
                Set-GistGetPackages -GistFile $testGist -Packages $packages

                # Assert
                Should -Invoke Set-GistContent -Times 1 -Scope It
                $expected = @(
                    "APackage: ",
                    "MPackage: ",
                    "ZPackage:"
                ) -join "`n"
                $actual = NormalizeLineEndings $script:lastContent
                $actual | Should -Be $expected
            }

            It "プロパティの有無が混在するパッケージを正しく処理できる" {
                # Arrange
                $withVersion = [GistGetPackage]::new("APackage")
                $withVersion.Version = "1.0.0"
                
                $packages = @(
                    [GistGetPackage]::new("ZPackage"),
                    $withVersion,
                    [GistGetPackage]::new("MPackage")
                )

                # Act
                Set-GistGetPackages -GistFile $testGist -Packages $packages

                # Assert
                Should -Invoke Set-GistContent -Times 1 -Scope It
                $expected = @(
                    "APackage:",
                    "  version: 1.0.0",
                    "MPackage: ",
                    "ZPackage:"
                ) -join "`n"
                $actual = NormalizeLineEndings $script:lastContent
                $actual | Should -Be $expected
            }
        }
    }
}