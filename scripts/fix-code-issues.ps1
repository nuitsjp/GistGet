# Automated Code Issue Fix Script

[CmdletBinding()]
param(
    [Parameter(Mandatory=$false)]
    [string]$SolutionPath,
    
    [Parameter(Mandatory=$false)]
    [ValidateSet("RedundantUsings", "PrimaryConstructors", "InitOnlyProperties", "All")]
    [string]$FixType = "RedundantUsings",
    
    [Parameter(Mandatory=$false)]
    [switch]$DryRun = $false,
    
    [Parameter(Mandatory=$false)]
    [switch]$RunInspectionAfter = $true
)

Write-Host "Automated Code Issue Fix Script" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan

# Find solution file if not specified
if (-not $SolutionPath) {
    $solutionFiles = Get-ChildItem -Filter "*.sln" -Path (Get-Location)
    if ($solutionFiles.Count -eq 0) {
        Write-Error "No solution file found in current directory. Please specify -SolutionPath parameter."
        exit 1
    }
    elseif ($solutionFiles.Count -gt 1) {
        Write-Host "Multiple solution files found:" -ForegroundColor Yellow
        $solutionFiles | ForEach-Object { Write-Host "  $($_.Name)" -ForegroundColor Gray }
        $SolutionPath = $solutionFiles[0].Name
        Write-Host "Using: $SolutionPath" -ForegroundColor Yellow
    }
    else {
        $SolutionPath = $solutionFiles[0].Name
    }
}

if (-not (Test-Path $SolutionPath)) {
    Write-Error "Solution file not found: $SolutionPath"
    exit 1
}

Write-Host "Solution: $SolutionPath" -ForegroundColor Green
Write-Host "Fix Type: $FixType" -ForegroundColor Green
Write-Host "Dry Run: $DryRun" -ForegroundColor Green
Write-Host ""

try {
    # Check if dotnet format is available
    $dotnetFormatPath = Get-Command dotnet -ErrorAction SilentlyContinue
    if (-not $dotnetFormatPath) {
        Write-Error "dotnet CLI not found. Please install .NET SDK."
        exit 1
    }
    
    # Check if ReSharper CLI is available
    $jbPath = Get-Command jb -ErrorAction SilentlyContinue
    if (-not $jbPath) {
        Write-Error "ReSharper CLI (jb) not found. Please install JetBrains.ReSharper.GlobalTools."
        exit 1
    }
    
    Write-Host "Prerequisites check passed." -ForegroundColor Green
    Write-Host ""
    
    switch ($FixType) {
        "RedundantUsings" {
            Write-Host "Fixing redundant using directives..." -ForegroundColor Yellow
            
            if ($DryRun) {
                Write-Host "DRY RUN: Would execute: dotnet format $SolutionPath --include-generated --verbosity diagnostic" -ForegroundColor Cyan
            }
            else {
                # Use dotnet format to remove unused usings
                Write-Host "Running dotnet format to remove unused usings..." -ForegroundColor Green
                & dotnet format $SolutionPath --include-generated --verbosity diagnostic
                
                if ($LASTEXITCODE -eq 0) {
                    Write-Host "✓ Unused using directives removed successfully." -ForegroundColor Green
                }
                else {
                    Write-Warning "dotnet format completed with warnings. Exit code: $LASTEXITCODE"
                }
            }
        }
        
        "PrimaryConstructors" {
            Write-Host "Converting to primary constructors..." -ForegroundColor Yellow
            
            if ($DryRun) {
                Write-Host "DRY RUN: Would execute ReSharper cleanup with primary constructor pattern" -ForegroundColor Cyan
            }
            else {
                # Use ReSharper CLI to apply code cleanup
                Write-Host "Running ReSharper cleanup for primary constructors..." -ForegroundColor Green
                & jb cleanupcode $SolutionPath --verbosity=WARN
                
                if ($LASTEXITCODE -eq 0) {
                    Write-Host "✓ Code cleanup completed successfully." -ForegroundColor Green
                }
                else {
                    Write-Warning "ReSharper cleanup completed with warnings. Exit code: $LASTEXITCODE"
                }
            }
        }
        
        "InitOnlyProperties" {
            Write-Host "Converting properties to init-only..." -ForegroundColor Yellow
            Write-Host "Note: This requires manual review as it may break existing code." -ForegroundColor Yellow
            
            # Read inspection results to find specific properties
            $inspectionFile = "inspection-results.xml"
            if (Test-Path $inspectionFile) {
                [xml]$results = Get-Content $inspectionFile -Encoding UTF8
                
                $initOnlyIssues = @()
                foreach ($project in $results.Report.Issues.Project) {
                    if ($project.Issue) {
                        $initOnlyIssues += $project.Issue | Where-Object { $_.TypeId -like "*PropertyCanBeMadeInitOnly*" }
                    }
                }
                
                if ($initOnlyIssues.Count -gt 0) {
                    Write-Host "Found $($initOnlyIssues.Count) properties that can be made init-only:" -ForegroundColor Cyan
                    
                    $groupedByFile = $initOnlyIssues | Group-Object File | Sort-Object Name
                    foreach ($fileGroup in $groupedByFile) {
                        Write-Host ""
                        Write-Host "File: $($fileGroup.Name)" -ForegroundColor White
                        foreach ($issue in $fileGroup.Group) {
                            $line = if ($issue.Line) { "Line $($issue.Line)" } else { "Offset $($issue.Offset)" }
                            Write-Host "  $line - $($issue.Message)" -ForegroundColor Gray
                        }
                    }
                    
                    Write-Host ""
                    Write-Host "Manual intervention required for init-only properties." -ForegroundColor Yellow
                    Write-Host "Please review each property to ensure it doesn't break existing functionality." -ForegroundColor Yellow
                }
                else {
                    Write-Host "No init-only property issues found." -ForegroundColor Green
                }
            }
            else {
                Write-Warning "Inspection results file not found. Please run code inspection first."
            }
        }
        
        "All" {
            Write-Host "Applying all automatic fixes..." -ForegroundColor Yellow
            
            # Run each fix type sequentially
            & $PSCommandPath -SolutionPath $SolutionPath -FixType "RedundantUsings" -DryRun:$DryRun -RunInspectionAfter:$false
            Write-Host ""
            & $PSCommandPath -SolutionPath $SolutionPath -FixType "PrimaryConstructors" -DryRun:$DryRun -RunInspectionAfter:$false
            Write-Host ""
            & $PSCommandPath -SolutionPath $SolutionPath -FixType "InitOnlyProperties" -DryRun:$DryRun -RunInspectionAfter:$false
        }
    }
    
    # Run inspection after fixes if requested
    if ($RunInspectionAfter -and -not $DryRun -and $FixType -ne "InitOnlyProperties") {
        Write-Host ""
        Write-Host "Running code inspection to verify fixes..." -ForegroundColor Yellow
        
        $inspectionScript = Join-Path $PSScriptRoot "run-code-inspection.ps1"
        if (Test-Path $inspectionScript) {
            & $inspectionScript -SolutionPath $SolutionPath -ShowSummary
        }
        else {
            Write-Warning "Code inspection script not found at: $inspectionScript"
        }
    }
    
    Write-Host ""
    Write-Host "Code fix process completed." -ForegroundColor Green
}
catch {
    Write-Error "An error occurred during code fixing: $($_.Exception.Message)"
    exit 1
}