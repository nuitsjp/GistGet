[CmdletBinding()]
param (
)

. $PSScriptRoot\common.ps1

$ModuleVersion = Get-LatestReleaseVersion

# Run tests
Write-Host "Running Pester tests..."
$testPath = Join-Path $global:projectRoot 'test'
$testResults = Invoke-Pester -Path $testPath -PassThru

if ($testResults.FailedCount -gt 0) {
    throw "Tests failed"
}


# Clean output directory
if (Test-Path -Path $global:outputPath) {
    Remove-Item -Path $global:outputPath -Recurse -Force
}
New-Item -ItemType Directory -Path $global:outputPath | Out-Null

# モジュールファイルのコピー
Copy-Item -Path $global:srcPath -Destination $global:modulePath -Recurse -Force

# モジュールのバージョン情報を更新
$moduleManifestPath = Join-Path -Path $global:modulePath -ChildPath 'GistGet.psd1'
(Get-Content -Path $moduleManifestPath) -replace 'ModuleVersion = ''1.0.0''', "ModuleVersion = '$ModuleVersion'" | Set-Content -Path $moduleManifestPath

# Create nuspec file
$nuspecPath = Join-Path -Path $global:modulePath -ChildPath 'GistGet.nuspec'
$nuspecContent = @"
<?xml version="1.0" encoding="utf-8"?>
<package xmlns="http://schemas.microsoft.com/packaging/2011/08/nuspec.xsd">
    <metadata>
        <id>GistGet</id>
        <version>$ModuleVersion</version>
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