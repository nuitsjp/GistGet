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

# Create module directory structure
$modulePath = Join-Path -Path $OutputPath -ChildPath 'GistGet'
$toolsPath = Join-Path -Path $modulePath -ChildPath 'tools'
New-Item -ItemType Directory -Path $modulePath | Out-Null
New-Item -ItemType Directory -Path $toolsPath | Out-Null

# モジュールマニフェストの更新
$manifestPath = Join-Path $projectRoot 'src/GistGet.psd1'
$manifest = Import-PowerShellDataFile -Path $manifestPath

# 更新された依存モジュール定義を作成
$requiredModulesString = "@(
    @{
        ModuleName = 'powershell-yaml'
        ModuleVersion = '0.4.7'
    },
    @{
        ModuleName = 'PowerShellForGitHub'
        ModuleVersion = '0.17.0'
    },
    @{
        ModuleName = 'Microsoft.WinGet.Client'
        ModuleVersion = '1.6.2'
    }
)"

# マニフェストの内容を更新
$manifestContent = Get-Content $manifestPath -Raw
$manifestContent = $manifestContent -replace "RequiredModules\s*=\s*@\([^)]+\)", "RequiredModules = $requiredModulesString"

# 新しいマニフェストファイルを作成
$tempManifestPath = Join-Path $modulePath 'GistGet.psd1'
$manifestContent | Set-Content $tempManifestPath -Encoding UTF8

# Copy module files
$filesToCopy = @(
    (Join-Path $projectRoot 'src/GistGet.psm1')
    (Join-Path $projectRoot 'src/Classes.ps1')
)

foreach ($file in $filesToCopy) {
    Copy-Item -Path $file -Destination $modulePath
}

# Private/Publicスクリプトファイルをtoolsフォルダにコピー
$sourceFolders = @(
    (Join-Path $projectRoot 'src/Private'),
    (Join-Path $projectRoot 'src/Public')
)

foreach ($folder in $sourceFolders) {
    if (Test-Path $folder) {
        Get-ChildItem -Path $folder -Filter *.ps1 | ForEach-Object {
            $destinationPath = Join-Path $toolsPath $_.Name
            Copy-Item -Path $_.FullName -Destination $destinationPath
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

# Create module package
$nuspecPath = Join-Path -Path $OutputPath -ChildPath 'GistGet.nuspec'
$nuspecContent = @"
<?xml version="1.0" encoding="utf-8"?>
<package xmlns="http://schemas.microsoft.com/packaging/2011/08/nuspec.xsd">
    <metadata>
        <id>GistGet</id>
        <version>$($manifest.ModuleVersion)</version>
        <authors>$($manifest.Author)</authors>
        <owners>$($manifest.Author)</owners>
        <requireLicenseAcceptance>false</requireLicenseAcceptance>
        <description>$($manifest.Description)</description>
        <projectUrl>$($manifest.PrivateData.PSData.ProjectUri)</projectUrl>
        <license type="expression">MIT</license>
        <releaseNotes>$($manifest.PrivateData.PSData.ReleaseNotes)</releaseNotes>
        <tags>$($manifest.PrivateData.PSData.Tags -join ' ')</tags>
        <dependencies>
            <group targetFramework=".NETStandard2.0">
                <dependency id="powershell-yaml" version="0.4.7" />
                <dependency id="PowerShellForGitHub" version="0.17.0" />
                <dependency id="Microsoft.WinGet.Client" version="1.6.2" />
            </group>
        </dependencies>
    </metadata>
    <files>
        <file src="GistGet\**" target="tools" />
        <file src="..\README.md" target="docs\" />
        <file src="..\LICENSE" target="tools\" />
    </files>
</package>
"@

$nuspecContent | Out-File -FilePath $nuspecPath -Encoding UTF8 -NoNewline

Write-Host "Build completed successfully"