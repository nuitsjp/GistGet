<#
.SYNOPSIS
    Run GistGet with auth login command.

.DESCRIPTION
    This script runs the GistGet application using dotnet run with the 'auth login' command.
    Useful for development and testing authentication flow.

.PARAMETER Configuration
    Build configuration (Debug or Release). Default is Debug.

.EXAMPLE
    .\Run-AuthLogin.ps1
    .\Run-AuthLogin.ps1 -Configuration Release
#>

param(
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"

# Get repository root path
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptPath
$projectPath = Join-Path $repoRoot "src\GistGet\GistGet.csproj"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Running GistGet - Auth Login" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration" -ForegroundColor Yellow
Write-Host "Project: $projectPath" -ForegroundColor Yellow
Write-Host ""

# Run the application with auth login
Write-Host "Executing: dotnet run --project `"$projectPath`" -c $Configuration -- auth login" -ForegroundColor Green
Write-Host ""

dotnet run --project "$projectPath" -c $Configuration -- auth login

$exitCode = $LASTEXITCODE

Write-Host ""
if ($exitCode -eq 0) {
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "Auth login completed successfully!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Cyan
} else {
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "Auth login failed with exit code: $exitCode" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Cyan
}

exit $exitCode
