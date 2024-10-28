# テスト対象のモジュールをインポート
Import-Module -Name "$PSScriptRoot\..\..\src\GistGet.psd1" -Force

InModuleScope GistGet {
    Describe "Set-GistContent Tests" {
        It "指定されたGistを更新する" {
            # Arrange: テストの準備
            $gistId = "e723f4e0011958413d806c589c6ffdd4"
            $gistFileName = "second.txt"
            $gist = [GistFile]::new($gistId, $gistFileName)
            $currentDateTime = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
            $content = "Second Updated at $currentDateTime"

            # Act: 関数を実行
            Set-GistContent -Gist $gist -Content $content

            # Assert: 結果が期待通りか確認
            $result = Get-GistContent -Gist $gist
            $result | Should -Be $content
        }

        It "存在しないGistIdを指定された場合、エラーとなる" {
            # Arrange: テストの準備
            $gistId = "notfound"
            $gistFileName = "second.txt"
            $gist = [GistFile]::new($gistId, $gistFileName)
            $content = "Not Found"

            # Act: 関数を実行
            $errorInfo = $null
            try {
                Set-GistContent -Gist $gist -Content $content
            }
            catch {
                $errorInfo = $_
            }

            # Assert: 結果が期待通りか確認
            $errorInfo.Exception.Message.Contains('https://github.com/nuitsjp/GistGet/blob/main/docs/Set-GitHubToken.md') | Should -Be $true
            $errorInfo.Exception.InnerException.Response.StatusCode | Should -Be 404
        }

        It "存在しないファイル名を指定された場合、追加されるがこのケースの呼び出しパターンは存在しない" {
            # Arrange: テストの準備

            # Act: 関数を実行

            # Assert: 結果が期待通りか確認
        }
    }
}
