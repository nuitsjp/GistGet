# GistGet

**GistGet** is a Windows CLI tool designed to synchronize Windows Package Manager (WinGet) packages with a GitHub Gist. It acts as a wrapper around WinGet, allowing users to maintain a portable list of installed software in the cloud (Gist) and easily sync their environment across machines.

## Core Mandates

**CRITICAL: You must adhere to these rules in all interactions.**

1.  **Language**:
    *   **Think in English.**
    *   **Interact with the user in Japanese.**
    *   Plans and artifacts (commit messages, PR descriptions) must be written in **Japanese**.
2.  **Test-Driven Development (TDD)**:
    *   Strictly adhere to the **t-wada style** of TDD.
    *   **RED-GREEN-REFACTOR** cycle must be followed without exception.
    *   Write a failing test first, then implement the minimal code to pass it, then refactor.
3.  **Conventions**:
    *   Adhere to existing coding styles and naming conventions found in the project.
    *   Use **C# 12** features.
    *   **Dependency Injection**: Structure logic through constructors and DI. Avoid static singletons.
4.  **No Reversions**: Do not revert changes unless explicitly asked.

## Project Overview

*   **Active Implementation**: `src/GistGet` (C# / .NET 8.0 Console Application)
*   **Legacy/Alternative**: `powershell/` (PowerShell Module) and `src_old/` (Deprecated C# version)
*   **Key Libraries**: `Spectre.Console` (UI), `System.CommandLine`, `Octokit` (GitHub), `YamlDotNet`.

## Architecture

The C# project (`src/GistGet`) follows a clean architecture:

*   **`Program.cs`**: Entry point. Sets up DI and `CommandBuilder`.
*   **`GistGet/Presentation/`**: CLI layer.
    *   `CommandBuilder.cs`: Defines CLI commands (`auth`, `sync`, `install`, etc.).
    *   `ConsoleService.cs`: Wrapper for `Spectre.Console`.
*   **`GistGet/Infrastructure/`**: External services.
    *   `GitHubService.cs`: Device Flow auth and Gist CRUD.
    *   `WinGetService.cs`: WinGet COM API wrapper.
    *   `WinGetPassthroughRunner.cs`: Executes `winget.exe` for non-intercepted commands.
*   **`GistGet/Application/`** (or Core):
    *   `PackageService.cs`: **Core Business Logic** (Sync, Install, etc.).
    *   `GistGetService.cs`: Orchestrates Gist operations.
*   **`src/GistGet.Tests/`**: xUnit test suite.

## Building and Running

### Prerequisites
*   .NET 8.0 SDK
*   Windows 10/11 (for WinGet)

### Commands

*   **Build**:
    ```powershell
    dotnet build
    ```
*   **Run CLI**:
    ```powershell
    dotnet run --project src/GistGet/GistGet.csproj -- <command> [options]
    # Example:
    dotnet run --project src/GistGet/GistGet.csproj -- auth login
    dotnet run --project src/GistGet/GistGet.csproj -- install --id Microsoft.VisualStudioCode
    ```
*   **Test**:
    ```powershell
    dotnet test
    # Or use the helper script (builds + tests + coverage):
    .\scripts\Run-Tests.ps1
    ```

## Feature Specification & Business Logic

Refer to `docs/SPEC.ja.md` for the authoritative specification.

*   **Single Source of Truth**: The Gist's `packages.yaml` is the master record.
*   **Sync Logic**:
    *   `sync` compares Gist vs. Local.
    *   Installs missing packages.
    *   Uninstalls packages marked `uninstall: true` in Gist.
    *   Updates `pin` settings (Blocking vs Pinning).
*   **Pinning**:
    *   Crucial logic around `winget pin`.
    *   `packages.yaml` stores `pin` version and `pinType` (`pinning`, `blocking`, `gating`).
*   **YAML Schema**:
    ```yaml
    <PackageId>:
      pin: <version>
      pinType: <type>
      uninstall: <bool>
      # ... other winget options (scope, silent, etc.)
    ```

## Directory Structure

*   `src/`: Active C# source code.
*   `powershell/`: PowerShell module implementation.
*   `src_old/`: Previous C# implementation (reference only).
*   `docs/`: Documentation and Specifications (e.g., `SPEC.ja.md`, `DESIGN.ja.md`).
*   `scripts/`: Helper scripts for testing and auth.
*   `.github/`: CI/CD workflows and instructions.

## Testing Guidelines

*   **Framework**: xUnit + Moq + Shouldly.
*   **Structure**: Tests should mirror the source structure.
*   **Coverage**: Aim for high coverage. Use `Run-Tests.ps1` to generate reports.
*   See `.github/instructions/cs.test.instructions.md` for detailed patterns.
