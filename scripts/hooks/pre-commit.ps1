# Pre-commit hook to run dotnet format and code quality checks
# PowerShell version for Windows

Write-Host "Running pre-commit checks..." -ForegroundColor Yellow

# Check which files are being committed
$stagedFiles = git diff --cached --name-only
$hasSourceChanges = $false

# Check if any staged files are in src/ or tests/ directories
foreach ($file in $stagedFiles) {
    if ($file -match "^src/" -or $file -match "^tests/") {
        $hasSourceChanges = $true
        break
    }
}

# If no source or test files are being committed, skip checks
if (-not $hasSourceChanges) {
    Write-Host "No source or test files changed, skipping code quality checks." -ForegroundColor Green
    exit 0
}

Write-Host "Source or test files detected, running code quality checks..." -ForegroundColor Yellow

# Check if dotnet is available
try {
    dotnet --version | Out-Null
} catch {
    Write-Host "Error: dotnet CLI is not installed or not in PATH" -ForegroundColor Red
    exit 1
}

# Check if dotnet-format is installed
$formatInstalled = dotnet tool list -g | Select-String "dotnet-format"
if (-not $formatInstalled) {
    Write-Host "Installing dotnet-format..." -ForegroundColor Yellow
    dotnet tool install -g dotnet-format
}

# Set environment variable for Windows targeting
$env:EnableWindowsTargeting = "true"

# Run dotnet restore if needed
Write-Host "Restoring dependencies..." -ForegroundColor Yellow
dotnet restore -p:EnableWindowsTargeting=true --verbosity quiet
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to restore dependencies!" -ForegroundColor Red
    exit 1
}

# Run dotnet format check
Write-Host "Checking code formatting..." -ForegroundColor Yellow
dotnet format --verify-no-changes --verbosity quiet --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "❌ Code formatting issues detected!" -ForegroundColor Red
    Write-Host "Run 'dotnet format' to fix formatting issues, then commit again." -ForegroundColor Yellow
    exit 1
}

# Run code analysis
Write-Host "Running code analysis..." -ForegroundColor Yellow
dotnet build --no-incremental /p:EnforceCodeStyleInBuild=true -p:EnableWindowsTargeting=true --verbosity quiet
if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "❌ Code analysis issues detected!" -ForegroundColor Red
    Write-Host "Fix the code analysis issues, then commit again." -ForegroundColor Yellow
    exit 1
}

Write-Host "✅ All pre-commit checks passed!" -ForegroundColor Green
exit 0
