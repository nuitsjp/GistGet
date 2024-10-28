function Set-GistFile
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [GistFile] $GistFile
    )
    Set-UserEnvironmentVariable -Name $global:EnvironmentVariableNameGistId -Value $GistFile.Id
    Set-UserEnvironmentVariable -Name $global:EnvironmentVariableNameGistFileName -Value $GistFile.FileName
}