Get-ChildItem -Path $PSScriptRoot\..\src\Library -Filter *.ps1 | foreach-object { . $_.FullName }

Uninstall-GistGetPackage