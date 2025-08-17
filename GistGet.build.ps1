# GistGet Build Script using Invoke-Build

# Default task
task . Help

# Setup task - Install required tools
task Setup {
    exec { powershell -File "build-scripts\Setup.ps1" }
}

# Build task
task Build {
    exec { powershell -File "build-scripts\Build.ps1" -Configuration Release -Verbosity minimal }
}

# Test task
task Test Build, {
    exec { powershell -File "build-scripts\Test.ps1" -Configuration Release -Verbosity normal -NoBuild -CollectCoverage }
}

# Coverage task - Generate coverage report
task Coverage Test, {
    exec { powershell -File "build-scripts\Coverage.ps1" -ShowSummary }
}

# Code Inspection task using ReSharper
task CodeInspection Build, {
    exec { powershell -File "build-scripts\CodeInspection.ps1" -ShowSummary }
}

# Clean task
task Clean {
    exec { powershell -File "build-scripts\Clean.ps1" -IncludeCoverage -IncludeInspection -IncludeReports }
}

# Format check task
task FormatCheck {
    exec { powershell -File "build-scripts\Format.ps1" -CheckOnly }
}

# Format fix task
task FormatFix {
    exec { powershell -File "build-scripts\Format.ps1" -Fix }
}

# Full build with setup
task Full Setup, Build, Test, Coverage, CodeInspection

# Help task
task Help {
    Write-Host "`nAvailable tasks:" -ForegroundColor Cyan
    Write-Host "  Setup         - Install required tools (ReSharper CLI, ReportGenerator, dotnet-format)" -ForegroundColor White
    Write-Host "  Build         - Build the solution" -ForegroundColor White
    Write-Host "  Test          - Run tests with coverage collection" -ForegroundColor White
    Write-Host "  Coverage      - Generate coverage report" -ForegroundColor White
    Write-Host "  CodeInspection- Run ReSharper code inspection" -ForegroundColor White
    Write-Host "  FormatCheck   - Check code formatting without making changes" -ForegroundColor White
    Write-Host "  FormatFix     - Apply code formatting fixes" -ForegroundColor White
    Write-Host "  Clean         - Clean build artifacts" -ForegroundColor White
    Write-Host "  Full          - Setup + Build + Test + Coverage + CodeInspection" -ForegroundColor White
    Write-Host "  Help          - Show this help" -ForegroundColor White
    Write-Host "`nDefault task runs: Help" -ForegroundColor Yellow
    Write-Host "`nUsage examples:" -ForegroundColor Cyan
    Write-Host "  Invoke-Build" -ForegroundColor White
    Write-Host "  Invoke-Build Build" -ForegroundColor White
    Write-Host "  Invoke-Build FormatCheck" -ForegroundColor White
    Write-Host "  Invoke-Build Full" -ForegroundColor White
}