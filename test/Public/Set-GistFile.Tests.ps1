# テスト対象のモジュールをインポート
Import-Module -Name "$PSScriptRoot\..\..\src\GistGet.psd1" -Force

InModuleScope GistGet {
    Describe "Set-GistFile Tests" {
        BeforeAll {
            # モックの準備
            Mock Set-UserEnvironmentVariable { 
            }
        }

        It "環境変数が存在する場合、正しく取得されること" {
            # Arrange: テストの準備
            $gistFile = [GistFile]::new("Foo", "Bar")

            # Act: 関数を実行
            Set-GistFile -GistFile $gistFile

            # Assert: 結果が期待通りか確認
            Should -Invoke Set-UserEnvironmentVariable -Exactly 2
            Should -Invoke Set-UserEnvironmentVariable -ParameterFilter {
                $Name -eq $global:EnvironmentVariableNameGistId -and
                $Value -eq "Foo"
            }
            Should -Invoke Set-UserEnvironmentVariable -ParameterFilter {
                $Name -eq $global:EnvironmentVariableNameGistFileName -and
                $Value -eq "Bar"
            }
        }
    }
}