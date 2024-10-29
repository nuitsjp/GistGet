[CmdletBinding()]
param (
    [Parameter()]
    [string]$Configuration = 'Release',
    [Parameter()]
    [string]$OutputPath = (Join-Path $PSScriptRoot 'Output')
)

# スクリプトのルートディレクトリを取得
$projectRoot = Split-Path -Parent $PSScriptRoot

# Clean output directory
if (Test-Path -Path $OutputPath) {
    Remove-Item -Path $OutputPath -Recurse -Force
}
New-Item -ItemType Directory -Path $OutputPath | Out-Null

# Create module directory
$modulePath = Join-Path -Path $OutputPath -ChildPath 'GistGet'
New-Item -ItemType Directory -Path $modulePath | Out-Null

# Copy module files
$filesToCopy = @(
    (Join-Path $projectRoot 'src/GistGet.psd1')
    (Join-Path $projectRoot 'src/GistGet.psm1')
    (Join-Path $projectRoot 'src/Classes.ps1')
    (Join-Path $projectRoot 'src/Public')
    (Join-Path $projectRoot 'src/Private')
)

foreach ($file in $filesToCopy) {
    Copy-Item -Path $file -Destination $modulePath -Recurse
}

# Run tests
Write-Host "Running Pester tests..."
$testPath = Join-Path $projectRoot 'test'
$testResults = Invoke-Pester -Path $testPath -PassThru

if ($testResults.FailedCount -gt 0) {
    throw "Tests failed"
}

# Create module package
$nuspecPath = Join-Path -Path $OutputPath -ChildPath 'GistGet.nuspec'
$manifestPath = Join-Path $projectRoot 'src/GistGet.psd1'
$manifest = Import-PowerShellDataFile -Path $manifestPath

$nuspecContent = @"
<?xml version="1.0"?>
<package xmlns="http://schemas.microsoft.com/packaging/2011/08/nuspec.xsd">
    <metadata>
        <id>GistGet</id>
        <version>$($manifest.ModuleVersion)</version>
        <authors>$($manifest.Author)</authors>
        <owners>$($manifest.Author)</owners>
        <description>$($manifest.Description)</description>
        <projectUrl>$($manifest.PrivateData.PSData.ProjectUri)</projectUrl>
        <licenseUrl>$($manifest.PrivateData.PSData.LicenseUri)</licenseUrl>
        <tags>PowerShell Module WinGet GitHub Gist</tags>
    </metadata>
</package>
"@

$nuspecContent | Out-File -FilePath $nuspecPath -Encoding UTF8

Write-Host "Build completed successfully"