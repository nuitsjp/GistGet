<#
.SYNOPSIS
    WinGetマニフェストを検証します。

.DESCRIPTION
    指定されたディレクトリのWinGetマニフェストファイルを検証し、
    結果を日本語で出力します。

.PARAMETER ManifestPath
    マニフェストファイルが格納されているディレクトリパス。

.EXAMPLE
    .\validate-manifest.ps1 -ManifestPath "external/winget-pkgs/manifests/n/NuitsJp/GistGet/1.0.5"
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ManifestPath
)

$ErrorActionPreference = 'Stop'

# パスの検証
if (-not (Test-Path $ManifestPath)) {
    Write-Error "マニフェストディレクトリが見つかりません: $ManifestPath"
    exit 1
}

# 必要なファイルの確認
$requiredFiles = @(
    "NuitsJp.GistGet.yaml",
    "NuitsJp.GistGet.installer.yaml",
    "NuitsJp.GistGet.locale.en-US.yaml"
)

Write-Host "マニフェストファイルを確認中..." -ForegroundColor Cyan

$missingFiles = @()
foreach ($file in $requiredFiles) {
    $filePath = Join-Path $ManifestPath $file
    if (Test-Path $filePath) {
        Write-Host "  [OK] $file" -ForegroundColor Green
    } else {
        Write-Host "  [NG] $file - 見つかりません" -ForegroundColor Red
        $missingFiles += $file
    }
}

if ($missingFiles.Count -gt 0) {
    Write-Error "必要なファイルが不足しています: $($missingFiles -join ', ')"
    exit 1
}

# winget validate の実行
Write-Host ""
Write-Host "winget validate を実行中..." -ForegroundColor Cyan

$validateResult = winget validate --manifest $ManifestPath 2>&1
$exitCode = $LASTEXITCODE

Write-Host ""
Write-Host "検証結果:" -ForegroundColor Yellow
Write-Host $validateResult

if ($exitCode -eq 0) {
    Write-Host ""
    Write-Host "検証成功: マニフェストは有効です。" -ForegroundColor Green
    exit 0
} else {
    Write-Host ""
    Write-Host "検証失敗: マニフェストにエラーがあります。" -ForegroundColor Red
    exit $exitCode
}
