# テスト対象のモジュールをインポート
Import-Module -Name "$PSScriptRoot\..\..\src\GistGet.psd1" -Force

InModuleScope GistGet {
    Describe "Install-GistGetPackage Not Installed Tests" {
        BeforeAll {
            # モックの準備
            Mock Find-Gist {
                return [GistFile]::new("Foo", "Bar")
            }

            Mock Get-GistGetPackage { 
                return @()
            }

            Mock Find-WinGetPackage { 
                return @(
                    [PSCustomObject]@{ Id = 'NuitsJp.ClaudeToZenn' }
                )
            }

            Mock Install-WinGetPackage { 
            }

            Mock Set-GistGetPackages {
            }
        }
    
        It "すべてのパラメータが正しく渡されることを確認" {
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
            Install-GistGetPackage @testParams

            # Assert: 結果が期待通りか確認
            Should -Invoke Get-GistGetPackage -ParameterFilter {
                $Gist -and
                $Gist.Id -eq "Foo" -and
                $Gist.FileName -eq "Bar"
            }

            Should -Invoke Find-WinGetPackage -ParameterFilter {
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

            Should -Invoke Install-WinGetPackage -ParameterFilter {
                $Id -eq "NuitsJp.ClaudeToZenn" -and
                $Force -eq $true -and
                $Mode -eq "Interactive"
            }

            Should -Invoke Set-GistGetPackages -ParameterFilter {
                $Gist -and
                $Gist.Id -eq "Foo" -and
                $Gist.FileName -eq "Bar" -and
                $Packages.Count -eq 1 -and
                $Packages[0].Id -eq "NuitsJp.ClaudeToZenn"
            }
        }
    }
}