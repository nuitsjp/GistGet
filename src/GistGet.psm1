Get-ChildItem -Path $PSScriptRoot\Library -Filter *.ps1 | foreach-object { . $_.FullName }
