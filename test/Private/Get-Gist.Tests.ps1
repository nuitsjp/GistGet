# テスト対象のモジュールをインポート
Import-Module -Name "$PSScriptRoot\..\..\src\GistGet.psd1" -Force

InModuleScope GistGet {
    Describe "Get-Gist Tests" {
        It "ファイル名を指定しない場合、最初のファイルを取得する" {
            # Arrange: テストの準備
            $gistId = "e081365e591c1d76141da8bef4fa2057"

            # Act: 関数を実行
            $result = Get-Gist -GistId $gistId

            # Assert: 結果が期待通りか確認
            $result | Should -Be "First"
        }

        It "ファイル名が指定された場合、指定されたファイルを取得する" {
            # Arrange: テストの準備
            $gistId = "e081365e591c1d76141da8bef4fa2057"
            $gistFileName = "second.txt"

            # Act: 関数を実行
            $result = Get-Gist -GistId $gistId -GistFileName $gistFileName

            # Assert: 結果が期待通りか確認
            $result | Should -Be "Second"
        }
    }
}
