<#
.SYNOPSIS
    Retrieves the GistGet Gist ID from environment variables or GitHub.

.DESCRIPTION
    This function attempts to retrieve the Gist ID for GistGet in the following order:
    1. Checks the user environment variable 'GistGetGistId'
    2. If not found, searches GitHub for Gists matching the GistGet description
    The function ensures unique identification of the target Gist.

.EXAMPLE
    PS> $gistId = Get-GistGetGistId
    Retrieves the Gist ID for GistGet configuration.

.NOTES
    Behavior in different scenarios:
    - If environment variable exists: Returns the stored Gist ID
    - If no Gists found: Returns null
    - If one Gist found: Returns its ID
    - If multiple Gists found: Throws an error to prevent ambiguity

    Requirements:
    - GitHub authentication must be configured
    - Get-GistDescription function must be available

.OUTPUTS
    [String]
    Returns the Gist ID if found, null if no Gist is found.

.LINK
    Get-GitHubGist
    Get-GistDescription
#>
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
