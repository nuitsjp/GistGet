param($installPath, $toolsPath, $package, $project)

$ErrorActionPreference = 'Stop'
$moduleSource = Join-Path $installPath "tools"
$moduleName = "GistGet"

# Get all module destination paths
$paths = $env:PSModulePath -split ';' | Where-Object { $_ -like "*$env:USERNAME*" }
if ($paths.Count -eq 0) {
    throw "No suitable PowerShell module path found"
}

$moduleDestination = Join-Path $paths[0] $moduleName
if (-not (Test-Path $moduleDestination)) {
    New-Item -ItemType Directory -Path $moduleDestination | Out-Null
}

# Copy module files
Copy-Item -Path "$moduleSource\*" -Destination $moduleDestination -Recurse -Force -Exclude "*.nuspec", "*.nupkg"