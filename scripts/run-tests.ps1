# GistGet Sandbox テスト実行スクリプト
param(
    [switch]$SkipInteractive,
    [switch]$BasicOnly
)

$ErrorActionPreference = "Stop"

function Write-TestResult {
    param([string]$TestName, [bool]$Success, [string]$Details = "")
    $status = if ($Success) { "✅ PASS" } else { "❌ FAIL" }
    $color = if ($Success) { "Green" } else { "Red" }
    
    Write-Host "$status $TestName" -ForegroundColor $color
    if ($Details) {
        Write-Host "   $Details" -ForegroundColor Gray
    }
}

function Test-Command {
    param([string]$Command, [string[]]$Arguments = @())
    
    try {
        $output = & $Command @Arguments 2>&1
        $success = $LASTEXITCODE -eq 0
        return @{
            Success = $success
            Output = $output
            ExitCode = $LASTEXITCODE
        }
    } catch {
        return @{
            Success = $false
            Output = $_.Exception.Message
            ExitCode = -1
        }
    }
}

Write-Host "🧪 Starting GistGet Sandbox Tests..." -ForegroundColor Cyan
Write-Host "=" * 50 -ForegroundColor Cyan

# 前提条件の確認
Write-Host "`n📋 Prerequisites Check:" -ForegroundColor Yellow

# WinGet確認
$wingetTest = Test-Command "winget" @("--version")
Write-TestResult "WinGet availability" $wingetTest.Success $wingetTest.Output

# .NET確認
$dotnetTest = Test-Command "dotnet" @("--version")
Write-TestResult ".NET Runtime availability" $dotnetTest.Success $dotnetTest.Output

# GistGet.exe確認
$gistgetExists = Test-Path "C:\GistGet\GistGet.exe"
Write-TestResult "GistGet.exe exists" $gistgetExists

if (-not $gistgetExists) {
    Write-Host "❌ GistGet.exe not found. Cannot proceed with tests." -ForegroundColor Red
    exit 1
}

# 基本コマンドテスト
Write-Host "`n🔧 Basic Command Tests:" -ForegroundColor Yellow

Set-Location "C:\GistGet"

# Help コマンド
$helpTest = Test-Command ".\GistGet.exe" @("--help")
Write-TestResult "Help command" $helpTest.Success

# List コマンド
$listTest = Test-Command ".\GistGet.exe" @("list")
Write-TestResult "List command" $listTest.Success

# Search コマンド
$searchTest = Test-Command ".\GistGet.exe" @("search", "git")
Write-TestResult "Search command" $searchTest.Success

if ($BasicOnly) {
    Write-Host "`n✨ Basic tests completed!" -ForegroundColor Green
    return
}

# WinGetマニフェストテスト
Write-Host "`n📦 WinGet Manifest Tests:" -ForegroundColor Yellow

$manifestPath = "C:\Manifests\NuitsJp\GistGet\1.0.0"
$manifestExists = Test-Path $manifestPath
Write-TestResult "Manifest files exist" $manifestExists

if ($manifestExists) {
    # マニフェスト検証
    $validateTest = Test-Command "winget" @("validate", $manifestPath)
    Write-TestResult "Manifest validation" $validateTest.Success $validateTest.Output
    
    if (-not $SkipInteractive) {
        Write-Host "`n🤔 Would you like to test WinGet installation? (y/N): " -NoNewline -ForegroundColor Yellow
        $response = Read-Host
        
        if ($response -eq 'y' -or $response -eq 'Y') {
            Write-Host "`n📥 Testing WinGet installation..." -ForegroundColor Yellow
            $installTest = Test-Command "winget" @("install", "--manifest", $manifestPath, "--accept-source-agreements", "--accept-package-agreements")
            Write-TestResult "WinGet manifest installation" $installTest.Success $installTest.Output
            
            if ($installTest.Success) {
                # インストール後のテスト
                $gistgetTest = Test-Command "gistget" @("--help")
                Write-TestResult "Installed GistGet functionality" $gistgetTest.Success
            }
        }
    }
}

# 認証テスト（対話的）
if (-not $SkipInteractive) {
    Write-Host "`n🔐 Authentication Tests (Interactive):" -ForegroundColor Yellow
    
    Write-Host "🤔 Would you like to test authentication? (y/N): " -NoNewline -ForegroundColor Yellow
    $response = Read-Host
    
    if ($response -eq 'y' -or $response -eq 'Y') {
        Write-Host "Testing login command..." -ForegroundColor Yellow
        $loginTest = Test-Command ".\GistGet.exe" @("login")
        Write-TestResult "Login command execution" $loginTest.Success
        
        Write-Host "Testing gist status..." -ForegroundColor Yellow
        $statusTest = Test-Command ".\GistGet.exe" @("gist", "status")
        Write-TestResult "Gist status command" $statusTest.Success
    }
}

# サイレントモードテスト
Write-Host "`n🔇 Silent Mode Tests:" -ForegroundColor Yellow

$silentTest = Test-Command ".\GistGet.exe" @("--silent", "list")
Write-TestResult "Silent mode flag" $silentTest.Success

# テスト結果サマリー
Write-Host "`n" + "=" * 50 -ForegroundColor Cyan
Write-Host "🏁 Test Summary Complete!" -ForegroundColor Green
Write-Host "Check the results above for any failed tests." -ForegroundColor Gray
Write-Host "`nFor manual testing, you can run:" -ForegroundColor Yellow
Write-Host "  .\GistGet.exe --help" -ForegroundColor Gray
Write-Host "  .\GistGet.exe login" -ForegroundColor Gray
Write-Host "  .\GistGet.exe gist set" -ForegroundColor Gray
Write-Host "  .\GistGet.exe --silent install Git.Git" -ForegroundColor Gray