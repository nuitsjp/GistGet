# テスト対象のモジュールをインポート
Import-Module -Name "$PSScriptRoot\..\..\src\GistGet.psd1" -Force

InModuleScope GistGet {
    Describe "Get-GistContent Tests" {
        It "ファイル名を指定しない場合、最初のファイルを取得する" {
            # Arrange: テストの準備
            $gistId = "e081365e591c1d76141da8bef4fa2057"
            $gistFileName = "second.txt"
            $gist = [GistFile]::new($gistId, $gistFileName)

            # Act: 関数を実行
            $result = Get-GistContent -GistFile $gist

            # Assert: 結果が期待通りか確認
            $result | Should -Be "Second"
        }

        It "ファイル名が指定された場合、指定されたファイルを取得する" {
            # Arrange: テストの準備
            $gistId = "e081365e591c1d76141da8bef4fa2057"
            $gistFileName = "foo"
            $gist = [GistFile]::new($gistId, $gistFileName)

            # Act: 関数を実行
            { Get-GistContent -Gist $gist } | Should -Throw

            # Assert: 結果が期待通りか確認
        }

        It "ファイル名が指定された場合、指定されたファイルを取得する" {
            # Arrange: テストの準備
            $gistId = "foo"
            $gistFileName = "second.txt"
            $gist = [GistFile]::new($gistId, $gistFileName)

            # Act: 関数を実行
            { Get-GistContent -Gist $gist } | Should -Throw

            # Assert: 結果が期待通りか確認
        }
    }
}
