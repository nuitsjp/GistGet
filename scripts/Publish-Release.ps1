<#
.SYNOPSIS
    GistGet のリリースビルドを作成し、配布用 ZIP アーカイブを生成します。

.DESCRIPTION
    このスクリプトは以下の処理を行います:
    1. x64 および ARM64 向けに Self-Contained ビルドを実行
    2. 各アーキテクチャ用の ZIP アーカイブを作成
    3. SHA256 ハッシュを計算し、ハッシュファイルを生成

.PARAMETER Version
    リリースバージョン (例: 0.1.0)。指定しない場合は csproj から取得。

.PARAMETER Configuration
    ビルド構成。既定値は Release。

.PARAMETER OutputPath
    出力先ディレクトリ。既定値は artifacts/。

.EXAMPLE
    .\Publish-Release.ps1
    csproj のバージョンでリリースビルドを作成

.EXAMPLE
    .\Publish-Release.ps1 -Version 1.0.0
    バージョン 1.0.0 でリリースビルドを作成
#>

[CmdletBinding()]
param(
    [Parameter()]
    [string]$Version,

    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [Parameter()]
    [string]$OutputPath = 'artifacts'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# スクリプトのルートディレクトリを取得
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptRoot
$projectPath = Join-Path $repoRoot 'src/GistGet/GistGet.csproj'

# バージョンが指定されていない場合は csproj から取得
if (-not $Version) {
    [xml]$csproj = Get-Content $projectPath
    $Version = $csproj.Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
    if (-not $Version) {
        Write-Error "バージョンが csproj に定義されていません。-Version パラメータで指定してください。"
        exit 1
    }
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "GistGet Release Build" -ForegroundColor Cyan
Write-Host "Version: $Version" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# 出力ディレクトリの準備
$artifactsPath = Join-Path $repoRoot $OutputPath
if (Test-Path $artifactsPath) {
    Write-Host "既存の出力ディレクトリを削除: $artifactsPath" -ForegroundColor Yellow
    Remove-Item -Recurse -Force $artifactsPath
}
New-Item -ItemType Directory -Path $artifactsPath -Force | Out-Null

# ビルド対象のアーキテクチャ
$architectures = @(
    @{ Rid = 'win-x64'; Name = 'x64' },
    @{ Rid = 'win-arm64'; Name = 'arm64' }
)

$hashEntries = @()

foreach ($arch in $architectures) {
    $rid = $arch.Rid
    $archName = $arch.Name
    $publishPath = Join-Path $artifactsPath "publish/$rid"
    $zipName = "GistGet-$rid.zip"
    $zipPath = Join-Path $artifactsPath $zipName

    Write-Host ""
    Write-Host "[$archName] ビルド開始..." -ForegroundColor Green

    # dotnet publish 実行
    $publishArgs = @(
        'publish'
        $projectPath
        '-c', $Configuration
        '-r', $rid
        '-o', $publishPath
        '-p:Version=' + $Version
        '--self-contained', 'true'
    )

    Write-Host "dotnet $($publishArgs -join ' ')" -ForegroundColor DarkGray
    & dotnet @publishArgs

    if ($LASTEXITCODE -ne 0) {
        Write-Error "[$archName] ビルドに失敗しました。"
        exit $LASTEXITCODE
    }

    # ZIP アーカイブの作成
    Write-Host "[$archName] ZIP アーカイブを作成: $zipName" -ForegroundColor Green
    Compress-Archive -Path "$publishPath/*" -DestinationPath $zipPath -Force

    # SHA256 ハッシュの計算
    $hash = (Get-FileHash -Path $zipPath -Algorithm SHA256).Hash
    Write-Host "[$archName] SHA256: $hash" -ForegroundColor Cyan

    $hashEntries += [PSCustomObject]@{
        Architecture = $archName
        FileName = $zipName
        SHA256 = $hash
    }
}

# ハッシュファイルの生成
$hashFilePath = Join-Path $artifactsPath "SHA256SUMS.txt"
$hashContent = $hashEntries | ForEach-Object {
    "$($_.SHA256)  $($_.FileName)"
}
$hashContent | Set-Content -Path $hashFilePath -Encoding UTF8

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "ビルド完了!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "出力ファイル:" -ForegroundColor Yellow
Get-ChildItem -Path $artifactsPath -File | ForEach-Object {
    Write-Host "  - $($_.Name)" -ForegroundColor White
}
Write-Host ""
Write-Host "SHA256 ハッシュ:" -ForegroundColor Yellow
$hashEntries | ForEach-Object {
    Write-Host "  $($_.Architecture): $($_.SHA256)" -ForegroundColor White
}

# WinGet マニフェスト更新用の情報を出力
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "WinGet マニフェスト用情報" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "PackageVersion: $Version" -ForegroundColor White
foreach ($entry in $hashEntries) {
    $installerUrl = "https://github.com/nuitsjp/GistGet/releases/download/v$Version/$($entry.FileName)"
    Write-Host ""
    Write-Host "Architecture: $($entry.Architecture)" -ForegroundColor Yellow
    Write-Host "InstallerUrl: $installerUrl" -ForegroundColor White
    Write-Host "InstallerSha256: $($entry.SHA256)" -ForegroundColor White
}

# 戻り値としてハッシュ情報を返す
return $hashEntries
