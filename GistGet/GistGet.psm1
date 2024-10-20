Get-ChildItem -Path $PSScriptRoot\Private -Filter *.ps1 | foreach-object { . $_.FullName }
Get-ChildItem -Path $PSScriptRoot\Public -Filter *.ps1 | foreach-object { . $_.FullName }
