Import-Module $PSScriptRoot\..\src\GistGet.psd1 -Force
Invoke-Pester -Name "Install-GistGetPackage"