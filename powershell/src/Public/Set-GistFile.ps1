function Set-GistFile
{
    [CmdletBinding()]
    param(
        [GistFile] $GistFile,
        [string] $GistId,
        [string] $GistFileName
    )

    if ($GistFile) {
        Set-UserEnvironmentVariable -Name $global:EnvironmentVariableNameGistId -Value $GistFile.Id
        Set-UserEnvironmentVariable -Name $global:EnvironmentVariableNameGistFileName -Value $GistFile.FileName
    }
    elseif ($GistId -and $GistFileName) {
        Set-UserEnvironmentVariable -Name $global:EnvironmentVariableNameGistId -Value $GistId
        Set-UserEnvironmentVariable -Name $global:EnvironmentVariableNameGistFileName -Value $GistFileName
    }
    else {
        throw "Either a GistFile object or a GistId and GistFileName must be provided."
    }
}