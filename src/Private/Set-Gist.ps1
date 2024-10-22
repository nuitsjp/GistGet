function Set-Gist {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string] $GistId,
        [string] $GistFileName,
        [Parameter(Mandatory = $true)]
        [string] $Content
    )

    try {
        # ファイルの名前を取得
        $fileName = $GistFileName
        if (-not $fileName) {
            # Gistの情報を取得
            $gist = Get-GitHubGist -Gist $GistId

            # Get the first file if GistFileName is not specified
            $fileName = $gist.files.PSObject.Properties.Name | Select-Object -First 1
        }

        # 更新用のハッシュテーブルを作成
        $updateHash = @{
            $fileName = @{
                content = $Content
            }
        }

        # Gistを更新
        Set-GitHubGist -Gist $GistId -Update $updateHash
    }
    catch {
        if ($_.Exception.Response.StatusCode -eq 404) {
            # メッセージと内部例外を設定して例外をスロー
            $message = "Gist '$GistId' not found. "
            throw New-Object System.Exception($message, $_.Exception)
        }
        else {
            throw
        }
    }
}