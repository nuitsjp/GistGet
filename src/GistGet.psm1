# GistGet.psm1

# エラーが発生した場合はスクリプトを停止
$ErrorActionPreference = 'Stop'

# Check if WinGet is available
try {
    $null = Get-Command winget -ErrorAction Stop
}
catch {
    Write-Error "WinGet is not installed. Please install WinGet first."
    return
}

Import-Module -Name PowerShellForGitHub
Import-Module -Name powershell-yaml
Import-Module -Name Microsoft.WinGet.Client

# グローバル変数の定義
# GistIdを環境変数へ保管するためのキー
$global:GistGetGistId = 'GistGetGistId'


# Publicフォルダーのスクリプトをロード（公開関数）
Get-ChildItem -Path (Join-Path $PSScriptRoot 'Public') -Filter *.ps1 | ForEach-Object {
    . $_.FullName
}

# Privateフォルダーのスクリプトをロード（非公開関数）
Get-ChildItem -Path (Join-Path $PSScriptRoot 'Private') -Filter *.ps1 | ForEach-Object {
    . $_.FullName
}

# Publicフォルダー内の関数のみを公開
Export-ModuleMember -Function (Get-ChildItem -Path (Join-Path $PSScriptRoot 'Public') -Filter *.ps1 | ForEach-Object {
    $content = Get-Content $_.FullName | Select-String -Pattern '^function\s+([^\s{]+)'
    if ($content) { $content.Matches.Groups[1].Value }
})
