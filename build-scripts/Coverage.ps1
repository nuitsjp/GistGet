# Coverage Report Generation Script

[CmdletBinding()]
param(
    [string]$ReportDirectory = ".reports/coverage/html",
    [string]$ReportTypes = "Html;TextSummary",
    [switch]$ShowSummary = $true
)

Write-Host "Generating coverage report..." -ForegroundColor Green

try {
    # Find the latest coverage file
    $searchPaths = @("src", ".reports")
    $coverageFiles = @()
    foreach ($path in $searchPaths) {
        if (Test-Path $path) {
            $files = Get-ChildItem -Path $path -Recurse -Filter "coverage.cobertura.xml" -ErrorAction SilentlyContinue
            $coverageFiles += $files
        }
    }
    $coverageFiles = $coverageFiles | Sort-Object LastWriteTime -Descending
    
    if ($coverageFiles.Count -eq 0) {
        Write-Warning "No coverage files found. Tests may not have run with coverage collection."
        Write-Host "Run tests with coverage first: .\build-scripts\Test.ps1 -CollectCoverage" -ForegroundColor Yellow
        exit 1
    }
    
    $latestCoverage = $coverageFiles[0].FullName
    Write-Host "Found coverage file: $latestCoverage" -ForegroundColor Yellow
    
    # Copy coverage XML to dedicated xml directory
    $xmlDir = ".reports/coverage/xml"
    if (-not (Test-Path $xmlDir)) {
        New-Item -ItemType Directory -Path $xmlDir -Force | Out-Null
    }
    $xmlFileName = "coverage.cobertura.xml"
    Copy-Item $latestCoverage (Join-Path $xmlDir $xmlFileName) -Force
    Write-Host "Coverage XML copied to: $xmlDir\$xmlFileName" -ForegroundColor Green
    
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
                
                # Copy summary to dedicated summary directory
                $summaryDir = ".reports/coverage/summary"
                if (-not (Test-Path $summaryDir)) {
                    New-Item -ItemType Directory -Path $summaryDir -Force | Out-Null
                }
                Copy-Item $summaryFile (Join-Path $summaryDir "Summary.txt") -Force
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