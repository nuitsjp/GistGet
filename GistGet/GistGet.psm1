# GistGet.psm1

# Publicフォルダーのスクリプトをロード（公開関数）
Get-ChildItem -Path (Join-Path $PSScriptRoot 'Public') -Filter *.ps1 | ForEach-Object {
    . $_.FullName
}

# Privateフォルダーのスクリプトをロード（非公開関数）
# Get-ChildItem -Path (Join-Path $PSScriptRoot 'Private') -Filter *.ps1 | ForEach-Object {
#     . $_.FullName
# }

# # Publicフォルダー内の関数のみを公開
# Export-ModuleMember -Function (Get-ChildItem -Path (Join-Path $PSScriptRoot 'Public') -Filter *.ps1 | ForEach-Object {
#     $content = Get-Content $_.FullName | Select-String -Pattern '^function\s+([^\s{]+)'
#     if ($content) { $content.Matches.Groups[1].Value }
# })
