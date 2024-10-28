function Set-GistContent {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [Gist] $Gist,
        [Parameter(Mandatory = $true)]
        [string] $Content
    )

    try {
        # ファイルの名前を取得
        $gistId = $Gist.Id
        $fileName = $Gist.FileName

        # 更新用のハッシュテーブルを作成
        $updateHash = @{
            $fileName = @{
                content = $Content
            }
        }

        # Gistを更新
        Set-GitHubGist -Gist $gistId -Update $updateHash
    }
    catch {
        if ($_.Exception.Response.StatusCode -eq 404) {
            # メッセージと内部例外を設定して例外をスロー
            $message = "Gist '$GistId' is missing or you do not have permission to access it, if GistId is correct, please issue a token and set it up: https://github.com/nuitsjp/GistGet/blob/main/docs/Set-GitHubToken.md"
            throw New-Object System.Exception($message, $_.Exception)
        }
        else {
            throw
        }
    }
}