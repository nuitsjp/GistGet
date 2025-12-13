<#
.SYNOPSIS
    Run all tests for the GistGet project.

.DESCRIPTION
    This script runs all tests (unit and integration) in the GistGet.Tests project with coverage collection.
    Integration tests require authentication via Run-AuthLogin.ps1 before running.
    If not authenticated, integration tests will be skipped automatically.

.PARAMETER Configuration
    Build configuration (Debug or Release). Default is Debug.

.PARAMETER CollectCoverage
    Whether to collect code coverage. Default is true.

.EXAMPLE
    .\Run-Tests.ps1
    .\Run-Tests.ps1 -Configuration Release
    .\Run-Tests.ps1 -CollectCoverage:$false
#>

param(
    [string]$Configuration = "Debug",
    [switch]$CollectCoverage = $true,
    [double]$CoverageThreshold = 95
)

$ErrorActionPreference = "Stop"

# Get repository root path
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptPath
$runSettingsPath = Join-Path $repoRoot "coverlet.runsettings"
$platform = "x64"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Running GistGet Tests" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration" -ForegroundColor Yellow
Write-Host "Collect Coverage: $CollectCoverage" -ForegroundColor Yellow
Write-Host "Coverage Threshold: $CoverageThreshold%" -ForegroundColor Yellow
Write-Host "Platform: $platform" -ForegroundColor Yellow
Write-Host ""

# Build the solution first
Write-Host "Building solution..." -ForegroundColor Green
dotnet build "$repoRoot\src\GistGet.slnx" -c $Configuration -p:Platform=$platform
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit $LASTEXITCODE
}

# Run tests
Write-Host ""
Write-Host "Running tests..." -ForegroundColor Green

$testArgs = @(
    "test",
    "$repoRoot\src\GistGet.Test\GistGet.Test.csproj",
    "-c", $Configuration,
    "--no-build",
    "-p:Platform=$platform",
    "--verbosity", "normal"
)

if ($CollectCoverage) {
    $testArgs += @(
        "--collect", "XPlat Code Coverage",
        "--results-directory", "$repoRoot\TestResults",
        "--settings", $runSettingsPath
    )
}

& dotnet $testArgs

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "Tests failed!" -ForegroundColor Red
    exit $LASTEXITCODE
}

if ($CollectCoverage) {
    Write-Host ""
    Write-Host "Coverage results saved to: $repoRoot\TestResults\" -ForegroundColor Yellow
    Write-Host "Analyzing coverage..." -ForegroundColor Green
    $analyzerScript = Join-Path $repoRoot "scripts/Analyze-Coverage.ps1"
    & $analyzerScript -ResultsDirectory "$repoRoot\TestResults" -Threshold $CoverageThreshold
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "All tests passed successfully!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan

exit 0
