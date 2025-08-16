# Build Solution Script

[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$Verbosity = "minimal"
)

Write-Host "Building solution..." -ForegroundColor Green

try {
    # Build the solution
    dotnet build --configuration $Configuration --verbosity $Verbosity
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Build completed successfully." -ForegroundColor Green
    }
    else {
        Write-Error "Build failed with exit code: $LASTEXITCODE"
        exit $LASTEXITCODE
    }
}
catch {
    Write-Error "An error occurred during build: $($_.Exception.Message)"
    exit 1
}