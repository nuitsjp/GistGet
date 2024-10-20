Get-ChildItem -Path $PSScriptRoot\..\src\Library -Filter *.ps1 | foreach-object { . $_.FullName }

$GistId = Get-GistGetGistId
Write-Host $GistId