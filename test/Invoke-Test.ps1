Import-Module $PSScriptRoot\..\src\GistGet.psd1 -Force
Invoke-Pester -Name "Update-GistGetPackage Update, Exists, Exists, NotEqual, True, False, True, True"