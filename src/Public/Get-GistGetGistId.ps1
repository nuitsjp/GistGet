function Get-GistGetGistId {
    # ユーザー環境変数「GistGetGistId」を取得
    $gistId = [System.Environment]::GetEnvironmentVariable('GistGetGistId', [System.EnvironmentVariableTarget]::User)
    if($gistId)
    {
        return $gistId
    }
    else
    {
        # Gistの検索
        $gists = Get-GitHubGist | Where-Object { $_.Description -like "$(Get-GistDescription)" }

        # 検索結果の数を取得
        $gistCount = if ($null -eq $gists) { 0 } else { @($gists).Count }
        switch ($gistCount) {
            0 {
                return $null
            }
            1 {
                return $gists[0].id
            }
            default {
                throw "Multiple Gists were found with ""$(Get-GistDescription)"" set in the Gist description."
            }
        }
    }
}
