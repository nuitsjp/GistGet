# Setup pre-commit hooks for GistGet project
# Run this script to enable pre-commit format checking

Write-Host "Setting up pre-commit hooks..." -ForegroundColor Yellow

# Check if we're in a git repository
if (-not (Test-Path ".git")) {
    Write-Host "❌ This script must be run from the root of a git repository" -ForegroundColor Red
    exit 1
}

# Check if dotnet is available
try {
    dotnet --version | Out-Null
} catch {
    Write-Host "❌ dotnet CLI is not installed or not in PATH" -ForegroundColor Red
    exit 1
}

# Install dotnet-format if not already installed
$formatInstalled = dotnet tool list -g | Select-String "dotnet-format"
if (-not $formatInstalled) {
    Write-Host "Installing dotnet-format..." -ForegroundColor Yellow
    dotnet tool install -g dotnet-format
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Failed to install dotnet-format" -ForegroundColor Red
        exit 1
    }
}

# Copy pre-commit hook template and enable git hooks
$templatePath = "scripts\hooks\pre-commit.ps1"
if (-not (Test-Path $templatePath)) {
    Write-Host "❌ Pre-commit template not found: $templatePath" -ForegroundColor Red
    exit 1
}

# Copy the template to .git/hooks/
$hookPath = ".git\hooks\pre-commit.ps1"
Copy-Item $templatePath $hookPath -Force
Write-Host "✅ Pre-commit script copied from template" -ForegroundColor Green

if ($IsWindows -or $env:OS -like "*Windows*") {
    # On Windows, create a simple hook that directly calls PowerShell
    $hookContent = "#!/bin/sh`npowershell.exe -ExecutionPolicy Bypass -File `"`$(dirname `$0)/pre-commit.ps1`""
    Set-Content -Path ".git\hooks\pre-commit" -Value $hookContent
    
    Write-Host "✅ Pre-commit hooks enabled (Windows)" -ForegroundColor Green
} else {
    # On Unix-like systems, create a shell script wrapper
    $shellWrapper = "#!/bin/sh`npwsh -File `"`$(dirname `"`$0`")/pre-commit.ps1`""
    Set-Content -Path ".git/hooks/pre-commit" -Value $shellWrapper
    chmod +x ".git/hooks/pre-commit"
    Write-Host "✅ Pre-commit hooks enabled (Unix)" -ForegroundColor Green
}

Write-Host ""
Write-Host "🎉 Setup complete!" -ForegroundColor Green
Write-Host "Pre-commit hooks will now run automatically before each commit." -ForegroundColor White
Write-Host "To manually check formatting, run: .\scripts\format-check.ps1" -ForegroundColor White
Write-Host "To fix formatting issues, run: .\scripts\format-fix.ps1" -ForegroundColor White
