[CmdletBinding()]
param (
    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    
    [Parameter()]
    [switch]$WhatIf
)

# デフォルトエンコーディングをUTF-8に設定
[System.Console]::OutputEncoding = [System.Text.Encoding]::UTF8
[System.Console]::InputEncoding = [System.Text.Encoding]::UTF8
$PSDefaultParameterValues['*:Encoding'] = 'utf8'
$OutputEncoding = [System.Text.Encoding]::UTF8

$ErrorActionPreference = 'Stop'
$VerbosePreference = 'Continue'

# スクリプトのルートディレクトリを取得
$projectRoot = Split-Path -Parent $PSScriptRoot
$buildScript = Join-Path $projectRoot 'build' 'build.ps1'
$outputPath = Join-Path $projectRoot 'build' 'Output'
$modulePath = Join-Path $outputPath 'GistGet'
$logPath = Join-Path $projectRoot 'logs'
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$logFile = Join-Path $logPath "publish_$timestamp.log"

function Write-Log {
    param(
        [string]$Message,
        [string]$Level = 'INFO'
    )
    
    $logMessage = "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')|$Level|$Message"
    Write-Verbose $logMessage
    Add-Content -Path $logFile -Value $logMessage
}

# ログディレクトリの作成
if (-not (Test-Path $logPath)) {
    New-Item -ItemType Directory -Path $logPath | Out-Null
}

Write-Log "Starting module publishing process"
Write-Log "Configuration: $Configuration"

# API Keyの取得と検証
Write-Log "Checking API Key..."
$apiKey = [Environment]::GetEnvironmentVariable('GIST_GET_API_KEY', 'User')
if ([string]::IsNullOrEmpty($apiKey)) {
    throw "API Key not found. Please set the GIST_GET_API_KEY environment variable."
}

# ビルドの実行
Write-Log "Executing build script..."
& $buildScript -Configuration $Configuration -OutputPath $outputPath
if ($LASTEXITCODE -ne 0) {
    throw "Build failed with exit code $LASTEXITCODE"
}

# モジュールマニフェストの読み込みと検証
Write-Log "Loading module manifest..."
$manifestPath = Join-Path $modulePath 'GistGet.psd1'
$manifest = Test-ModuleManifest -Path $manifestPath -ErrorAction Stop
$version = $manifest.Version
Write-Log "Module version: $version"

# 既存のモジュールのチェック
Write-Log "Checking existing module version..."
$existingModule = Find-Module -Name 'GistGet' -ErrorAction SilentlyContinue
if ($existingModule -and $existingModule.Version -ge $version) {
    throw "Module version $version already exists in PowerShell Gallery. Please update the version number in $manifestPath"
}

# モジュールの公開
Write-Log "Publishing module..."
$publishParams = @{
    Path = $modulePath
    NuGetApiKey = $apiKey
    Verbose = $true
    ErrorAction = 'Stop'
    Force = $true
}

if (-not $WhatIf) {
    Publish-Module @publishParams
    Write-Log "Module published successfully!" -Level 'SUCCESS'
} else {
    Publish-Module @publishParams -WhatIf
    Write-Log "WhatIf: Module would be published with version $version"
}

Write-Host "Module published successfully!" -ForegroundColor Green
Write-Host "Version: $version"
Write-Host "Package: GistGet"