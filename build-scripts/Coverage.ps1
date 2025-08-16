# Coverage Report Generation Script

[CmdletBinding()]
param(
    [string]$ReportDirectory = "coverage-report",
    [string]$ReportTypes = "Html;TextSummary",
    [switch]$ShowSummary = $true
)

Write-Host "Generating coverage report..." -ForegroundColor Green

try {
    # Find the latest coverage file
    $coverageFiles = Get-ChildItem -Path "tests" -Recurse -Filter "coverage.cobertura.xml" | Sort-Object LastWriteTime -Descending
    
    if ($coverageFiles.Count -eq 0) {
        Write-Warning "No coverage files found. Tests may not have run with coverage collection."
        Write-Host "Run tests with coverage first: .\build-scripts\Test.ps1 -CollectCoverage" -ForegroundColor Yellow
        exit 1
    }
    
    $latestCoverage = $coverageFiles[0].FullName
    Write-Host "Found coverage file: $latestCoverage" -ForegroundColor Yellow
    
    # Create coverage report directory
    if (Test-Path $ReportDirectory) {
        Remove-Item $ReportDirectory -Recurse -Force
    }
    New-Item -ItemType Directory -Path $ReportDirectory -Force | Out-Null
    
    # Generate HTML report
    reportgenerator -reports:$latestCoverage -targetdir:$ReportDirectory -reporttypes:$ReportTypes
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Coverage report generated successfully in: $ReportDirectory" -ForegroundColor Green
        
        # Display text summary if requested
        if ($ShowSummary) {
            $summaryFile = Join-Path $ReportDirectory "Summary.txt"
            if (Test-Path $summaryFile) {
                Write-Host "`nCoverage Summary:" -ForegroundColor Cyan
                Get-Content $summaryFile | Write-Host
            }
        }
    }
    else {
        Write-Error "Failed to generate coverage report. Exit code: $LASTEXITCODE"
        exit $LASTEXITCODE
    }
}
catch {
    Write-Error "An error occurred during coverage report generation: $($_.Exception.Message)"
    exit 1
}