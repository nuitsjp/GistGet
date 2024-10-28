# テスト対象のモジュールをインポート
Import-Module -Name "$PSScriptRoot\..\..\src\GistGet.psd1" -Force

InModuleScope GistGet {
    Describe "Get-GistFile exist environment variables Tests" {
        BeforeAll {
            # モックの準備
            Mock Get-UserEnvironmentVariable { 
                param($Name)
                if ($Name -eq $global:EnvironmentVariableNameGistId) {
                    return 'Foo'
                }
                elseif ($Name -eq $global:EnvironmentVariableNameGistFileName) {
                    return 'Bar'
                }

                throw
            }
        }

        It "環境変数が存在する場合、正しく取得されること" {
            # Arrange: テストの準備

            # Act: 関数を実行
            $result = Get-GistFile

            # Assert: 結果が期待通りか確認
            $result | Should -Not -Be $null
            $result.Id | Should -Be 'Foo'
            $result.FileName | Should -Be 'Bar'
        }
    }

    Describe "Find one Gist file from description" {
        BeforeAll {
            # モックの準備
            Mock Get-GistDescription { 
                return 'GistGet Test'
            }

            Mock Get-UserEnvironmentVariable { 
                return $null
            }

            Mock Get-GitHubGist {
                $result = @(
                    [PSCustomObject]@{
                        Description = "GistGet Test"
                        id = "82b27147c684502b4f9c4488f9fe6fa9"
                        files = [PSCustomObject]@{
                            'GistGet.yaml' = @{}
                        }
                    },
                    [PSCustomObject]@{
                        Description = "GistGet"
                    }
                )
                return $result
            }
        }

        It "Gistの概要から1つだけGistが取得できた場合" {
            # Arrange: テストの準備

            # Act: 関数を実行
            $result = Get-GistFile

            # Assert: 結果が期待通りか確認
            $result | Should -Not -Be $null
            $result.Id | Should -Be '82b27147c684502b4f9c4488f9fe6fa9'
            $result.FileName | Should -Be 'GistGet.yaml'
        }
    }

    Describe "Find two Gist file from description" {
        BeforeAll {
            # モックの準備
            Mock Get-GistDescription { 
                return 'GistGet Test'
            }

            Mock Get-UserEnvironmentVariable { 
                return $null
            }

            Mock Get-GitHubGist {
                $result = @(
                    [PSCustomObject]@{
                        Description = "GistGet Test"
                        id = "82b27147c684502b4f9c4488f9fe6fa9"
                        files = [PSCustomObject]@{
                            'GistGet.yaml' = @{}
                            'Second.yaml' = @{}
                        }
                    },
                    [PSCustomObject]@{
                        Description = "GistGet"
                    }
                )
                return $result
            }
        }

        It "Gistの概要から1つだけGistが取得できた場合" {
            # Arrange: テストの準備

            # Act: 関数を実行
            { Get-GistFile } | Should -Throw

            # Assert: 結果が期待通りか確認
        }
    }

    Describe "Find two Gist from description" {
        BeforeAll {
            # モックの準備
            Mock Get-GistDescription { 
                return 'GistGet Test'
            }

            Mock Get-UserEnvironmentVariable { 
                return $null
            }

            Mock Get-GitHubGist {
                $result = @(
                    [PSCustomObject]@{
                        Description = "GistGet Test"
                        id = "82b27147c684502b4f9c4488f9fe6fa9"
                        files = [PSCustomObject]@{
                            'GistGet.yaml' = @{}
                            'Second.yaml' = @{}
                        }
                    },
                    [PSCustomObject]@{
                        Description = "GistGet Test"
                    }
                )
                return $result
            }
        }

        It "Gistの概要から1つだけGistが取得できた場合" {
            # Arrange: テストの準備

            # Act: 関数を実行
            { Get-GistFile } | Should -Throw

            # Assert: 結果が期待通りか確認
        }
    }

}