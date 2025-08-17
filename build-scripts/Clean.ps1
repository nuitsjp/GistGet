# Clean Build Artifacts Script

[CmdletBinding()]
param(
    [switch]$IncludeCoverage = $true,
    [switch]$IncludeInspection = $true,
    [switch]$IncludeReports = $true,
    [switch]$ShowDetails = $false
)

Write-Host "Cleaning build artifacts..." -ForegroundColor Green

try {
    $cleanedItems = @()
    
    # Remove bin and obj directories
    Write-Host "Removing bin and obj directories..." -ForegroundColor Yellow
    $binDirs = Get-ChildItem -Path . -Recurse -Directory -Name "bin" -ErrorAction SilentlyContinue
    $objDirs = Get-ChildItem -Path . -Recurse -Directory -Name "obj" -ErrorAction SilentlyContinue
    
    foreach ($dir in $binDirs) {
        $fullPath = Join-Path (Get-Location) $dir
        if (Test-Path $fullPath) {
            Remove-Item $fullPath -Recurse -Force
            $cleanedItems += "bin: $fullPath"
            if ($ShowDetails) {
                Write-Host "  Removed: $fullPath" -ForegroundColor Gray
            }
        }
    }
    
    foreach ($dir in $objDirs) {
        $fullPath = Join-Path (Get-Location) $dir
        if (Test-Path $fullPath) {
            Remove-Item $fullPath -Recurse -Force
            $cleanedItems += "obj: $fullPath"
            if ($ShowDetails) {
                Write-Host "  Removed: $fullPath" -ForegroundColor Gray
            }
        }
    }
    
    # Remove coverage files and reports
    if ($IncludeCoverage) {
        Write-Host "Removing coverage artifacts..." -ForegroundColor Yellow
        
        # Remove coverage report directory
        if (Test-Path "coverage-report") {
            Remove-Item "coverage-report" -Recurse -Force
            $cleanedItems += "Coverage report directory"
            if ($Verbose) {
                Write-Host "  Removed: coverage-report" -ForegroundColor Gray
            }
        }
        
        # Remove coverage files from test directories
        $coverageFiles = Get-ChildItem -Path . -Recurse -Filter "coverage.*.xml" -ErrorAction SilentlyContinue
        foreach ($file in $coverageFiles) {
            Remove-Item $file.FullName -Force
            $cleanedItems += "Coverage file: $($file.Name)"
            if ($Verbose) {
                Write-Host "  Removed: $($file.FullName)" -ForegroundColor Gray
            }
        }
        
        # Remove TestResults directories
        $testResultsDirs = Get-ChildItem -Path . -Recurse -Directory -Name "TestResults" -ErrorAction SilentlyContinue
        foreach ($dir in $testResultsDirs) {
            $fullPath = (Get-ChildItem -Path . -Recurse -Directory | Where-Object {$_.Name -eq "TestResults"}).FullName
            if ($fullPath) {
                foreach ($path in $fullPath) {
                    Remove-Item $path -Recurse -Force
                    $cleanedItems += "TestResults: $path"
                    if ($ShowDetails) {
                        Write-Host "  Removed: $path" -ForegroundColor Gray
                    }
                }
            }
        }
    }
    
    # Remove inspection files
    if ($IncludeInspection) {
        Write-Host "Removing inspection artifacts..." -ForegroundColor Yellow
        
        if (Test-Path "inspection-results.xml") {
            Remove-Item "inspection-results.xml" -Force
            $cleanedItems += "Inspection results file"
            if ($ShowDetails) {
                Write-Host "  Removed: inspection-results.xml" -ForegroundColor Gray
            }
        }
    }
    
    # Remove .reports directory
    if ($IncludeReports) {
        Write-Host "Removing .reports directory..." -ForegroundColor Yellow
        
        if (Test-Path ".reports") {
            Remove-Item ".reports" -Recurse -Force
            $cleanedItems += ".reports directory"
            if ($ShowDetails) {
                Write-Host "  Removed: .reports" -ForegroundColor Gray
            }
        }
    }
    
    # Summary
    Write-Host ""
    Write-Host "Clean completed successfully!" -ForegroundColor Green
    Write-Host "Cleaned $($cleanedItems.Count) items." -ForegroundColor Green
    
    if ($ShowDetails -and $cleanedItems.Count -gt 0) {
        Write-Host ""
        Write-Host "Summary of cleaned items:" -ForegroundColor Cyan
        foreach ($item in $cleanedItems) {
            Write-Host "  - $item" -ForegroundColor Gray
        }
    }
}
catch {
    Write-Error "An error occurred during cleanup: $($_.Exception.Message)"
    exit 1
}