<#
.SYNOPSIS
    ReSharper コマンドラインツールをセットアップします。

.DESCRIPTION
    JetBrains ReSharper コマンドラインツール（InspectCode など）を .NET ローカルツールとして
    プロジェクトにインストールします。これにより「Find Code Issues」などの機能を利用できます。

.EXAMPLE
    .\Setup-ReSharperCLT.ps1
    ReSharper コマンドラインツールをインストール

.NOTES
    前提条件: .NET SDK がインストールされていること
#>

$ErrorActionPreference = "Stop"

$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptPath
$configDir = Join-Path $repoRoot ".config"
$manifestPath = Join-Path $configDir "dotnet-tools.json"

function Write-Banner {
    param([string]$Title)
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host $Title -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
}

Write-Banner "ReSharper Command Line Tools Setup"

# Step 1: ツールマニフェストの作成（存在しない場合）
if (-not (Test-Path $manifestPath)) {
    Write-Host "Creating .NET tool manifest..." -ForegroundColor Yellow
    Push-Location $repoRoot
    try {
        dotnet new tool-manifest
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to create tool manifest"
        }
        Write-Host "Tool manifest created: $manifestPath" -ForegroundColor Green
    }
    finally {
        Pop-Location
    }
}
else {
    Write-Host "Tool manifest already exists: $manifestPath" -ForegroundColor Green
}

# Step 2: ReSharper CLI ツールのインストール
Write-Host ""
Write-Host "Installing JetBrains ReSharper Global Tools..." -ForegroundColor Yellow
Push-Location $repoRoot
try {
    # 既にインストール済みかチェック
    $toolList = dotnet tool list --local 2>&1
    if ($toolList -match "jetbrains\.resharper\.globaltools") {
        Write-Host "ReSharper CLI tools are already installed." -ForegroundColor Green
        
        # 更新があるかチェック
        Write-Host "Checking for updates..." -ForegroundColor Yellow
        dotnet tool update JetBrains.ReSharper.GlobalTools --local
    }
    else {
        dotnet tool install JetBrains.ReSharper.GlobalTools --local
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to install ReSharper CLI tools"
        }
        Write-Host "ReSharper CLI tools installed successfully." -ForegroundColor Green
    }
}
finally {
    Pop-Location
}

Write-Banner "Setup Complete"

Write-Host ""
Write-Host "利用可能なコマンド:" -ForegroundColor Yellow
Write-Host "  dotnet jb inspectcode   - コード品質の問題を検出" -ForegroundColor White
Write-Host "  dotnet jb cleanupcode   - コードのクリーンアップ" -ForegroundColor White
Write-Host "  dotnet jb dupfinder     - 重複コードの検出" -ForegroundColor White
Write-Host ""
Write-Host "使用例:" -ForegroundColor Yellow
Write-Host "  dotnet jb inspectcode src\GistGet.slnx -o=inspectcode-report.xml" -ForegroundColor Gray
Write-Host "  dotnet jb inspectcode src\GistGet.slnx -o=inspectcode-report.sarif --format=sarif" -ForegroundColor Gray
Write-Host ""
Write-Host "コードインスペクションを実行するには:" -ForegroundColor Cyan
Write-Host "  .\scripts\Run-ReSharperInspect.ps1" -ForegroundColor White
Write-Host ""
