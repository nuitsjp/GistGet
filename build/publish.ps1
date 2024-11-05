[CmdletBinding()]
param (
    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    
    [Parameter()]
    [switch]$WhatIf
)

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

function Test-ModuleStructure {
    param(
        [string]$ModulePath
    )

    Write-Log "Checking module structure at $ModulePath"

    # 必要なファイルの存在確認
    $requiredFiles = @(
        'GistGet.psd1',
        'GistGet.psm1'
    )

    foreach ($file in $requiredFiles) {
        $filePath = Join-Path $ModulePath $file
        if (-not (Test-Path $filePath)) {
            throw "Required file not found: $file"
        }
    }

    # マニフェストの検証
    $manifest = Import-PowerShellDataFile -Path (Join-Path $ModulePath 'GistGet.psd1')
    
    # 依存関係のバージョン確認
    if ($manifest.RequiredModules) {
        foreach ($module in $manifest.RequiredModules) {
            if ($module -is [string]) {
                Write-Log "WARNING: Module $module should specify required version" -Level 'WARN'
            }
        }
    }

    # プライベートフォルダとパブリックフォルダの構造確認
    $folders = @('Private', 'Public')
    foreach ($folder in $folders) {
        $folderPath = Join-Path $ModulePath $folder
        if (-not (Test-Path $folderPath)) {
            Write-Log "WARNING: Recommended folder structure not found: $folder" -Level 'WARN'
        }
    }

    return $true
}

try {
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
    
    # モジュール構造のチェック
    if (-not (Test-ModuleStructure -ModulePath $modulePath)) {
        throw "Module structure validation failed"
    }

    # マニフェストの読み込みと検証
    Write-Log "Loading module manifest..."
    $manifestPath = Join-Path $modulePath 'GistGet.psd1'
    $manifest = Import-PowerShellDataFile -Path $manifestPath
    $version = $manifest.ModuleVersion
    Write-Log "Module version: $version"
    
    # 既存のモジュールのチェック
    Write-Log "Checking existing module version..."
    $existingModule = Find-Module -Name 'GistGet' -ErrorAction SilentlyContinue
    if ($existingModule -and $existingModule.Version -ge [Version]$version) {
        throw "Module version $version already exists in PowerShell Gallery. Please update the version number in $manifestPath"
    }
    
    # モジュールの公開
    Write-Log "Publishing module..."
    if (-not $WhatIf) {
        $publishParams = @{
            Path = $modulePath
            NuGetApiKey = $apiKey
            Verbose = $true
            ErrorAction = 'Stop'
            Force = $true
        }
        
        Publish-Module @publishParams
        Write-Log "Module published successfully!" -Level 'SUCCESS'
    } else {
        Write-Log "WhatIf: Module would be published with version $version"
    }
    
    Write-Host "Module published successfully!" -ForegroundColor Green
    Write-Host "Version: $version"
    Write-Host "Package: GistGet"
}
catch {
    $errorMessage = "Publishing failed: $($_.Exception.Message)"
    Write-Log $errorMessage -Level 'ERROR'
    Write-Log $_.Exception.StackTrace -Level 'ERROR'
    Write-Error $errorMessage
    exit 1
}