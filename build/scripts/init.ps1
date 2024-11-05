param($installPath, $toolsPath, $package)

$moduleRoot = Join-Path $installPath "tools"
if (Test-Path (Join-Path $moduleRoot "GistGet.psd1")) {
    Import-Module (Join-Path $moduleRoot "GistGet.psd1") -Force
}