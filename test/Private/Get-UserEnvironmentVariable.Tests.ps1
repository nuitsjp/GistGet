# テスト対象のモジュールをインポート
Import-Module -Name "$PSScriptRoot\..\..\src\GistGet.psd1" -Force

InModuleScope GistGet {
    Describe "Get-UserEnvironmentVariable Tests" {
        BeforeEach {
            Set-ItemProperty -Path 'HKCU:\Environment' -Name "GIST_GET_TEST" -Value "FooBar"
        }

        It "ファイル名を指定しない場合、最初のファイルを取得する" {
            # Arrange: テストの準備
            $name = "GIST_GET_TEST"

            # Act: 関数を実行
            $result = Get-UserEnvironmentVariable -Name $name

            # Assert: 結果が期待通りか確認
            $result | Should -Be "FooBar"
        }

        AfterEach {
            # 環境変数を削除する
            Set-ItemProperty -Path 'HKCU:\Environment' -Name "GIST_GET_TEST" -Value $null
        }
    }
}
