# Setup Build Environment Script
# Installs required tools for the build process

[CmdletBinding()]
param(
    [switch]$Force = $false
)

Write-Host "Setting up build environment..." -ForegroundColor Green

try {
    # Check and install JetBrains.ReSharper.GlobalTools
    $resharperTool = dotnet tool list --global | Select-String "jetbrains.resharper.globaltools"
    if (-not $resharperTool -or $Force) {
        if ($resharperTool -and $Force) {
            Write-Host "Force updating JetBrains.ReSharper.GlobalTools..." -ForegroundColor Yellow
            dotnet tool update --global JetBrains.ReSharper.GlobalTools
        }
        else {
            Write-Host "Installing JetBrains.ReSharper.GlobalTools..." -ForegroundColor Yellow
            dotnet tool install --global JetBrains.ReSharper.GlobalTools
        }
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "JetBrains.ReSharper.GlobalTools installed successfully." -ForegroundColor Green
        }
        else {
            Write-Error "Failed to install JetBrains.ReSharper.GlobalTools. Exit code: $LASTEXITCODE"
            exit $LASTEXITCODE
        }
    }
    else {
        Write-Host "JetBrains.ReSharper.GlobalTools is already installed." -ForegroundColor Green
    }
    
    # Check and install ReportGenerator for coverage reports
    $reportGenerator = dotnet tool list --global | Select-String "dotnet-reportgenerator-globaltool"
    if (-not $reportGenerator -or $Force) {
        if ($reportGenerator -and $Force) {
            Write-Host "Force updating ReportGenerator..." -ForegroundColor Yellow
            dotnet tool update --global dotnet-reportgenerator-globaltool
        }
        else {
            Write-Host "Installing ReportGenerator..." -ForegroundColor Yellow
            dotnet tool install --global dotnet-reportgenerator-globaltool
        }
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "ReportGenerator installed successfully." -ForegroundColor Green
        }
        else {
            Write-Error "Failed to install ReportGenerator. Exit code: $LASTEXITCODE"
            exit $LASTEXITCODE
        }
    }
    else {
        Write-Host "ReportGenerator is already installed." -ForegroundColor Green
    }
    
    # Check and install dotnet-format
    $dotnetFormat = dotnet tool list --global | Select-String "dotnet-format"
    if (-not $dotnetFormat -or $Force) {
        if ($dotnetFormat -and $Force) {
            Write-Host "Force updating dotnet-format..." -ForegroundColor Yellow
            dotnet tool update --global dotnet-format
        }
        else {
            Write-Host "Installing dotnet-format..." -ForegroundColor Yellow
            dotnet tool install --global dotnet-format
        }
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "dotnet-format installed successfully." -ForegroundColor Green
        }
        else {
            Write-Error "Failed to install dotnet-format. Exit code: $LASTEXITCODE"
            exit $LASTEXITCODE
        }
    }
    else {
        Write-Host "dotnet-format is already installed." -ForegroundColor Green
    }
    
    Write-Host ""
    Write-Host "Build environment setup completed successfully!" -ForegroundColor Green
}
catch {
    Write-Error "An error occurred during setup: $($_.Exception.Message)"
    exit 1
}