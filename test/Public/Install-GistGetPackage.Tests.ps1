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

            $script:testParams = @{
                Query = "test-query"
                Id = $testPackage.Id
                MatchOption = "Equals"
                Moniker = "test-moniker"
                Name = "test-name"
                Source = "test-source"
                Force = $true
                Mode = "Interactive"
                SkipDependencies = $true
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

        It "すべてのパラメータが正しく渡されることを確認" {
            # Act
            Install-GistGetPackage @testParams -Confirm:$false

            # Assert
            Should -Invoke Find-WinGetPackage -ParameterFilter {
                $Query -eq $testParams.Query -and
                $Id -eq $testParams.Id -and
                $MatchOption -eq $testParams.MatchOption -and
                $Moniker -eq $testParams.Moniker -and
                $Name -eq $testParams.Name -and
                $Source -eq $testParams.Source
            }

            Should -Invoke Install-WinGetPackage -ParameterFilter {
                $Id -eq $testParams.Id -and
                $Force -eq $testParams.Force -and
                $Mode -eq $testParams.Mode -and
                $SkipDependencies -eq $testParams.SkipDependencies
            }

            Should -Invoke Set-GistGetPackages -ParameterFilter {
                $Packages.Count -eq 1 -and
                $Packages[0].Id -eq $testParams.Id -and
                $Packages[0].Force -eq $testParams.Force -and
                $Packages[0].Mode -eq $testParams.Mode -and
                $Packages[0].SkipDependencies -eq $testParams.SkipDependencies
            }
        }

        Context "パッケージ検索結果による動作テスト" {
            It "パッケージが見つからない場合は警告を表示" {
                # Arrange
                Mock Find-WinGetPackage { $null }

                # Act & Assert
                Install-GistGetPackage -Query "nonexistent" -Confirm:$false -WarningVariable warning
                $warning | Should -Be "No packages found matching the specified criteria."
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
                Install-GistGetPackage -Query "multiple" -Confirm:$false -WarningVariable warning -ErrorAction SilentlyContinue
                $warning | Select-Object -First 1 | Should -Be "Multiple packages found:"
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
                    $Packages.Count -eq 1 -and
                    $Packages[0].Id -eq $testPackage.Id
                }
            }
        }

        Context "パラメーターバリデーションテスト" {
            It "不正なArchitectureを指定するとエラーになる" {
                # Act & Assert
                { Install-GistGetPackage -Query "test" -Architecture "Invalid" -Confirm:$false } |
                    Should -Throw "*Cannot validate argument on parameter 'Architecture'*"
            }

            It "不正なMatchOptionを指定するとエラーになる" {
                # Act & Assert
                { Install-GistGetPackage -Query "test" -MatchOption "Invalid" -Confirm:$false } |
                    Should -Throw "*Cannot validate argument on parameter 'MatchOption'*"
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
                Install-GistGetPackage -Query "test" -Confirm:$false

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
                Install-GistGetPackage -Query "test" -Confirm:$false

                # Assert
                Should -Invoke Confirm-Reboot -Times 1
                Should -Invoke Restart-Computer -Times 1 -ParameterFilter {
                    $Force -eq $true
                }
            }

            It "再起動が不要な場合、確認を表示しない" {
                # Act
                Install-GistGetPackage -Query "test" -Confirm:$false

                # Assert
                Should -Invoke Confirm-Reboot -Times 0
                Should -Invoke Restart-Computer -Times 0
            }
        }
    }
}