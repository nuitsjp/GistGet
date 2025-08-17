# Test Execution Script

[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$Verbosity = "normal",
    [switch]$NoBuild = $true,
    [switch]$CollectCoverage = $true,
    [string]$TestResultsDirectory = ".reports/test-results/trx"
)

Write-Host "Running tests..." -ForegroundColor Green

try {
    # Create test results directory if it doesn't exist
    if (-not (Test-Path $TestResultsDirectory)) {
        New-Item -ItemType Directory -Path $TestResultsDirectory -Force | Out-Null
        Write-Host "Created test results directory: $TestResultsDirectory" -ForegroundColor Green
    }
    
    $testArgs = @(
        "--configuration", $Configuration
        "--verbosity", $Verbosity
        "--results-directory", $TestResultsDirectory
        "--logger", "trx"
    )
    
    if ($NoBuild) {
        $testArgs += "--no-build"
    }
    
    if ($CollectCoverage) {
        $testArgs += "--collect:`"XPlat Code Coverage`""
    }
    
    # Run tests
    dotnet test @testArgs
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Tests completed successfully." -ForegroundColor Green
    }
    else {
        Write-Error "Tests failed with exit code: $LASTEXITCODE"
        exit $LASTEXITCODE
    }
}
catch {
    Write-Error "An error occurred during test execution: $($_.Exception.Message)"
    exit 1
}