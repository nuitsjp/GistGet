#requires -Version 7.0
<#
.SYNOPSIS
    GistGet のリリースビルドを作成します。

.DESCRIPTION
    このスクリプトは以下の処理を行います:
    1. 出力ディレクトリをクリア
    2. GistGet.csproj をpublish（ランチャー）
    3. NuitsJp.GistGet.csproj を同じディレクトリにpublish（メイン実装 + COM DLL）
    4. オプションでZIPアーカイブを作成

.PARAMETER Version
    リリースバージョン (例: 1.0.0)。指定しない場合は csproj から取得。

.PARAMETER OutputPath
    出力ディレクトリのパス。指定しない場合は artifacts/publish/win-x64。

.PARAMETER CreateZip
    ZIPアーカイブを作成する場合に指定。

.EXAMPLE
    .\Build-Release.ps1
    デフォルト設定でリリースビルドを作成

.EXAMPLE
    .\Build-Release.ps1 -OutputPath "C:\temp\gistget"
    指定したディレクトリにリリースビルドを作成

.EXAMPLE
    .\Build-Release.ps1 -CreateZip
    ZIPアーカイブも作成
#>

[CmdletBinding()]
param(
    [Parameter()]
    [string]$Version,

    [Parameter()]
    [string]$OutputPath,

    [Parameter()]
    [switch]$CreateZip
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

#region Constants and Paths
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..\..')).Path
$gistGetProjectPath = Join-Path $repoRoot 'src/GistGet/GistGet.csproj'
$nuitsJpGistGetProjectPath = Join-Path $repoRoot 'src/NuitsJp.GistGet/NuitsJp.GistGet.csproj'
$artifactsPath = Join-Path $repoRoot 'artifacts'
#endregion

#region Helper Functions
function Write-Step {
    param([string]$Step, [string]$Description)
    Write-Host ""
    Write-Host "[$Step] $Description" -ForegroundColor Green
}

function Get-VersionFromCsproj {
    [xml]$csproj = Get-Content $gistGetProjectPath
    $ver = $csproj.Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
    if (-not $ver) {
        throw "バージョンが csproj に定義されていません。-Version パラメータで指定してください。"
    }
    return $ver
}
#endregion

#region Main
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "GistGet Release Build" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# バージョンの取得
if (-not $Version) {
    $Version = Get-VersionFromCsproj
}

# 出力パスの設定
if (-not $OutputPath) {
    $OutputPath = Join-Path $artifactsPath "publish/win-x64"
}

Write-Host "Version: $Version" -ForegroundColor Yellow
Write-Host "Output: $OutputPath" -ForegroundColor Yellow
Write-Host ""

# Step 1: 出力ディレクトリのクリア
Write-Step "1" "出力ディレクトリをクリア中..."
if (Test-Path $OutputPath) {
    Remove-Item -Recurse -Force $OutputPath
    Write-Host "既存のファイルを削除しました。" -ForegroundColor Gray
}
New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null

# Step 2: GistGet.csproj をpublish（ランチャー）
Write-Step "2" "GistGet.csproj をpublish中..."
$publishArgs = @(
    'publish'
    $gistGetProjectPath
    '-c', 'Release'
    '-r', 'win-x64'
    '-o', $OutputPath
    "-p:Version=$Version"
    '--self-contained', 'true'
)

& dotnet @publishArgs
if ($LASTEXITCODE -ne 0) {
    Write-Error "GistGet.csproj のpublishに失敗しました。"
    exit $LASTEXITCODE
}

# Step 3: NuitsJp.GistGet.csproj をpublish（メイン実装 + COM DLL）
Write-Step "3" "NuitsJp.GistGet.csproj をpublish中..."
$publishArgs = @(
    'publish'
    $nuitsJpGistGetProjectPath
    '-c', 'Release'
    '-r', 'win-x64'
    '-o', $OutputPath
    "-p:Version=$Version"
    '--self-contained', 'true'
)

& dotnet @publishArgs
if ($LASTEXITCODE -ne 0) {
    Write-Error "NuitsJp.GistGet.csproj のpublishに失敗しました。"
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "ビルド完了: $OutputPath" -ForegroundColor Green

# Step 4: ビルド結果の検証
Write-Step "4" "ビルド結果を検証中..."

$requiredFiles = @(
    @{ Name = 'GistGet.exe'; Description = 'ランチャー実行ファイル' },
    @{ Name = 'NuitsJp.GistGet.exe'; Description = 'メイン実行ファイル' },
    @{ Name = 'Microsoft.Management.Deployment.CsWinRTProjection.dll'; Description = 'WinGet COM相互運用DLL' },
    @{ Name = 'Microsoft.Management.Deployment.dll'; Description = 'WinGet COM相互運用DLL' },
    @{ Name = 'Microsoft.Management.Deployment.winmd'; Description = 'WinGet メタデータ' },
    @{ Name = 'WinRT.Runtime.dll'; Description = 'WinRT ランタイムDLL' }
)

$missingFiles = @()
foreach ($file in $requiredFiles) {
    $filePath = Join-Path $OutputPath $file.Name
    if (Test-Path $filePath) {
        $size = (Get-Item $filePath).Length
        $sizeStr = if ($size -gt 1MB) { "{0:N1} MB" -f ($size / 1MB) } else { "{0:N0} KB" -f ($size / 1KB) }
        Write-Host "  ✓ $($file.Name) ($sizeStr)" -ForegroundColor Green
    } else {
        Write-Host "  ✗ $($file.Name) - 見つかりません ($($file.Description))" -ForegroundColor Red
        $missingFiles += $file.Name
    }
}

if ($missingFiles.Count -gt 0) {
    Write-Error @"
ビルド結果の検証に失敗しました。
以下の必須ファイルが見つかりません:
$($missingFiles -join "`n")

ビルドプロセスを確認してください。
"@
    exit 1
}

Write-Host "すべての必須ファイルが存在します。" -ForegroundColor Green

# Step 5: ZIPアーカイブの作成（オプション）
if ($CreateZip) {
    Write-Step "5" "ZIPアーカイブを作成中..."
    $zipName = "GistGet-win-x64.zip"
    $zipPath = Join-Path $artifactsPath $zipName

    if (Test-Path $zipPath) {
        Remove-Item -Force $zipPath
    }

    Compress-Archive -Path "$OutputPath/*" -DestinationPath $zipPath -Force

    # SHA256 計算
    $sha256 = (Get-FileHash -Path $zipPath -Algorithm SHA256).Hash
    Write-Host "SHA256: $sha256" -ForegroundColor Cyan

    # SHA256SUMS.txt 作成
    $hashFilePath = Join-Path $artifactsPath "SHA256SUMS.txt"
    "$sha256  $zipName" | Set-Content -Path $hashFilePath -Encoding UTF8

    Write-Host "ZIPアーカイブ: $zipPath" -ForegroundColor Green
}

# 出力ファイル一覧
Write-Host ""
Write-Host "出力ファイル:" -ForegroundColor Yellow
Get-ChildItem $OutputPath | ForEach-Object {
    $size = if ($_.Length -gt 1MB) { "{0:N1} MB" -f ($_.Length / 1MB) } else { "{0:N0} KB" -f ($_.Length / 1KB) }
    Write-Host ("  {0,-50} {1,10}" -f $_.Name, $size) -ForegroundColor Gray
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "完了!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

# 出力パスを返す（パイプラインで使用可能）
return $OutputPath
#endregion
