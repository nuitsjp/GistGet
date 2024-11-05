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
$requiredModules = @{
    'powershell-yaml' = '0.4.7'
    'PowerShellForGitHub' = '0.17.0'
    'Microsoft.WinGet.Client' = '1.6.2'
}

# Clean output directory
if (Test-Path -Path $OutputPath) {
    Remove-Item -Path $OutputPath -Recurse -Force
}
New-Item -ItemType Directory -Path $OutputPath | Out-Null

# モジュールディレクトリ構造の作成
$modulePath = Join-Path -Path $OutputPath -ChildPath 'GistGet'
$modulePrivatePath = Join-Path -Path $modulePath -ChildPath 'Private'
$modulePublicPath = Join-Path -Path $modulePath -ChildPath 'Public'

foreach ($path in @($modulePath, $modulePrivatePath, $modulePublicPath)) {
    New-Item -ItemType Directory -Path $path -Force | Out-Null
}

# モジュールファイルのコピー
$files = @{
    'GistGet.psd1' = $modulePath
    'GistGet.psm1' = $modulePath
    'Classes.ps1' = $modulePath
}

foreach ($file in $files.Keys) {
    $sourcePath = Join-Path $projectRoot 'src' $file
    $destPath = $files[$file]
    Copy-Item -Path $sourcePath -Destination $destPath
}

# Public/Private関数をコピー
$functionFolders = @(
    @{ Source = 'Private'; Target = $modulePrivatePath }
    @{ Source = 'Public'; Target = $modulePublicPath }
)

foreach ($folder in $functionFolders) {
    $sourcePath = Join-Path $projectRoot 'src' $folder.Source
    if (Test-Path $sourcePath) {
        Copy-Item -Path "$sourcePath\*.ps1" -Destination $folder.Target
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
        <license type="expression">MIT</license>
        <tags>WinGet GitHub Gist Package Management</tags>
        <dependencies>
            <dependency id="powershell-yaml" version="$($requiredModules['powershell-yaml'])" />
            <dependency id="PowerShellForGitHub" version="$($requiredModules['PowerShellForGitHub'])" />
            <dependency id="Microsoft.WinGet.Client" version="$($requiredModules['Microsoft.WinGet.Client'])" />
        </dependencies>
    </metadata>
    <files>
        <file src="GistGet\**\*.*" target="tools" />
        <file src="..\README.md" target="content" />
        <file src="..\LICENSE" target="content" />
    </files>
</package>
"@

$nuspecContent | Out-File -FilePath $nuspecPath -Encoding UTF8

Write-Host "Build completed successfully"