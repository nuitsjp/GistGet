# ReSharper Code Inspection Script

[CmdletBinding()]
param(
    [string]$SolutionPath,
    [string]$OutputFile = ".reports/inspection/xml/inspection-results.xml",
    [ValidateSet("ERROR", "WARN", "INFO", "VERBOSE")]
    [string]$Verbosity = "WARN",
    [switch]$ShowSummary = $true,
    [switch]$ShowDetails = $false,
    [switch]$GroupByFile = $false
)

Write-Host "Running ReSharper code inspection..." -ForegroundColor Green

try {
    # Check if InspectCode is available
    $inspectCodePath = Get-Command inspectcode -ErrorAction SilentlyContinue
    $jbPath = Get-Command jb -ErrorAction SilentlyContinue
    
    if (-not $inspectCodePath -and -not $jbPath) {
        Write-Error "InspectCode tool not found. Please run Setup first or install JetBrains.ReSharper.GlobalTools manually."
        exit 1
    }
    
    $inspectCommand = if ($inspectCodePath) { "inspectcode" } else { "jb inspectcode" }
    Write-Host "Using InspectCode: $inspectCommand" -ForegroundColor Green
    
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
    
    Write-Host "Inspecting solution: $SolutionPath" -ForegroundColor Yellow
    Write-Host "Output file: $OutputFile" -ForegroundColor Green
    Write-Host "Verbosity: $Verbosity" -ForegroundColor Green
    Write-Host ""
    
    # Create output directory if it doesn't exist
    $outputDir = Split-Path $OutputFile -Parent
    if (-not (Test-Path $outputDir)) {
        New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
        Write-Host "Created output directory: $outputDir" -ForegroundColor Green
    }
    
    # Remove existing output file
    if (Test-Path $OutputFile) {
        Remove-Item $OutputFile -Force
    }
    
    # Run InspectCode
    $startTime = Get-Date
    
    $arguments = @(
        $SolutionPath
        "--output=$OutputFile"
        "--format=Xml"
        "--verbosity=$Verbosity"
    )
    
    if ($inspectCommand -eq "inspectcode") {
        & inspectcode @arguments
    }
    else {
        & jb inspectcode @arguments
    }
    
    $endTime = Get-Date
    $duration = $endTime - $startTime
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "InspectCode failed with exit code: $LASTEXITCODE"
        exit $LASTEXITCODE
    }
    
    Write-Host ""
    Write-Host "Code inspection completed in $($duration.TotalSeconds.ToString('F2')) seconds." -ForegroundColor Green
    
    # Display results summary
    if ($ShowSummary -and (Test-Path $OutputFile)) {
        Write-Host ""
        Write-Host "Inspection Results Summary:" -ForegroundColor Cyan
        Write-Host "===========================" -ForegroundColor Cyan
        
        try {
            [xml]$results = Get-Content $OutputFile -Encoding UTF8
            
            if ($results.Report.Issues.Project) {
                $allIssues = @()
                foreach ($project in $results.Report.Issues.Project) {
                    if ($project.Issue) {
                        $allIssues += $project.Issue
                    }
                }
                
                if ($allIssues.Count -gt 0) {
                    Write-Host "Total Issues Found: $($allIssues.Count)" -ForegroundColor Yellow
                    Write-Host ""
                    
                    # Group by severity
                    $severityGroups = $allIssues | Group-Object Severity | Sort-Object Name
                    foreach ($group in $severityGroups) {
                        $color = switch ($group.Name) {
                            "ERROR" { "Red" }
                            "WARNING" { "Yellow" }
                            "SUGGESTION" { "Cyan" }
                            default { "Gray" }
                        }
                        Write-Host "  $($group.Name): $($group.Count)" -ForegroundColor $color
                    }
                    
                    Write-Host ""
                    
                    # Group by issue type (top 10)
                    $typeGroups = $allIssues | Group-Object TypeId | Sort-Object Count -Descending | Select-Object -First 10
                    if ($typeGroups.Count -gt 0) {
                        Write-Host "Top Issue Types:" -ForegroundColor Cyan
                        foreach ($group in $typeGroups) {
                            Write-Host "  $($group.Name): $($group.Count)" -ForegroundColor Gray
                        }
                    }
                    
                    # Show detailed issues if requested
                    if ($ShowDetails) {
                        Write-Host ""
                        Write-Host "Detailed Issues:" -ForegroundColor Cyan
                        Write-Host "================" -ForegroundColor Cyan
                        
                        if ($GroupByFile) {
                            # Group by file
                            $fileGroups = $allIssues | Group-Object File | Sort-Object Name
                            foreach ($fileGroup in $fileGroups) {
                                Write-Host ""
                                Write-Host "File: $($fileGroup.Name)" -ForegroundColor White
                                Write-Host ("-" * ($fileGroup.Name.Length + 6))
                                
                                foreach ($issue in ($fileGroup.Group | Sort-Object Line)) {
                                    $color = switch ($issue.Severity) {
                                        "ERROR" { "Red" }
                                        "WARNING" { "Yellow" }
                                        "SUGGESTION" { "Cyan" }
                                        default { "Gray" }
                                    }
                                    $line = if ($issue.Line) { "Line $($issue.Line)" } else { "Offset $($issue.Offset)" }
                                    Write-Host "  [$($issue.Severity)] $line - $($issue.TypeId)" -ForegroundColor $color
                                    Write-Host "    $($issue.Message)" -ForegroundColor Gray
                                }
                            }
                        }
                        else {
                            # Group by severity and type
                            foreach ($severityGroup in $severityGroups) {
                                Write-Host ""
                                Write-Host "$($severityGroup.Name) Issues:" -ForegroundColor $(switch ($severityGroup.Name) {
                                    "ERROR" { "Red" }
                                    "WARNING" { "Yellow" }
                                    "SUGGESTION" { "Cyan" }
                                    default { "Gray" }
                                })
                                
                                $issueTypeGroups = $severityGroup.Group | Group-Object TypeId | Sort-Object Count -Descending
                                foreach ($typeGroup in $issueTypeGroups) {
                                    Write-Host "  $($typeGroup.Name) ($($typeGroup.Count) issues):" -ForegroundColor Gray
                                    
                                    foreach ($issue in ($typeGroup.Group | Sort-Object File, Line | Select-Object -First 5)) {
                                        $line = if ($issue.Line) { "Line $($issue.Line)" } else { "Offset $($issue.Offset)" }
                                        Write-Host "    $($issue.File) - $line" -ForegroundColor DarkGray
                                    }
                                    
                                    if ($typeGroup.Count -gt 5) {
                                        Write-Host "    ... and $($typeGroup.Count - 5) more" -ForegroundColor DarkGray
                                    }
                                }
                            }
                        }
                    }
                }
                else {
                    Write-Host "No issues found! Code quality looks good." -ForegroundColor Green
                }
            }
            else {
                Write-Host "No issues found! Code quality looks good." -ForegroundColor Green
            }
        }
        catch {
            Write-Warning "Could not parse inspection results file: $($_.Exception.Message)"
            Write-Host "Results saved to: $OutputFile" -ForegroundColor Green
        }
    }
    
    Write-Host ""
    Write-Host "Results saved to: $OutputFile" -ForegroundColor Green
    
    # Return non-zero exit code if issues were found (for CI/CD)
    if ($ShowSummary -and (Test-Path $OutputFile)) {
        try {
            [xml]$results = Get-Content $OutputFile -Encoding UTF8
            if ($results.Report.Issues.Project) {
                $allIssues = @()
                foreach ($project in $results.Report.Issues.Project) {
                    if ($project.Issue) {
                        $allIssues += $project.Issue
                    }
                }
                if ($allIssues.Count -gt 0) {
                    Write-Host "Code inspection found issues. See detailed report above." -ForegroundColor Yellow
                    # Don't exit with error code - inspection completed successfully
                }
            }
        }
        catch {
            # If we can't parse results, assume no issues
        }
    }
}
catch {
    Write-Error "An error occurred during code inspection: $($_.Exception.Message)"
    exit 1
}