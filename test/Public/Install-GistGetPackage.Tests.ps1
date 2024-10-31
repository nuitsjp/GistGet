# テスト対象のモジュールをインポート
Import-Module -Name "$PSScriptRoot\..\..\src\GistGet.psd1" -Force

InModuleScope GistGet {
    Describe "Install-GistGetPackage" {
        BeforeAll {
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

            # 基本的なモックの準備
            Mock Get-GistFile {
                [GistFile]::new("TestGistId", "packages.json")
            }

            Mock Get-GistGetPackage { 
                @()
            }

            Mock Find-WinGetPackage { 
                @(
                    [PSCustomObject]@{
                        Id = 'TestPackage.Id'
                        Name = 'Test Package'
                        Version = '1.0.0'
                    }
                )
            }

            Mock Install-WinGetPackage { }
            Mock Set-GistGetPackages { }
            
            # WinGetモジュールの存在確認用モック
            Mock Get-Module { 
                @{ Name = 'Microsoft.WinGet.Client' } 
            } -ParameterFilter { 
                $Name -eq 'Microsoft.WinGet.Client' -and $ListAvailable 
            }
        }

        It "すべてのパラメータが正しく渡されることを確認" {
            # Arrange
            $testParams = @{
                Query = "test-query"
                Id = "TestPackage.Id"
                MatchOption = "Equals"
                Moniker = "test-moniker"
                Name = "test-name"
                Source = "test-source"
                Force = $true
                Mode = "Interactive"
                SkipDependencies = $true
            }
    
            # GistGetPackageのモックデータ
            Mock Get-GistGetPackage {
                [System.Collections.ArrayList]@()
            }
    
            # Act
            Install-GistGetPackage @testParams -Confirm:$false
    
            # Assert
            Should -Invoke Find-WinGetPackage -ParameterFilter {
                $Query -eq "test-query" -and
                $Id -eq "TestPackage.Id" -and
                $MatchOption -eq "Equals" -and
                $Moniker -eq "test-moniker" -and
                $Name -eq "test-name" -and
                $Source -eq "test-source"
            }
    
            Should -Invoke Install-WinGetPackage -ParameterFilter {
                $Id -eq "TestPackage.Id" -and
                $Force -eq $true -and
                $Mode -eq "Interactive" -and
                $SkipDependencies -eq $true
            }
    
            Should -Invoke Set-GistGetPackages -ParameterFilter {
                $Packages.Count -eq 1 -and
                $Packages[0].Id -eq "TestPackage.Id" -and
                $Packages[0].Force -eq $true -and
                $Packages[0].Mode -eq "Interactive" -and
                $Packages[0].SkipDependencies -eq $true
            }
        }
    
        It "PSCatalogPackageパラメーターセットが機能する" {
            # Arrange
            Mock Get-GistGetPackage {
                [System.Collections.ArrayList]@()
            }
    
            $mockPackage = New-Object Microsoft.WinGet.Client.Engine.PSObjects.PSCatalogPackage
            $mockPackage.Id = "TestPackage.Id"
            $mockPackage.Name = "Test Package"
            $mockPackage.Version = "1.0.0"
    
            # Act
            Install-GistGetPackage -PSCatalogPackage $mockPackage -Force -SkipDependencies -Confirm:$false
    
            # Assert
            Should -Invoke Install-WinGetPackage -ParameterFilter {
                $Id -eq "TestPackage.Id" -and
                $Force -eq $true -and
                $SkipDependencies -eq $true
            }
    
            Should -Invoke Set-GistGetPackages -ParameterFilter {
                $Packages.Count -eq 1 -and
                $Packages[0].Id -eq "TestPackage.Id" -and
                $Packages[0].Force -eq $true -and
                $Packages[0].SkipDependencies -eq $true
            }
        }

        Context "パッケージ検索結果による動作テスト" {
            It "パッケージが見つからない場合は警告を表示" {
                # Arrange
                Mock Find-WinGetPackage { return $null }

                # Act
                $warning = $null
                Install-GistGetPackage -Query "nonexistent" -Confirm:$false -WarningVariable warning

                # Assert
                $warning | Should -Be "No packages found matching the specified criteria."
            }

            It "複数のパッケージが見つかった場合は警告を表示" {
                # Arrange
                Mock Find-WinGetPackage { 
                    @(
                        [PSCustomObject]@{ Id = 'Package1'; Name = 'Package 1'; Version = '1.0.0' },
                        [PSCustomObject]@{ Id = 'Package2'; Name = 'Package 2'; Version = '1.0.0' }
                    )
                }

                Mock Get-GistGetPackage {
                    [System.Collections.ArrayList]@()
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

                # Cleanup - モックを元に戻す
                Mock Get-Module { 
                    @{ Name = 'Microsoft.WinGet.Client' } 
                } -ParameterFilter { 
                    $Name -eq 'Microsoft.WinGet.Client' -and $ListAvailable 
                }
            }

            It "インストール失敗時もGistは更新されない" {
                # Arrange
                Mock Install-WinGetPackage { throw "Installation failed" }

                # Act & Assert
                { Install-GistGetPackage -Id "TestPackage.Id" -Confirm:$false -ErrorAction Stop } | 
                    Should -Throw "*Failed to install package*Installation failed*"
                Should -Invoke Set-GistGetPackages -Times 0
            }
        }

        Context "Gist管理機能のテスト" {
            It "新しいパッケージがGistに追加される" {
                # Arrange
                Mock Get-GistGetPackage { [System.Collections.ArrayList]@() }
                Mock Install-WinGetPackage { }

                # Act
                Install-GistGetPackage -Id "TestPackage.Id" -Confirm:$false

                # Assert
                Should -Invoke Set-GistGetPackages -ParameterFilter {
                    $Packages.Count -eq 1 -and
                    $Packages[0].Id -eq "TestPackage.Id"
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
    }
}
