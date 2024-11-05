[CmdletBinding()]
param (
    [Parameter()]
    [string]$Configuration = 'Release',
    [Parameter()]
    [string]$OutputPath = (Join-Path $PSScriptRoot 'Output')
)

# モジュールのバージョン情報
$moduleVersion = "1.0.0"

# スクリプトのルートディレクトリを取得
$projectRoot = Split-Path -Parent $PSScriptRoot

# Run tests
Write-Host "Running Pester tests..."
$testPath = Join-Path $projectRoot 'test'
$testResults = Invoke-Pester -Path $testPath -PassThru

if ($testResults.FailedCount -gt 0) {
    throw "Tests failed"
}


# Clean output directory
if (Test-Path -Path $OutputPath) {
    Remove-Item -Path $OutputPath -Recurse -Force
}
New-Item -ItemType Directory -Path $OutputPath | Out-Null

# モジュールディレクトリ構造の作成
$modulePath = Join-Path -Path $OutputPath -ChildPath 'GistGet'
$srcPath = Join-Path -Path $projectRoot -ChildPath 'src'

Copy-Item -Path $srcPath -Destination $modulePath -Recurse -Force

# Create nuspec file
$nuspecPath = Join-Path -Path $modulePath -ChildPath 'GistGet.nuspec'
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