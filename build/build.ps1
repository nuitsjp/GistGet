[CmdletBinding()]
param (
    [Parameter()]
    [string]$Configuration = 'Release',
    [Parameter()]
    [string]$OutputPath = './Output'
)

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
    'src/GistGet.psd1'
    'src/GistGet.psm1'
    'src/Classes.ps1'
    'src/Public'
    'src/Private'
)

foreach ($file in $filesToCopy) {
    Copy-Item -Path $file -Destination $modulePath -Recurse
}

# Run tests
Write-Host "Running Pester tests..."
$testResults = Invoke-Pester -Path "./test" -PassThru

if ($testResults.FailedCount -gt 0) {
    throw "Tests failed"
}

# Create module package
$nuspecPath = Join-Path -Path $OutputPath -ChildPath 'GistGet.nuspec'
$manifest = Import-PowerShellDataFile -Path 'src/GistGet.psd1'

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