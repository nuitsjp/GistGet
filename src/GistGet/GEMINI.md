# GistGet

**GistGet** is a Windows CLI tool designed to synchronize Windows Package Manager (WinGet) packages with a GitHub Gist. It acts as a wrapper around WinGet, allowing users to maintain a portable list of installed software in the cloud (Gist) and easily sync their environment across machines.

## Project Overview

*   **Type:** C# / .NET 8.0 Console Application
*   **Purpose:** Sync WinGet packages with GitHub Gists.
*   **Key Dependencies:**
    *   `Spectre.Console`: Rich terminal UI.
    *   `System.CommandLine`: CLI argument parsing.
    *   `Octokit`: GitHub API client (Gist management).
    *   `Microsoft.Management.Deployment`: WinGet COM API integration.
    *   `YamlDotNet`: YAML serialization for package lists.

## Architecture

The project follows a clean architecture with Dependency Injection (DI):

*   **`Program.cs`**: Entry point. Sets up DI container and runs the `CommandBuilder`.
*   **`GistGet/Presentation/`**: Handles CLI interactions.
    *   `CommandBuilder.cs`: Defines the CLI commands and handlers.
    *   `ConsoleService.cs`: Wraps console output (Spectre.Console).
*   **`GistGet/Infrastructure/`**: Implementations of external services.
    *   `GitHubService.cs`: Handles GitHub authentication (Device Flow) and Gist CRUD operations.
    *   `WinGetService.cs`: Uses WinGet COM API to query package information.
    *   `WinGetPassthroughRunner.cs`: Executes `winget.exe` directly for passthrough commands.
*   **`GistGet/` (Core)**:
    *   `PackageService.cs`: **(WIP)** Core logic for syncing, installing, and managing packages. currently contains mostly `NotImplementedException`.
    *   `GistGetService.cs`: Orchestrator for authentication and high-level Gist operations.
    *   `GistGetPackage.cs`: Data model representing a package (mapped to YAML).

## Key Features (Planned/Implemented)

*   **Auth (`auth`)**: Login to GitHub using Device Flow (Implemented).
*   **Sync (`sync`)**: Synchronize local packages with Gist (Logic pending in `PackageService`).
*   **Management (`install`, `uninstall`, `upgrade`, `pin`)**: Wraps WinGet commands and updates the Gist automatically (Logic pending in `PackageService`).
*   **Passthrough**: Forwards standard WinGet commands (`search`, `list`, etc.) to the local `winget` executable (Implemented).

## Building and Running

**Prerequisites:**
*   .NET 8.0 SDK
*   Windows 10/11 (for WinGet support)

**Build:**
```powershell
dotnet build
```

**Run:**
```powershell
dotnet run -- [command] [options]
# Example:
dotnet run -- auth login
dotnet run -- install --id Microsoft.VisualStudioCode
```

**Publish (Single File):**
The project is configured for `win-x64` standalone execution.
```powershell
dotnet publish -c Release -r win-x64
```

## Development Status

*   **Active Development**: The core business logic in `PackageService.cs` is largely stubbed out and requires implementation to connect the `GitHubService` and `WinGetService` components.
*   **Implemented**: CLI structure, GitHub Gist serialization/deserialization, WinGet COM querying, WinGet process execution.
*   **TODO**: Implement `InstallAndSaveAsync`, `SyncAsync`, and other methods in `PackageService.cs`.

## Testing

- Detailed coding guidelines: see [`../../.github/instructions/cs.test.instructions.md`](../../.github/instructions/cs.test.instructions.md) for file/class structure, naming conventions, AAA pattern, and sample code.
