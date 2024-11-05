[CmdletBinding()]
param (
    [Parameter()]
    [switch]$WhatIf
)

. $PSScriptRoot\common.ps1

$logPath = Join-Path $global:projectRoot 'logs'
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

# API Keyの取得と検証
Write-Log "Checking API Key..."
$apiKey = [Environment]::GetEnvironmentVariable('GIST_GET_API_KEY', 'User')
if ([string]::IsNullOrEmpty($apiKey)) {
    throw "API Key not found. Please set the GIST_GET_API_KEY environment variable."
}

# ビルドの実行
Write-Log "Executing build script..."
& $global:buildScript
if ($LASTEXITCODE -ne 0) {
    throw "Build failed with exit code $LASTEXITCODE"
}

# モジュールのバージョンを取得
$version = Get-LatestReleaseVersion
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
    Path = $global:modulePath
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