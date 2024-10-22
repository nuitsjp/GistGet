# テスト対象のモジュールをインポート
Import-Module -Name "$PSScriptRoot\..\..\src\GistGet.psd1" -Force

InModuleScope GistGet {
    Describe "Set-Gist Tests" {
        It "ファイル名を指定しない場合、最初のファイルを更新する" {
            try {
                # Arrange: テストの準備
                $gistId = "e723f4e0011958413d806c589c6ffdd4"
                $currentDateTime = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
                $content = "First Updated at $currentDateTime"

                # Act: 関数を実行
                Set-Gist -GistId $gistId -Content $content

                # Assert: 結果が期待通りか確認
                $result = Get-Gist -GistId $gistId
                $result | Should -Be $content
            }
            catch {
                if ($_.Exception.Response.StatusCode -eq 404) {
                    Write-Error "Gist not found."
                }
                else {
                    Write-Error $_.Exception.Message
                }
                throw
            }
        }

        It "ファイル名が指定された場合、指定されたファイルを更新する" {
            # Arrange: テストの準備
            $gistId = "e723f4e0011958413d806c589c6ffdd4"
            $gistFileName = "second.txt"
            $currentDateTime = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
            $content = "Second Updated at $currentDateTime"

            # Act: 関数を実行
            Set-Gist -GistId $gistId -GistFileName $gistFileName -Content $content

            # Assert: 結果が期待通りか確認
            $result = Get-Gist -GistId $gistId -GistFileName $gistFileName
            $result | Should -Be $content
        }

        It "存在しないGistIdを指定された場合、エラーとなる" {
            # Arrange: テストの準備
            $gistId = "notfound"
            $content = "Not Found"

            # Act: 関数を実行
            $errorInfo = $null
            try {
                Set-Gist -GistId $gistId -Content $content
            }
            catch {
                $errorInfo = $_
            }

            # Assert: 結果が期待通りか確認
            $errorInfo.Exception.Message.Contains('https://github.com/nuitsjp/GistGet/blob/main/docs/Set-GitHubToken.md') | Should -Be $true
            $errorInfo.Exception.InnerException.Response.StatusCode | Should -Be 404
        }
    }
}
