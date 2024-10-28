# テスト対象のモジュールをインポート
Import-Module -Name "$PSScriptRoot\..\..\src\GistGet.psd1" -Force

InModuleScope GistGet {
    Describe "Set-UserEnvironmentVariable Tests" {
        BeforeEach {
            # 環境変数を削除する
            [System.Environment]::SetEnvironmentVariable("GIST_GET_TEST", $null, [System.EnvironmentVariableTarget]::User)
        }

        It "ファイル名を指定しない場合、最初のファイルを取得する" {
            # Arrange: テストの準備
            $name = "GIST_GET_TEST"
            $value = Get-Date -Format "yyyy-MM-dd HH:mm:ss"

            # Act: 関数を実行
            $result = Set-UserEnvironmentVariable -Name $name -Value $value

            # Assert: 結果が期待通りか確認
            $result = [System.Environment]::GetEnvironmentVariable($name, [System.EnvironmentVariableTarget]::User)
            $result | Should -Be $value
        }

        AfterEach {
            # 環境変数を削除する
            [System.Environment]::SetEnvironmentVariable("GIST_GET_TEST", $null, [System.EnvironmentVariableTarget]::User)
        }
    }
}
