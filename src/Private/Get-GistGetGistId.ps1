function Get-GistGetGistId {
    # ユーザー環境変数「GistGetGistId」を取得
    return [System.Environment]::GetEnvironmentVariable('GistGetGistId', [System.EnvironmentVariableTarget]::User)
}
