# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

GistGet is a PowerShell module for managing WinGet installation lists on GitHub Gist. It synchronizes package definitions between local environments and cloud-stored YAML files, enabling consistent development environment setup across machines.

## Core Architecture

### Module Structure
- **Module Entry Point**: `src/GistGet.psm1` - Main module file that loads classes and functions
- **Classes**: `src/Classes.ps1` - Contains `GistFile` and `GistGetPackage` classes with YAML serialization
- **Public Functions**: `src/Public/` - User-facing cmdlets (Set-GitHubToken, Sync-GistGetPackage, Install-GistGetPackage, etc.)
- **Private Functions**: `src/Private/` - Internal helper functions for Gist operations and environment management
- **Module Manifest**: `src/GistGet.psd1` - Defines module metadata, exports, and dependencies

### Key Dependencies
- `powershell-yaml` (0.4.12) - YAML parsing and generation
- `PowerShellForGitHub` (0.17.0) - GitHub Gist API interactions  
- `Microsoft.WinGet.Client` (1.10.340) - WinGet package management

### Data Model
The `GistGetPackage` class represents WinGet packages with properties like Id, Custom parameters, Architecture, Scope, etc. It provides YAML serialization methods (`ToYaml`, `ParseYaml`) for cloud synchronization.

## Development Commands

### Testing
```powershell
# Run all tests
.\test\Invoke-Test.ps1

# Run specific test (from project root)
Invoke-Pester -Path "test/Public/Install-GistGetPackage.Tests.ps1"
```

### Building
```powershell
# Build module (runs tests, copies source to build/Output, updates version)
.\build\build.ps1
```

### Publishing
```powershell
# Publish to PowerShell Gallery (requires GIST_GET_API_KEY environment variable)
.\build\publish.ps1

# Test publish (dry run)
.\build\publish.ps1 -WhatIf
```

## Code Patterns

### Function Organization
- All public functions are in `src/Public/` and exported in the module manifest
- Private helpers in `src/Private/` handle Gist operations, environment variables, and confirmation dialogs
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
- Test asset YAML files in `test/*/assets/` directories
- Separate test files for each public function
- Module import with `-Force` flag in test runner