# GistGet Build Script using Invoke-Build

# Default task
task . Build, Test, Coverage, CodeInspection

# Setup task - Install required tools
task Setup {
    Write-Host "Setting up build environment..." -ForegroundColor Green
    
    # Check and install JetBrains.ReSharper.GlobalTools
    $resharperTool = dotnet tool list --global | Select-String "jetbrains.resharper.globaltools"
    if (-not $resharperTool) {
        Write-Host "Installing JetBrains.ReSharper.GlobalTools..." -ForegroundColor Yellow
        exec { dotnet tool install --global JetBrains.ReSharper.GlobalTools }
        Write-Host "JetBrains.ReSharper.GlobalTools installed successfully." -ForegroundColor Green
    }
    else {
        Write-Host "JetBrains.ReSharper.GlobalTools is already installed." -ForegroundColor Green
    }
    
    # Check and install ReportGenerator for coverage reports
    $reportGenerator = dotnet tool list --global | Select-String "dotnet-reportgenerator-globaltool"
    if (-not $reportGenerator) {
        Write-Host "Installing ReportGenerator..." -ForegroundColor Yellow
        exec { dotnet tool install --global dotnet-reportgenerator-globaltool }
        Write-Host "ReportGenerator installed successfully." -ForegroundColor Green
    }
    else {
        Write-Host "ReportGenerator is already installed." -ForegroundColor Green
    }
}

# Build task
task Build {
    Write-Host "Building solution..." -ForegroundColor Green
    exec { dotnet build --configuration Release --verbosity minimal }
}

# Test task
task Test Build, {
    Write-Host "Running tests..." -ForegroundColor Green
    exec { dotnet test --configuration Release --no-build --verbosity normal --collect:"XPlat Code Coverage" }
}

# Coverage task - Generate coverage report
task Coverage Test, {
    Write-Host "Generating coverage report..." -ForegroundColor Green
    
    # Find the latest coverage file
    $coverageFiles = Get-ChildItem -Path "tests" -Recurse -Filter "coverage.cobertura.xml" | Sort-Object LastWriteTime -Descending
    
    if ($coverageFiles.Count -gt 0) {
        $latestCoverage = $coverageFiles[0].FullName
        Write-Host "Found coverage file: $latestCoverage" -ForegroundColor Yellow
        
        # Create coverage report directory
        $reportDir = "coverage-report"
        if (Test-Path $reportDir) {
            Remove-Item $reportDir -Recurse -Force
        }
        New-Item -ItemType Directory -Path $reportDir -Force | Out-Null
        
        # Generate HTML report
        exec { reportgenerator -reports:$latestCoverage -targetdir:$reportDir -reporttypes:"Html;TextSummary" }
        
        Write-Host "Coverage report generated in: $reportDir" -ForegroundColor Green
        
        # Display text summary
        $summaryFile = Join-Path $reportDir "Summary.txt"
        if (Test-Path $summaryFile) {
            Write-Host "`nCoverage Summary:" -ForegroundColor Cyan
            Get-Content $summaryFile | Write-Host
        }
    }
    else {
        Write-Warning "No coverage files found. Tests may not have run with coverage collection."
    }
}

# Code Inspection task using ReSharper
task CodeInspection Build, {
    Write-Host "Running ReSharper code inspection..." -ForegroundColor Green
    
    # Find solution file
    $solutionFile = Get-ChildItem -Filter "*.sln" | Select-Object -First 1
    if (-not $solutionFile) {
        throw "Solution file not found"
    }
    
    $outputFile = "inspection-results.xml"
    Write-Host "Inspecting solution: $($solutionFile.Name)" -ForegroundColor Yellow
    
    # Run InspectCode
    $inspectCodePath = Get-Command inspectcode -ErrorAction SilentlyContinue
    if ($inspectCodePath) {
        exec { inspectcode $solutionFile.Name --output=$outputFile --format=Xml --verbosity=WARN }
    }
    else {
        exec { jb inspectcode $solutionFile.Name --output=$outputFile --format=Xml --verbosity=WARN }
    }
    
    if (Test-Path $outputFile) {
        Write-Host "Code inspection completed. Results saved to: $outputFile" -ForegroundColor Green
        
        # Parse and display summary
        [xml]$results = Get-Content $outputFile
        $issues = $results.Report.Issues.Project.Issue
        if ($issues) {
            $issueCount = $issues.Count
            Write-Host "`nCode Issues Found: $issueCount" -ForegroundColor $(if ($issueCount -gt 0) { 'Yellow' } else { 'Green' })
            
            # Group by severity
            $groupedIssues = $issues | Group-Object TypeId
            foreach ($group in $groupedIssues) {
                Write-Host "  $($group.Name): $($group.Count)" -ForegroundColor Gray
            }
        }
        else {
            Write-Host "`nNo code issues found!" -ForegroundColor Green
        }
    }
    else {
        Write-Warning "Inspection results file not found"
    }
}

# Clean task
task Clean {
    Write-Host "Cleaning build artifacts..." -ForegroundColor Green
    
    # Remove bin and obj directories
    Get-ChildItem -Path . -Recurse -Directory -Name "bin" | Remove-Item -Recurse -Force
    Get-ChildItem -Path . -Recurse -Directory -Name "obj" | Remove-Item -Recurse -Force
    
    # Remove coverage and inspection files
    if (Test-Path "coverage-report") { Remove-Item "coverage-report" -Recurse -Force }
    if (Test-Path "inspection-results.xml") { Remove-Item "inspection-results.xml" -Force }
    
    Write-Host "Clean completed." -ForegroundColor Green
}

# Full build with setup
task Full Setup, Build, Test, Coverage, CodeInspection

# Help task
task Help {
    Write-Host "`nAvailable tasks:" -ForegroundColor Cyan
    Write-Host "  Setup         - Install required tools (ReSharper CLI, ReportGenerator)" -ForegroundColor White
    Write-Host "  Build         - Build the solution" -ForegroundColor White
    Write-Host "  Test          - Run tests with coverage collection" -ForegroundColor White
    Write-Host "  Coverage      - Generate coverage report" -ForegroundColor White
    Write-Host "  CodeInspection- Run ReSharper code inspection" -ForegroundColor White
    Write-Host "  Clean         - Clean build artifacts" -ForegroundColor White
    Write-Host "  Full          - Setup + Build + Test + Coverage + CodeInspection" -ForegroundColor White
    Write-Host "  Help          - Show this help" -ForegroundColor White
    Write-Host "`nDefault task runs: Build, Test, Coverage, CodeInspection" -ForegroundColor Yellow
    Write-Host "`nUsage examples:" -ForegroundColor Cyan
    Write-Host "  Invoke-Build" -ForegroundColor White
    Write-Host "  Invoke-Build Build" -ForegroundColor White
    Write-Host "  Invoke-Build Full" -ForegroundColor White
}