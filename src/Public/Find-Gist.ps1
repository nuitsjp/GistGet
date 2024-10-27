function Find-Gist
{
    $gistId = [System.Environment]::GetEnvironmentVariable('GIST_GET_GIST_ID', [System.EnvironmentVariableTarget]::User)
    $fistFileName = [System.Environment]::GetEnvironmentVariable('GIST_GET_GIST_FILE_NAME', [System.EnvironmentVariableTarget]::User)
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
                return $null
            }
            1 {
                return $gists[0].id
            }
            default {
                throw "Multiple Gists were found with ""$(Get-GistDescription)"" set in the Gist description."
            }
        }

        throw "Please specify a GistId or Path. Alternatively, you need to register the GistId in advance using Set-GistGetGistId."
    }

}