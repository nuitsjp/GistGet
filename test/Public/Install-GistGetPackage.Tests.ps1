# テスト対象のモジュールをインポート
Import-Module -Name "$PSScriptRoot\..\..\src\GistGet.psd1" -Force

InModuleScope GistGet {
    Describe "Install-GistGetPackage" {
        BeforeAll {
            # 共通のテストデータを定義
            $script:testPackage = @{
                Id = 'TestPackage.Id'
                Name = 'Test Package'
                Version = '1.0.0'
            }

            # すべてのパラメーターを含むテストパラメーターを定義
            $script:testParams = @{
                # 検索関連パラメーター
                Query = "test-query"
                Id = $testPackage.Id
                MatchOption = "Equals"
                Moniker = "test-moniker"
                Name = "test-name"
                Source = "test-source"

                # インストール関連パラメーター
                AllowHashMismatch = $true
                Architecture = "X64"
                Custom = "/custom-param"
                Force = $true
                Header = "Authorization: Bearer token123"
                InstallerType = "Msi"
                Locale = "ja-JP"
                Location = "C:\CustomInstall"
                Log = "C:\Logs\install.log"
                Mode = "Interactive"
                Override = "/SILENT"
                Scope = "User"
                SkipDependencies = $true
                Version = "2.0.0"
            }

            # PSCatalogPackage名前空間とクラスを定義
            $typeDef = @"
namespace Microsoft.WinGet.Client.Engine.PSObjects
{
    public class PSCatalogPackage
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }

        public PSCatalogPackage() {}
    }
}
"@
            Add-Type -TypeDefinition $typeDef -ErrorAction SilentlyContinue

            # 基本的なモックの設定
            function Set-DefaultMocks {
                Mock Get-GistFile {
                    [GistFile]::new("TestGistId", "packages.json")
                }

                Mock Get-GistGetPackage { 
                    [System.Collections.ArrayList]@()
                }

                Mock Find-WinGetPackage { 
                    [PSCustomObject]$script:testPackage
                }

                Mock Install-WinGetPackage {
                    [PSCustomObject]@{
                        Id = $script:testPackage.Id
                        RebootRequired = $false
                    }
                }

                Mock Set-GistGetPackages { }
                Mock Restart-Computer { }
                Mock Confirm-Reboot { $false }

                # WinGetモジュールの存在確認用モック
                Mock Get-Module { 
                    @{ Name = 'Microsoft.WinGet.Client' } 
                } -ParameterFilter { 
                    $Name -eq 'Microsoft.WinGet.Client' -and $ListAvailable 
                }
            }

            # デフォルトのモックを設定
            Set-DefaultMocks
        }

        BeforeEach {
            # 各テストの前にモックをリセット
            Set-DefaultMocks
        }

        Context "基本機能のテスト" {
            It "WinGetモジュールの存在チェックが行われる" {
                # Act
                Install-GistGetPackage -Id $testPackage.Id -Confirm:$false

                # Assert
                Should -Invoke Get-Module -ParameterFilter {
                    $Name -eq 'Microsoft.WinGet.Client' -and $ListAvailable
                }
            }
        }

        Context "パラメーター渡しのテスト" {
            It "検索関連パラメーターが正しく渡される" {
                # Arrange
                $searchParams = @{
                    Query = $testParams.Query
                    Id = $testParams.Id
                    MatchOption = $testParams.MatchOption
                    Moniker = $testParams.Moniker
                    Name = $testParams.Name
                    Source = $testParams.Source
                }

                # Act
                Install-GistGetPackage @searchParams -Confirm:$false

                # Assert
                Should -Invoke Find-WinGetPackage -ParameterFilter {
                    $Query -eq $testParams.Query -and
                    $Id -eq $testParams.Id -and
                    $MatchOption -eq $testParams.MatchOption -and
                    $Moniker -eq $testParams.Moniker -and
                    $Name -eq $testParams.Name -and
                    $Source -eq $testParams.Source
                }
            }

            It "インストール関連パラメーターが正しく渡される" {
                # Arrange
                $installParams = @{
                    Id = $testParams.Id
                    AllowHashMismatch = $testParams.AllowHashMismatch
                    Architecture = $testParams.Architecture
                    Custom = $testParams.Custom
                    Force = $testParams.Force
                    Header = $testParams.Header
                    InstallerType = $testParams.InstallerType
                    Locale = $testParams.Locale
                    Location = $testParams.Location
                    Log = $testParams.Log
                    Mode = $testParams.Mode
                    Override = $testParams.Override
                    Scope = $testParams.Scope
                    SkipDependencies = $testParams.SkipDependencies
                    Version = $testParams.Version
                }

                # Act
                Install-GistGetPackage @installParams -Confirm:$false

                # Assert
                Should -Invoke Install-WinGetPackage -ParameterFilter {
                    $Id -eq $testParams.Id -and
                    $AllowHashMismatch -eq $testParams.AllowHashMismatch -and
                    $Architecture -eq $testParams.Architecture -and
                    $Custom -eq $testParams.Custom -and
                    $Force -eq $testParams.Force -and
                    $Header -eq $testParams.Header -and
                    $InstallerType -eq $testParams.InstallerType -and
                    $Locale -eq $testParams.Locale -and
                    $Location -eq $testParams.Location -and
                    $Log -eq $testParams.Log -and
                    $Mode -eq $testParams.Mode -and
                    $Override -eq $testParams.Override -and
                    $Scope -eq $testParams.Scope -and
                    $SkipDependencies -eq $testParams.SkipDependencies -and
                    $Version -eq $testParams.Version
                }
            }

            It "パラメーターがGistGetPackagesに正しく保存される" {
                # Act
                Install-GistGetPackage @testParams -Confirm:$false

                # Assert
                Should -Invoke Set-GistGetPackages -ParameterFilter {
                    $Packages[0].Id -eq $testParams.Id -and
                    $Packages[0].AllowHashMismatch -eq $testParams.AllowHashMismatch -and
                    $Packages[0].Architecture -eq $testParams.Architecture -and
                    $Packages[0].Custom -eq $testParams.Custom -and
                    $Packages[0].Force -eq $testParams.Force -and
                    $Packages[0].Header -eq $testParams.Header -and
                    $Packages[0].InstallerType -eq $testParams.InstallerType -and
                    $Packages[0].Locale -eq $testParams.Locale -and
                    $Packages[0].Location -eq $testParams.Location -and
                    $Packages[0].Log -eq $testParams.Log -and
                    $Packages[0].Mode -eq $testParams.Mode -and
                    $Packages[0].Override -eq $testParams.Override -and
                    $Packages[0].Scope -eq $testParams.Scope -and
                    $Packages[0].SkipDependencies -eq $testParams.SkipDependencies -and
                    $Packages[0].Version -eq $testParams.Version
                }
            }

            It "ValidateSetパラメーターに不正な値を指定するとエラーになる" {
                # Architecture
                { Install-GistGetPackage -Id $testPackage.Id -Architecture "Invalid" } |
                    Should -Throw "*Cannot validate argument on parameter 'Architecture'*"

                # InstallerType
                { Install-GistGetPackage -Id $testPackage.Id -InstallerType "Invalid" } |
                    Should -Throw "*Cannot validate argument on parameter 'InstallerType'*"

                # Scope
                { Install-GistGetPackage -Id $testPackage.Id -Scope "Invalid" } |
                    Should -Throw "*Cannot validate argument on parameter 'Scope'*"

                # Mode
                { Install-GistGetPackage -Id $testPackage.Id -Mode "Invalid" } |
                    Should -Throw "*Cannot validate argument on parameter 'Mode'*"

                # MatchOption
                { Install-GistGetPackage -Id $testPackage.Id -MatchOption "Invalid" } |
                    Should -Throw "*Cannot validate argument on parameter 'MatchOption'*"
            }
        }

        Context "パッケージ検索結果による動作テスト" {
            It "パッケージが見つからない場合は警告を表示" {
                # Arrange
                Mock Find-WinGetPackage { $null }

                # Act & Assert
                Install-GistGetPackage -Query "nonexistent" -Confirm:$false -WarningVariable warning
                $warning | Should -Be "No packages found matching the specified criteria."
                Should -Invoke Install-WinGetPackage -Times 0
            }

            It "複数のパッケージが見つかった場合は警告を表示" {
                # Arrange
                Mock Find-WinGetPackage { 
                    @(
                        [PSCustomObject]$testPackage,
                        [PSCustomObject]@{
                            Id = 'Package2'
                            Name = 'Package 2'
                            Version = '1.0.0'
                        }
                    )
                }

                # Act & Assert
                Install-GistGetPackage -Query "multiple" -Confirm:$false -WarningVariable warning
                $warning | Select-Object -First 1 | Should -Be "Multiple packages found:"
            }

            It "Idが指定された場合は完全一致のパッケージのみをインストール" {
                # Arrange
                Mock Find-WinGetPackage { 
                    @(
                        [PSCustomObject]$testPackage,
                        [PSCustomObject]@{
                            Id = 'SimilarPackage.Id'
                            Name = 'Similar Package'
                            Version = '1.0.0'
                        }
                    )
                }

                # Act
                Install-GistGetPackage -Id $testPackage.Id -Confirm:$false

                # Assert
                Should -Invoke Install-WinGetPackage -ParameterFilter {
                    $Id -eq $testPackage.Id
                } -Times 1
            }
        }

        Context "エラーハンドリングテスト" {
            It "WinGetモジュールが存在しない場合はエラーを投げる" {
                # Arrange
                Mock Get-Module { $null } -ParameterFilter { 
                    $Name -eq 'Microsoft.WinGet.Client' -and $ListAvailable 
                }

                # Act & Assert
                { Install-GistGetPackage -Query "test" } | 
                    Should -Throw "Microsoft.WinGet.Client module is not installed*"
            }

            It "インストール失敗時もGistは更新されない" {
                # Arrange
                Mock Install-WinGetPackage { throw "Installation failed" }

                # Act & Assert
                { Install-GistGetPackage -Id $testPackage.Id -Confirm:$false -ErrorAction Stop } | 
                    Should -Throw "*Failed to install package*Installation failed*"
                Should -Invoke Set-GistGetPackages -Times 0
            }
        }

        Context "Gist管理機能のテスト" {
            It "新しいパッケージがGistに追加される" {
                # Act
                Install-GistGetPackage -Id $testPackage.Id -Confirm:$false

                # Assert
                Should -Invoke Set-GistGetPackages -ParameterFilter {
                    $Packages[0].Id -eq $testPackage.Id
                }
            }

            It "既存のパッケージの場合はGistが更新されない" {
                # Arrange
                Mock Get-GistGetPackage {
                    [System.Collections.ArrayList]@(
                        [GistGetPackage]::FromHashtable(@{ Id = $testPackage.Id })
                    )
                }

                # Act
                Install-GistGetPackage -Id $testPackage.Id -Confirm:$false

                # Assert
                Should -Invoke Set-GistGetPackages -Times 0
            }
        }

        Context "再起動要求のテスト" {
            It "再起動が必要なパッケージがある場合、確認プロンプトを表示" {
                # Arrange
                Mock Install-WinGetPackage {
                    [PSCustomObject]@{
                        Id = $testPackage.Id
                        RebootRequired = $true
                    }
                }

                # Act
                Install-GistGetPackage -Id $testPackage.Id -Confirm:$false

                # Assert
                Should -Invoke Confirm-Reboot -ParameterFilter {
                    $PackageIds -contains $testPackage.Id
                }
                Should -Invoke Restart-Computer -Times 0
            }

            It "再起動が必要で確認にYesと答えた場合、再起動を実行" {
                # Arrange
                Mock Install-WinGetPackage {
                    [PSCustomObject]@{
                        Id = $testPackage.Id
                        RebootRequired = $true
                    }
                }
                Mock Confirm-Reboot { $true }

                # Act
                Install-GistGetPackage -Id $testPackage.Id -Confirm:$false

                # Assert
                Should -Invoke Restart-Computer -Times 1 -ParameterFilter {
                    $Force -eq $true
                }
            }

            It "再起動が不要な場合、確認を表示しない" {
                # Act
                Install-GistGetPackage -Id $testPackage.Id -Confirm:$false

                # Assert
                Should -Invoke Confirm-Reboot -Times 0
                Should -Invoke Restart-Computer -Times 0
            }
        }
    }
}