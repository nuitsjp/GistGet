function Set-GistGetGistId {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [string]$GistId
    )

    # ユーザー環境変数「GistGetGistId」にGistIdを設定
    [System.Environment]::SetEnvironmentVariable('GistGetGistId', $GistId, [System.EnvironmentVariableTarget]::User)
    # プロセス環境変数「GistGetGistId」にGistIdを設定
    [System.Environment]::SetEnvironmentVariable('GistGetGistId', $GistId, [System.EnvironmentVariableTarget]::Process)
}
