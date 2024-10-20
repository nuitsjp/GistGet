Get-ChildItem -Path $PSScriptRoot\..\src\Library -Filter *.ps1 | foreach-object { . $_.FullName }

# Import-GistGetPackage -GistId '49990de4389f126d1f6d57c10c408a0c' -Verbose
# Import-GistGetPackage -Uri https://gist.githubusercontent.com/nuitsjp/49990de4389f126d1f6d57c10c408a0c/raw/9b7f836844695528df08261cc15c56fd8e1bdb8f/winget.yml -Verbose
Import-GistGetPackage -Verbose
