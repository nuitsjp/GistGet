[CmdletBinding()]
param (
    [Parameter()]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

# スクリプトのルートディレクトリを取得
$projectRoot = Split-Path -Parent $PSScriptRoot
$buildScript = Join-Path $projectRoot 'build' 'build.ps1'
$outputPath = Join-Path $projectRoot 'build' 'Output'
$modulePath = Join-Path $outputPath 'GistGet'

try {
    # API Keyの取得
    $apiKey = [Environment]::GetEnvironmentVariable('GIST_GET_API_KEY', 'User')
    if ([string]::IsNullOrEmpty($apiKey)) {
        throw "API Key not found. Please set the GIST_GET_API_KEY environment variable."
    }

    # ビルドの実行
    Write-Host "Building module..."
    & $buildScript -Configuration $Configuration -OutputPath $outputPath
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed with exit code $LASTEXITCODE"
    }

    # マニフェストファイルの読み込み
    $manifestPath = Join-Path $modulePath 'GistGet.psd1'
    $manifest = Import-PowerShellDataFile -Path $manifestPath
    $version = $manifest.ModuleVersion

    # 既存のモジュールのチェック
    Write-Host "Checking existing module version..."
    $existingModule = Find-Module -Name 'GistGet' -ErrorAction SilentlyContinue
    if ($existingModule -and $existingModule.Version -ge [Version]$version) {
        throw "Module version $version already exists in PowerShell Gallery. Please update the version number in $manifestPath"
    }

    # モジュールの公開
    Write-Host "Publishing module version $version..."
    # Publish-Module -Path $modulePath -NuGetApiKey $apiKey -Verbose

    Write-Host "Module published successfully!" -ForegroundColor Green
    Write-Host "Version: $version"
    Write-Host "Package: GistGet"
    Write-Host "Project: $(Join-Path $projectRoot 'src')"
    Write-Host "Output: $outputPath"
    Write-Host "Please verify the module is available at: https://www.powershellgallery.com/packages/GistGet"
}
catch {
    Write-Error "Publishing failed: $_"
    exit 1
}