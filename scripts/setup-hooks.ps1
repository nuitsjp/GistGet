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

# Enable git hooks (Windows needs this due to filesystem permissions)
if ($IsWindows -or $env:OS -like "*Windows*") {
    # On Windows, make sure the PowerShell script can be executed
    $hookPath = ".git\hooks\pre-commit.ps1"
    if (Test-Path $hookPath) {
        # Create a batch file wrapper for the PowerShell script
        $batchContent = "@echo off`npowershell.exe -ExecutionPolicy Bypass -File `"%~dp0pre-commit.ps1`""
        Set-Content -Path ".git\hooks\pre-commit.bat" -Value $batchContent
        
        # Create the main pre-commit hook that calls the batch file
        $hookContent = "#!/bin/sh`n`"./pre-commit.bat`""
        Set-Content -Path ".git\hooks\pre-commit" -Value $hookContent
        
        Write-Host "✅ Pre-commit hooks enabled (Windows)" -ForegroundColor Green
    } else {
        Write-Host "❌ pre-commit.ps1 not found" -ForegroundColor Red
        exit 1
    }
} else {
    # On Unix-like systems, make the shell script executable
    $hookPath = ".git/hooks/pre-commit"
    if (Test-Path $hookPath) {
        chmod +x $hookPath
        Write-Host "✅ Pre-commit hooks enabled (Unix)" -ForegroundColor Green
    } else {
        Write-Host "❌ pre-commit hook not found" -ForegroundColor Red
        exit 1
    }
}

Write-Host ""
Write-Host "🎉 Setup complete!" -ForegroundColor Green
Write-Host "Pre-commit hooks will now run automatically before each commit." -ForegroundColor White
Write-Host "To manually check formatting, run: .\scripts\format-check.ps1" -ForegroundColor White
Write-Host "To fix formatting issues, run: .\scripts\format-fix.ps1" -ForegroundColor White
