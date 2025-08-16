# Code Formatting Script
# Handles both checking and fixing code formatting

[CmdletBinding()]
param(
    [switch]$CheckOnly = $false,
    [switch]$Fix = $false,
    [string]$Verbosity = "diagnostic",
    [switch]$NoRestore = $false
)

# Default to check mode if neither CheckOnly nor Fix is specified
if (-not $CheckOnly -and -not $Fix) {
    $CheckOnly = $true
}

$action = if ($Fix) { "Fixing" } else { "Checking" }
Write-Host "$action code formatting..." -ForegroundColor Yellow

try {
    # Check if dotnet is available
    try {
        dotnet --version | Out-Null
    } catch {
        Write-Error "dotnet CLI is not installed or not in PATH"
        exit 1
    }

    # Check if dotnet-format is installed
    $formatInstalled = dotnet tool list -g | Select-String "dotnet-format"
    if (-not $formatInstalled) {
        Write-Host "Installing dotnet-format..." -ForegroundColor Yellow
        dotnet tool install -g dotnet-format
        
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Failed to install dotnet-format"
            exit 1
        }
    }

    # Set environment variable for Windows targeting
    $env:EnableWindowsTargeting = "true"

    # Restore dependencies if requested
    if (-not $NoRestore) {
        Write-Host "Restoring dependencies..." -ForegroundColor Yellow
        dotnet restore -p:EnableWindowsTargeting=true --verbosity quiet
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Failed to restore dependencies!"
            exit 1
        }
    }

    # Build format arguments
    $formatArgs = @(
        "--verbosity", $Verbosity
        "--no-restore"
    )

    if ($CheckOnly) {
        $formatArgs += "--verify-no-changes"
        Write-Host "Running format verification..." -ForegroundColor Yellow
    }
    else {
        Write-Host "Applying code formatting..." -ForegroundColor Yellow
    }

    # Run dotnet format
    dotnet format @formatArgs
    $formatExitCode = $LASTEXITCODE

    # Run code analysis if checking
    if ($CheckOnly) {
        Write-Host "Running code analysis..." -ForegroundColor Yellow
        dotnet build --no-incremental /p:EnforceCodeStyleInBuild=true -p:EnableWindowsTargeting=true --verbosity quiet
        $analysisExitCode = $LASTEXITCODE
    }
    else {
        $analysisExitCode = 0
    }

    Write-Host ""
    
    if ($CheckOnly) {
        if ($formatExitCode -eq 0 -and $analysisExitCode -eq 0) {
            Write-Host "✅ All formatting checks passed!" -ForegroundColor Green
            Write-Host "Your code is ready for commit." -ForegroundColor White
        } else {
            if ($formatExitCode -ne 0) {
                Write-Host "❌ Code formatting issues detected!" -ForegroundColor Red
                Write-Host "Run with -Fix switch to apply formatting fixes." -ForegroundColor Yellow
            }
            if ($analysisExitCode -ne 0) {
                Write-Host "❌ Code analysis issues detected!" -ForegroundColor Red
                Write-Host "Fix the code analysis issues manually." -ForegroundColor Yellow
            }
            exit 1
        }
    }
    else {
        if ($formatExitCode -eq 0) {
            Write-Host "✅ Code formatting applied successfully!" -ForegroundColor Green
            Write-Host "Review the changes and commit when ready." -ForegroundColor White
        } else {
            Write-Host "❌ Failed to apply formatting!" -ForegroundColor Red
            exit 1
        }
    }
}
catch {
    Write-Error "An error occurred during formatting: $($_.Exception.Message)"
    exit 1
}