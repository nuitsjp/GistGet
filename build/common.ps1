function Get-LatestReleaseVersion {
    [CmdletBinding()]
    param(
        # タグのプレフィックス（デフォルトは"Release-"）
        [string]$Prefix = "Release-"
    )
    
    # Gitのタグを取得
    $tags = git tag
    if ($null -eq $tags -or $tags.Count -eq 0) {
        Write-Error "No Git tags found."
    }

    # Release-X.Y.Z 形式のタグをフィルタリングして、バージョンオブジェクトに変換
    $versions = $tags | 
        Where-Object { $_ -match "^$([regex]::Escape($Prefix))(\d+\.\d+\.\d+)$" } |
        ForEach-Object {
            [PSCustomObject]@{
                Version = [System.Version]($_ -replace "^$([regex]::Escape($Prefix))", '')
                Tag = $_
            }
        }

    if ($null -eq $versions -or $versions.Count -eq 0) {
        Write-Error "No valid release tags found matching the pattern: ${Prefix}X.Y.Z"
    }

    # 最新バージョンを取得
    $latest = $versions | Sort-Object { $_.Version } -Descending | Select-Object -First 1

    return $latest
}

# デフォルトエンコーディングをUTF-8に設定
[System.Console]::OutputEncoding = [System.Text.Encoding]::UTF8
[System.Console]::InputEncoding = [System.Text.Encoding]::UTF8
$PSDefaultParameterValues['*:Encoding'] = 'utf8'
$OutputEncoding = [System.Text.Encoding]::UTF8

$ErrorActionPreference = 'Stop'

# グローバル変数：スクリプトのルートディレクトリを取得
$global:projectRoot = Split-Path -Parent $PSScriptRoot
$global:buildScript = Join-Path $global:projectRoot 'build' 'build.ps1'
$global:outputPath = Join-Path $global:projectRoot 'build' 'Output'
$global:modulePath = Join-Path $outputPath 'GistGet'
$global:srcPath = Join-Path $global:projectRoot 'src'

