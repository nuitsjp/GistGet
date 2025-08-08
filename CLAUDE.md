# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

GistGet is a PowerShell module for managing WinGet installation lists on GitHub Gist. It synchronizes package definitions between local environments and cloud-stored YAML files, enabling consistent development environment setup across machines.

The repository is organized with PowerShell code in the `powershell/` directory as preparation for future .NET 8 development (see README.md for .NET architecture plans).

## Core Architecture

### Module Structure
- **Module Entry Point**: `powershell/src/GistGet.psm1` - Main module file that loads classes and functions
- **Classes**: `powershell/src/Classes.ps1` - Contains `GistFile` and `GistGetPackage` classes with YAML serialization
- **Public Functions**: `powershell/src/Public/` - User-facing cmdlets (Set-GitHubToken, Sync-GistGetPackage, Install-GistGetPackage, etc.)
- **Private Functions**: `powershell/src/Private/` - Internal helper functions for Gist operations and environment management
- **Module Manifest**: `powershell/src/GistGet.psd1` - Defines module metadata, exports, and dependencies

### Key Dependencies
- `powershell-yaml` (0.4.12) - YAML parsing and generation
- `PowerShellForGitHub` (0.17.0) - GitHub Gist API interactions  
- `Microsoft.WinGet.Client` (1.10.340) - WinGet package management

### Data Model
The `GistGetPackage` class represents WinGet packages with properties like Id, Custom parameters, Architecture, Scope, etc. It provides YAML serialization methods (`ToYaml`, `ParseYaml`) for cloud synchronization.

## Development Commands

### Testing
```powershell
# Run all tests (from project root)
.\powershell\test\Invoke-Test.ps1

# Run specific test (from project root)
Invoke-Pester -Path "powershell/test/Public/Install-GistGetPackage.Tests.ps1"

# Run all tests using Pester directly
$testPath = Join-Path (Get-Location) 'powershell\test'
Invoke-Pester -Path $testPath
```

### Building
```powershell
# Build module (runs tests, copies source to build/Output, updates version)
.\powershell\build\build.ps1
```

### Publishing
```powershell
# Publish to PowerShell Gallery (requires GIST_GET_API_KEY environment variable)
.\powershell\build\publish.ps1

# Test publish (dry run)
.\powershell\build\publish.ps1 -WhatIf
```

## Code Patterns

### Function Organization
- All public functions are in `powershell/src/Public/` and exported in the module manifest
- Private helpers in `powershell/src/Private/` handle Gist operations, environment variables, and confirmation dialogs
- Functions follow PowerShell naming conventions: Verb-Noun pattern

### Error Handling
- Module sets `$ErrorActionPreference = 'Stop'` for fail-fast behavior
- Functions use proper PowerShell error handling with `-ErrorAction` parameters

### Environment Variables
Global constants define environment variable names:
- `$global:EnvironmentVariableNameGistId = 'GIST_GET_GIST_ID'`
- `$global:EnvironmentVariableNameGistFileName = 'GIST_GET_GIST_FILE_NAME'`

### YAML Processing
Package definitions are stored as YAML in Gist files. The `GistGetPackage` class handles:
- Converting PowerShell objects to/from YAML
- Maintaining sorted order by package Id
- Handling null/empty properties correctly

## Testing Approach

Tests use Pester framework with:
- Mock objects for external dependencies (WinGet, GitHub API)
- Test asset YAML files in `powershell/test/*/assets/` directories
- Separate test files for each public function
- Module import with `-Force` flag in test runner

## Future Development Notes

The repository is structured to support future .NET 8 development alongside the PowerShell module. The README.md contains detailed architectural plans for a .NET 8 implementation using WinGet COM API with OAuth Device Flow authentication for GitHub Gist synchronization.