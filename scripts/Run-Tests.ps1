<#
.SYNOPSIS
    Run all tests for the GistGet project.

.DESCRIPTION
    This script runs all unit tests in the GistGet.Tests project with coverage collection.

.PARAMETER Configuration
    Build configuration (Debug or Release). Default is Debug.

.PARAMETER CollectCoverage
    Whether to collect code coverage. Default is true.

.EXAMPLE
    .\Run-Tests.ps1
    .\Run-Tests.ps1 -Configuration Release
    .\Run-Tests.ps1 -CollectCoverage $false
#>

param(
    [string]$Configuration = "Debug",
    [bool]$CollectCoverage = $true
)

$ErrorActionPreference = "Stop"

# Get repository root path
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptPath

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Running GistGet Tests" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration" -ForegroundColor Yellow
Write-Host "Collect Coverage: $CollectCoverage" -ForegroundColor Yellow
Write-Host ""

# Build the solution first
Write-Host "Building solution..." -ForegroundColor Green
dotnet build "$repoRoot\GistGet.sln" -c $Configuration
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit $LASTEXITCODE
}

# Run tests
Write-Host ""
Write-Host "Running tests..." -ForegroundColor Green

$testArgs = @(
    "test",
    "$repoRoot\src\GistGet.Tests\GistGet.Tests.csproj",
    "-c", $Configuration,
    "--no-build",
    "--verbosity", "normal"
)

if ($CollectCoverage) {
    $testArgs += @(
        "--collect:XPlat Code Coverage",
        "--results-directory", "$repoRoot\TestResults"
    )
}

& dotnet $testArgs

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "Tests failed!" -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "All tests passed successfully!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan

if ($CollectCoverage) {
    Write-Host ""
    Write-Host "Coverage results saved to: $repoRoot\TestResults\" -ForegroundColor Yellow
}

exit 0
