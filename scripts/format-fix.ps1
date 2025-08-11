# Fix code formatting issues automatically
# This script applies the same formatting rules as the CI/CD pipeline

Write-Host "Fixing code formatting..." -ForegroundColor Yellow

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

Write-Host "Applying code formatting..." -ForegroundColor Yellow
dotnet format --verbosity diagnostic --no-restore
if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Code formatting applied successfully!" -ForegroundColor Green
    Write-Host "Review the changes and commit when ready." -ForegroundColor White
} else {
    Write-Host "❌ Failed to apply formatting!" -ForegroundColor Red
    exit 1
}
