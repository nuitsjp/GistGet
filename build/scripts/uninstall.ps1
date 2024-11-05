param($installPath, $toolsPath, $package, $project)

$ErrorActionPreference = 'Stop'
$moduleName = "GistGet"

# Get module path
$paths = $env:PSModulePath -split ';' | Where-Object { $_ -like "*$env:USERNAME*" }
if ($paths.Count -gt 0) {
    $moduleDestination = Join-Path $paths[0] $moduleName
    if (Test-Path $moduleDestination) {
        Remove-Item -Path $moduleDestination -Recurse -Force
    }
}