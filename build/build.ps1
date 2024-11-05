[CmdletBinding()]
param (
    [Parameter()]
    [string]$Configuration = 'Release',
    [Parameter()]
    [string]$OutputPath = (Join-Path $PSScriptRoot 'Output')
)

# スクリプトのルートディレクトリを取得
$projectRoot = Split-Path -Parent $PSScriptRoot

# モジュールのバージョン情報
$moduleVersion = "1.0.0"

# Clean output directory
if (Test-Path -Path $OutputPath) {
    Remove-Item -Path $OutputPath -Recurse -Force
}
New-Item -ItemType Directory -Path $OutputPath | Out-Null

# モジュールディレクトリ構造の作成
$modulePath = Join-Path -Path $OutputPath -ChildPath 'GistGet'
$moduleToolsPath = Join-Path -Path $modulePath -ChildPath 'tools'
$moduleContentPath = Join-Path $modulePath 'content'

# ツールフォルダとサブフォルダの作成
foreach ($path in @($modulePath, $moduleToolsPath, $moduleContentPath)) {
    New-Item -ItemType Directory -Path $path -Force | Out-Null
}

# Copy installation scripts
$scriptsPath = Join-Path $PSScriptRoot 'scripts'
@('install.ps1', 'uninstall.ps1', 'init.ps1') | ForEach-Object {
    $scriptPath = Join-Path $scriptsPath $_
    if (Test-Path $scriptPath) {
        Copy-Item -Path $scriptPath -Destination $moduleToolsPath
    }
    else {
        Write-Warning "Installation script not found: $_"
    }
}

# モジュールファイルのコピー
$moduleFiles = @('GistGet.psd1', 'GistGet.psm1', 'Classes.ps1')
foreach ($file in $moduleFiles) {
    Copy-Item -Path (Join-Path $projectRoot "src\$file") -Destination $moduleToolsPath
}

# Create function folders in tools directory
$toolsPublicPath = Join-Path $moduleToolsPath 'Public'
$toolsPrivatePath = Join-Path $moduleToolsPath 'Private'
New-Item -ItemType Directory -Path $toolsPublicPath -Force | Out-Null
New-Item -ItemType Directory -Path $toolsPrivatePath -Force | Out-Null

# Copy Public and Private functions
foreach ($folder in @(
    @{ Source = 'Public'; Target = $toolsPublicPath },
    @{ Source = 'Private'; Target = $toolsPrivatePath }
)) {
    $sourcePath = Join-Path $projectRoot "src\$($folder.Source)"
    if (Test-Path $sourcePath) {
        Get-ChildItem -Path $sourcePath -Filter "*.ps1" | ForEach-Object {
            Copy-Item -Path $_.FullName -Destination $folder.Target
        }
    }
}

# Run tests
Write-Host "Running Pester tests..."
$testPath = Join-Path $projectRoot 'test'
$testResults = Invoke-Pester -Path $testPath -PassThru

if ($testResults.FailedCount -gt 0) {
    throw "Tests failed"
}

# Create nuspec file
$nuspecPath = Join-Path -Path $OutputPath -ChildPath 'GistGet.nuspec'
$nuspecContent = @"
<?xml version="1.0" encoding="utf-8"?>
<package xmlns="http://schemas.microsoft.com/packaging/2011/08/nuspec.xsd">
    <metadata>
        <id>GistGet</id>
        <version>$moduleVersion</version>
        <authors>nuits.jp</authors>
        <owners>nuits.jp</owners>
        <requireLicenseAcceptance>false</requireLicenseAcceptance>
        <description>PowerShell module to manage WinGet package lists on Gist or Web or File.</description>
        <projectUrl>https://github.com/nuitsjp/GistGet</projectUrl>
        <readme>docs\README.md</readme>
        <license type="expression">MIT</license>
        <tags>WinGet GitHub Gist Package Management</tags>
    </metadata>
    <files>
        <file src="GistGet\**" target="tools" exclude="**\*.pdb"/>
        <file src="..\README.md" target="docs\" />
        <file src="..\LICENSE" target="content\" />
    </files>
</package>
"@

$nuspecContent | Out-File -FilePath $nuspecPath -Encoding UTF8

Write-Host "Build completed successfully"