# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

GistGet is a hybrid PowerShell module and .NET 8 console application for managing WinGet installation lists on GitHub Gist. It synchronizes package definitions between local environments and cloud-stored YAML files, enabling consistent development environment setup across machines.

The repository contains:
- **PowerShell Module**: `powershell/` directory - Original PowerShell implementation
- **.NET 8 Application**: `src/` directory - Modern .NET implementation using WinGet COM API
- **Build System**: Invoke-Build based automation in `build-scripts/`

## Primary Directive

- Think in English, interact with the user in Japanese.
- When modifying the implementation, strictly adhere to the t-wada style of Test-Driven Development (TDD).
  - **t-wada TDD Concept**:
    1. 1st Issue
        1. First, write a failing test (Red).
        2. Then, write the simplest code to make it pass (Green).
        3. Finally, refactor the code (Refactor).
    2. 2nd Issue
        1. First, write a failing test (Red).
        2. Then, write the simplest code to make it pass (Green).
        3. Finally, refactor the code (Refactor).
  - Each cycle should be small and focused on a single purpose.

## Core Architecture

### .NET 8 Application Structure (Primary)
- **Entry Point**: `src/NuitsJp.GistGet/Program.cs` - Application startup and DI container setup
- **Presentation Layer**: `src/NuitsJp.GistGet/Presentation/` - CLI commands and console interactions
  - `CommandRouter.cs` - Routes commands to appropriate handlers
  - Command-specific subdirectories: `Login/`, `Sync/`, `GistConfig/`, `WinGet/`
- **Business Layer**: `src/NuitsJp.GistGet/Business/` - Core business logic
  - `GistManager.cs`, `GistSyncService.cs`, `PackageManagementService.cs`
- **Infrastructure Layer**: `src/NuitsJp.GistGet/Infrastructure/` - External service integrations
  - `GitHub/` - GitHub Gist API integration using Octokit
  - `WinGet/` - WinGet COM API and passthrough implementations
  - `Storage/` - Local configuration storage with DPAPI encryption
- **Models**: `src/NuitsJp.GistGet/Models/` - Data models and JSON contexts

### PowerShell Module Structure (Legacy)
- **Module Entry Point**: `powershell/src/GistGet.psm1`
- **Classes**: `powershell/src/Classes.ps1` - YAML serialization classes
- **Public/Private Functions**: Command implementations and helpers

### Key Dependencies (.NET 8)
- `Microsoft.WindowsPackageManager.ComInterop` (1.10.340) - WinGet COM API
- `Octokit` (14.0.0) - GitHub API client
- `YamlDotNet` (16.3.0) - YAML processing
- `Sharprompt` (3.0.0) - Interactive console prompts
- `Microsoft.Extensions.Hosting` (8.0.1) - Dependency injection and hosting
- `System.Security.Cryptography.ProtectedData` (8.0.0) - Secure credential storage

### Key Dependencies (PowerShell)
- `powershell-yaml` (0.4.12) - YAML parsing and generation
- `PowerShellForGitHub` (0.17.0) - GitHub Gist API interactions  
- `Microsoft.WinGet.Client` (1.10.340) - WinGet package management

### Data Model
The `PackageDefinition` class (.NET) and `GistGetPackage` class (PowerShell) represent WinGet packages with properties like Id, Version, Architecture, Scope, etc. Both provide YAML serialization for cloud synchronization.

## Development Commands

### .NET 8 Application (Primary Development)
```powershell
# Initial setup - install required tools
Invoke-Build Setup

# Build the .NET solution
Invoke-Build Build

# Run all tests with coverage
Invoke-Build Test

# Generate coverage report
Invoke-Build Coverage

# Run code inspection with ReSharper
Invoke-Build CodeInspection

# Full build pipeline (Setup + Build + Test + Coverage + CodeInspection)
Invoke-Build Full

# Clean build artifacts
Invoke-Build Clean

# Format checking and fixing
Invoke-Build FormatCheck    # Check code formatting
Invoke-Build FormatFix      # Apply formatting fixes

# Direct dotnet commands
dotnet build                # Build solution
dotnet test                 # Run tests
dotnet test --filter "Category=Unit"    # CI-safe unit tests only
dotnet test --filter "Category=Local"   # Local integration tests
dotnet publish -c Release -r win-x64 --self-contained  # Create single-file executable
```

### PowerShell Module (Legacy)
```powershell
# Run PowerShell module tests
.\powershell\test\Invoke-Test.ps1

# Build PowerShell module
.\powershell\build\build.ps1

# Publish to PowerShell Gallery
.\powershell\build\publish.ps1
```

## Code Patterns

### .NET 8 Application Patterns
- **Dependency Injection**: Uses Microsoft.Extensions.DependencyInjection for service registration
- **Layered Architecture**: Presentation → Business → Infrastructure separation
- **Interface-Based Design**: All services implement interfaces for testability
- **Command Pattern**: Each CLI command is a separate class inheriting from command base classes
- **Error Handling**: Uses structured exception handling with custom error types
- **Configuration**: Uses DPAPI for secure local storage of credentials and settings
- **Test Categories**: Tests are categorized as "Unit", "Local", or "Manual" for different execution contexts

### PowerShell Module Patterns
- All public functions are in `powershell/src/Public/` and exported in the module manifest
- Private helpers in `powershell/src/Private/` handle Gist operations, environment variables, and confirmation dialogs
- Functions follow PowerShell naming conventions: Verb-Noun pattern
- Module sets `$ErrorActionPreference = 'Stop'` for fail-fast behavior

### Shared Patterns
- **YAML Processing**: Both implementations store package definitions as YAML in Gist files
- **Environment Variables**: Configuration through environment variables (GIST_GET_*)
- **Authentication**: GitHub OAuth Device Flow for interactive authentication
- **Hybrid Strategy**: COM API for package management operations, passthrough for information commands

## Testing Approach

### .NET 8 Testing
- **Framework**: xUnit with Moq for mocking
- **Test Categories**: 
  - `"Unit"` - CI-safe tests with no external dependencies
  - `"Local"` - Integration tests requiring local Windows environment
  - `"Manual"` - Tests requiring manual verification
- **Structure**: Mirror source structure in `src/NuitsJp.GistGet.Tests/`
- **Mocking**: Mock services for external dependencies (WinGet COM API, GitHub API)
- **Coverage**: ReportGenerator for coverage reporting

### PowerShell Testing
- **Framework**: Pester with mock objects for external dependencies
- **Test Assets**: YAML files in `powershell/test/*/assets/` directories
- **Organization**: Separate test files for each public function
- **Module Loading**: Import with `-Force` flag in test runner

### CI/CD Strategy
- **GitHub Actions**: Runs unit tests only (`"Category=Unit"`)
- **Local Development**: Full test suite including integration tests
- **Manual Testing**: Windows Sandbox for clean environment verification

## Development Notes

This is a hybrid repository with both PowerShell and .NET 8 implementations. The .NET 8 version is the primary development focus, using WinGet COM API for enhanced functionality. The PowerShell module remains for compatibility and legacy support.