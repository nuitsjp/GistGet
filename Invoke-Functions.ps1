param(
    [Parameter(Mandatory = $true)]
    [string] $Token
)

Import-Module -Name "$PSScriptRoot\src\GistGet.psd1" -Force

Set-GitHubToken -Token $Token