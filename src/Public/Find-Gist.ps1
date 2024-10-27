function Find-Gist
{
    $gistId = Get-UserEnvironmentVariable -Name 'GIST_GET_GIST_ID'
    $fistFileName = Get-UserEnvironmentVariable -Name 'GIST_GET_GIST_FILE_NAME'
    if($gistId -and $fistFileName) {
        return [Gist]::new($gistId, $fistFileName)
    }
    else {
        # Gistの検索
        $gists = Get-GitHubGist | Where-Object { $_.Description -like "$(Get-GistDescription)" }

        # 検索結果の数を取得
        $gistCount = if ($null -eq $gists) { 0 } else { @($gists).Count }
        switch ($gistCount) {
            0 {
                # Gistが見つからない場合
                throw "Gist with ""$(Get-GistDescription)"" in the Gist description was not found."
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