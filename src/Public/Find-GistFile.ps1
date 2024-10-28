function Find-Gist
{
    $gistId = Get-UserEnvironmentVariable -Name $global:EnvironmentVariableNameGistId
    $fistFileName = Get-UserEnvironmentVariable -Name $global:EnvironmentVariableNameGistFileName

    if($gistId -and $fistFileName) {
        return [GistFile]::new($gistId, $fistFileName)
    }
    else {
        # Gistの検索
        $all = Get-GitHubGist
        $gists = $all | Where-Object { $_.Description -like "$(Get-GistDescription)" }

        # 検索結果の数を取得
        $gistCount = if ($null -eq $gists) { 0 } else { @($gists).Count }
        switch ($gistCount) {
            0 {
                # Gistが見つからない場合
                throw "Gist with ""$(Get-GistDescription)"" in the Gist description was not found."
            }
            1 {
                $id = $gists[0].id
                $files = $gists[0].files
                $fileNames = $files.PSObject.Properties.Name
                $fileCount = @($fileNames).Count
                if($fileCount -gt 1) {
                    throw "Multiple files were found in the Gist with ""$(Get-GistDescription)"" set in the Gist description."
                }
                $fileName = $fileNames | Select-Object -First 1
                return [GistFile]::new($id, $fileName)
            }
            default {
                throw "Multiple Gists were found with ""$(Get-GistDescription)"" set in the Gist description."
            }
        }
    }

}