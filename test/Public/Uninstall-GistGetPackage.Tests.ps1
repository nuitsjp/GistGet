# テスト対象のモジュールをインポート
Import-Module -Name "$PSScriptRoot\..\..\src\GistGet.psd1" -Force

InModuleScope GistGet {
    Describe "Uninstall-GistGetPackage インストールあり and Gistあり Tests" {
        BeforeAll {
            # モックの準備
            Mock Get-GistFile {
                return [GistFile]::new("Foo", "Bar")
            }

            Mock Get-WinGetPackage { 
                return @(
                    [PSCustomObject]@{ Id = 'NuitsJp.ClaudeToZenn'}
                )
            }

            Mock Get-GistGetPackage { 
                return @(
                    [GistGetPackage]::new('NuitsJp.ClaudeToZenn')
                )
            }

            Mock Uninstall-WinGetPackage { 
            }

            Mock Set-GistGetPackages {
            }
        }
    
        It "アンインストールされ、Gistからも削除されることを確認" {
            # Arrange: テストパラメータの設定
            $testParams = @{
                Query = "test-query"
                Command = "test-command"
                Count = 1
                Id = "NuitsJp.ClaudeToZenn"
                MatchOption = "Equals"
                Moniker = "test-moniker"
                Name = "test-name"
                Source = "test-source"
                Tag = "test-tag"
                Force = $true
                Mode = "Interactive"
            }

            # Act: 関数を実行
            Uninstall-GistGetPackage @testParams

            # Assert: 結果が期待通りか確認
            Should -Invoke Get-GistGetPackage -ParameterFilter {
                $GistFile -and
                $GistFile.Id -eq "Foo" -and
                $GistFile.FileName -eq "Bar"
            }

            Should -Invoke Get-WinGetPackage -ParameterFilter {
                $Query -eq "test-query" -and
                $Command -eq "test-command" -and
                $Count -eq 1 -and
                $Id -eq "NuitsJp.ClaudeToZenn" -and
                $MatchOption -eq "Equals" -and
                $Moniker -eq "test-moniker" -and
                $Name -eq "test-name" -and
                $Source -eq "test-source" -and
                $Tag -eq "test-tag"
            }

            Should -Invoke Uninstall-WinGetPackage -ParameterFilter {
                $Id -eq "NuitsJp.ClaudeToZenn" -and
                $Force -eq $true -and
                $Mode -eq "Interactive"
            }

            Should -Invoke Set-GistGetPackages -ParameterFilter {
                $GistFile -and
                $GistFile.Id -eq "Foo" -and
                $GistFile.FileName -eq "Bar" -and
                $Packages.Count -eq 1 -and
                $Packages[0].Id -eq "NuitsJp.ClaudeToZenn" -and
                $Packages[0].Uninstall -eq $true
            }
        }

    }

    Describe "Uninstall-GistGetPackage インストールあり and Gistなし Tests" {
        BeforeAll {
            # モックの準備
            Mock Get-WinGetPackage { 
                return @(
                    [PSCustomObject]@{ Id = 'NuitsJp.ClaudeToZenn'}
                )
            }

            Mock Get-GistGetPackage { 
                return @(
                    [GistGetPackage]::new('Foo')
                )
            }

            Mock Uninstall-WinGetPackage { 
            }

            Mock Set-GistGetPackages {
            }
        }
    
        It "すべてのパラメータが正しく渡されることを確認" {
            # Arrange: テストパラメータの設定
            Mock Get-GistFile {
                return [GistFile]::new("Foo", "Bar")
            }

            $testParams = @{
                Id = "NuitsJp.ClaudeToZenn"
            }

            # Act: 関数を実行
            Uninstall-GistGetPackage @testParams

            # Assert: 結果が期待通りか確認
            Should -Invoke Get-GistGetPackage -ParameterFilter {
                $GistFile -and
                $GistFile.Id -eq "Foo" -and
                $GistFile.FileName -eq "Bar"
            }

            Should -Invoke Get-WinGetPackage -ParameterFilter {
                $Id -eq "NuitsJp.ClaudeToZenn"
            }

            Should -Invoke Uninstall-WinGetPackage -ParameterFilter {
                $Id -eq "NuitsJp.ClaudeToZenn"
            }

            Should -Not -Invoke Set-GistGetPackages
        }
    }

    Describe "Uninstall-GistGetPackage インストールなし and Gistあり Tests" {
        BeforeAll {
            # モックの準備
            Mock Get-GistFile {
                return [GistFile]::new("Foo", "Bar")
            }

            Mock Get-WinGetPackage { 
                return @(
                )
            }

            Mock Get-GistGetPackage { 
                return @(
                    [GistGetPackage]::new('NuitsJp.ClaudeToZenn')
                )
            }

            Mock Uninstall-WinGetPackage { 
            }

            Mock Set-GistGetPackages {
            }
        }
    
        It "すべてのパラメータが正しく渡されることを確認" {
            # Arrange: テストパラメータの設定
            $testParams = @{
                Id = "NuitsJp.ClaudeToZenn"
            }

            # Act: 関数を実行
            Uninstall-GistGetPackage @testParams

            # Assert: 結果が期待通りか確認
            Should -Invoke Get-GistGetPackage -ParameterFilter {
                $GistFile -and
                $GistFile.Id -eq "Foo" -and
                $GistFile.FileName -eq "Bar"
            }

            Should -Invoke Get-WinGetPackage -ParameterFilter {
                $Id -eq "NuitsJp.ClaudeToZenn"
            }

            Should -Not -Invoke Uninstall-WinGetPackage

            Should -Invoke Set-GistGetPackages -ParameterFilter {
                $GistFile -and
                $GistFile.Id -eq "Foo" -and
                $GistFile.FileName -eq "Bar" -and
                $Packages.Count -eq 1 -and
                $Packages[0].Id -eq "NuitsJp.ClaudeToZenn" -and
                $Packages[0].Uninstall -eq $true
            }
        }
    }

    Describe "Uninstall-GistGetPackage インストールなし and Gistなし Tests" {
        BeforeAll {
            # モックの準備
            Mock Get-GistFile {
                return [GistFile]::new("Foo", "Bar")
            }

            Mock Get-WinGetPackage { 
                return @(
                )
            }

            Mock Get-GistGetPackage { 
                return @(
                )
            }

            Mock Uninstall-WinGetPackage { 
            }

            Mock Set-GistGetPackages {
            }
        }
    
        It "すべてのパラメータが正しく渡されることを確認" {
            # Arrange: テストパラメータの設定
            $testParams = @{
                Id = "NuitsJp.ClaudeToZenn"
            }

            # Act: 関数を実行
            Uninstall-GistGetPackage @testParams

            # Assert: 結果が期待通りか確認
            Should -Invoke Get-GistGetPackage -ParameterFilter {
                $GistFile -and
                $GistFile.Id -eq "Foo" -and
                $GistFile.FileName -eq "Bar"
            }

            Should -Invoke Get-WinGetPackage -ParameterFilter {
                $Id -eq "NuitsJp.ClaudeToZenn"
            }

            Should -Invoke Uninstall-WinGetPackage -Times 0

            Should -Invoke Set-GistGetPackages -Times 0
        }
    }

}