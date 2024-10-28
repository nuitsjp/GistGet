# Module Pesterの5.6.1以上がインストールされていなかった場合は、インストールする
if (-not (Get-Module -Name Pester -ListAvailable | Where-Object Version -ge 5.6.1)) {
    Install-Module -Name Pester -Force -Scope CurrentUser
}

if (-not (Get-Module -Name Microsoft.WinGet.Client -ListAvailable)) {
    Install-Module -Name Microsoft.WinGet.Client -Force -Scope CurrentUser
}

# Invoke-Pester -Name "Install-GistGetPackage Not Installed Tests"
Import-Module -Name "$PSScriptRoot\..\src\GistGet.psd1" -Force
Get-GistGetPackage
