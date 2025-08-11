# Check code formatting without making changes
# This script runs the same checks as the CI/CD pipeline

Write-Host "Checking code formatting..." -ForegroundColor Yellow

# Check if dotnet is available
try {
    dotnet --version | Out-Null
} catch {
    Write-Host "❌ dotnet CLI is not installed or not in PATH" -ForegroundColor Red
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

Write-Host "Restoring dependencies..." -ForegroundColor Yellow
dotnet restore -p:EnableWindowsTargeting=true --verbosity quiet
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to restore dependencies!" -ForegroundColor Red
    exit 1
}

Write-Host "Running format verification..." -ForegroundColor Yellow
dotnet format --verify-no-changes --verbosity diagnostic --no-restore
$formatExitCode = $LASTEXITCODE

Write-Host "Running code analysis..." -ForegroundColor Yellow
dotnet build --no-incremental /p:EnforceCodeStyleInBuild=true -p:EnableWindowsTargeting=true --verbosity quiet
$analysisExitCode = $LASTEXITCODE

Write-Host ""
if ($formatExitCode -eq 0 -and $analysisExitCode -eq 0) {
    Write-Host "✅ All checks passed!" -ForegroundColor Green
    Write-Host "Your code is ready for commit." -ForegroundColor White
} else {
    if ($formatExitCode -ne 0) {
        Write-Host "❌ Code formatting issues detected!" -ForegroundColor Red
        Write-Host "Run '.\scripts\format-fix.ps1' to fix formatting issues." -ForegroundColor Yellow
    }
    if ($analysisExitCode -ne 0) {
        Write-Host "❌ Code analysis issues detected!" -ForegroundColor Red
        Write-Host "Fix the code analysis issues manually." -ForegroundColor Yellow
    }
    exit 1
}
