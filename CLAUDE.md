# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Language Policy

- **Think in English** but **interact with users in Japanese**
- Plans, commit messages, and PR descriptions must be written in **Japanese**

## Build & Development Commands

```powershell
# Build
dotnet build src/GistGet.slnx -c Debug

# Run CLI
dotnet run --project src/NuitsJp.GistGet/NuitsJp.GistGet.csproj -- <command>
# Examples: -- auth login, -- sync, -- install <id>

# Run all tests with coverage
dotnet test src/NuitsJp.GistGet.Test/NuitsJp.GistGet.Test.csproj -c Debug --collect:"XPlat Code Coverage" --results-directory TestResults

# Run a specific test class
dotnet test src/NuitsJp.GistGet.Test/NuitsJp.GistGet.Test.csproj --filter "FullyQualifiedName~ClassName"

# Run a specific test method
dotnet test src/NuitsJp.GistGet.Test/NuitsJp.GistGet.Test.csproj --filter "FullyQualifiedName~ClassName.MethodName"

# Full code quality pipeline (FormatCheck -> Build -> Tests -> ReSharper)
.\scripts\Run-CodeQuality.ps1

# Run specific steps only
.\scripts\Run-CodeQuality.ps1 -Build           # Build only
.\scripts\Run-CodeQuality.ps1 -Build -Tests    # Build and tests only
.\scripts\Run-CodeQuality.ps1 -Tests           # Tests only

# GitHub authentication (required before integration tests)
.\scripts\Run-AuthLogin.ps1
```

## Architecture

GistGet is a CLI tool that syncs winget packages across devices via GitHub Gist.

### Project Structure

```
src/
├── GistGet/                       # Launcher executable (thin wrapper)
│   └── Program.cs                 # Entry point that calls NuitsJp.GistGet
├── NuitsJp.GistGet/               # Main library
│   ├── Program.cs                 # DI bootstrap and CLI entry point
│   ├── GistGetService.cs          # Main orchestration (init, sync, install, etc.)
│   ├── GistGetPackage.cs          # Package model
│   ├── GistGetPackageSerializer.cs # YAML serialization
│   ├── Presentation/
│   │   ├── CommandBuilder.cs      # All CLI commands definition
│   │   └── ConsoleService.cs      # Console output handling
│   ├── Infrastructure/
│   │   ├── WinGet/                # WinGet COM interop helpers
│   │   ├── CredentialService.cs   # Windows Credential Manager
│   │   ├── GitHubService.cs       # Gist read/write operations
│   │   └── WinGetService.cs       # Package search, install, upgrade, uninstall
│   └── *Options.cs                # InstallOptions, UpgradeOptions, UninstallOptions
└── NuitsJp.GistGet.Test/          # Test project
```

### Key Dependencies

- **Microsoft.WindowsPackageManager.ComInterop**: WinGet COM API
- **Octokit**: GitHub API (Gist operations)
- **System.CommandLine**: CLI argument parsing
- **Spectre.Console**: Rich console output
- **Sharprompt**: Interactive prompts for `init` command
- **YamlDotNet**: YAML serialization for GistGet.yaml
- **UnitGenerator**: Value object generation (PackageId, Version)

### Core Workflows

1. **auth login**: GitHub device flow -> stores token in Windows Credential Manager
2. **init**: Lists local winget packages -> user selects -> creates/updates GistGet.yaml in Gist
3. **sync**: Fetches GistGet.yaml -> compares with local -> installs missing, uninstalls marked packages
4. **install/upgrade/uninstall**: Standard winget operations + updates Gist

## Coding Standards

- **Framework**: .NET 10.0, C# 14, Windows 10.0.26100.0
- **DI**: All services registered in Program.cs, constructor injection
- **Async**: `*Async` suffix for async methods
- **Testing**: xUnit + Moq + Shouldly, strict AAA pattern with comment separators
- **TDD**: Follow t-wada style RED-GREEN-REFACTOR cycle

### Test File Structure

```csharp
public class WinGetServiceTests
{
    protected readonly WinGetService WinGetService = new();

    public class FindById : WinGetServiceTests
    {
        [Fact]
        public void ExistingPackage_ReturnsPackage()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var packageId = new PackageId("jqlang.jq");

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = WinGetService.FindById(packageId);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            result.ShouldNotBeNull();
        }
    }
}
```

## Coverage Requirements

- **Line coverage**: 98% minimum
- **Branch coverage**: 85% minimum
- **Per-file threshold**: 89% minimum
