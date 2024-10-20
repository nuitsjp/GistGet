function Get-GistGetGistId {
    # プロセス環境変数「GistGetGistId」を取得
    $GistId = [System.Environment]::GetEnvironmentVariable('GistGetGistId', [System.EnvironmentVariableTarget]::Process)
    if ($GistId) {
        return $GistId
    }
    # ユーザー環境変数「GistGetGistId」を取得
    return [System.Environment]::GetEnvironmentVariable('GistGetGistId', [System.EnvironmentVariableTarget]::User)
}
