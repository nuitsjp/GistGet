---
description: Reference for adding logging to GistGetService.cs
---

# GistGetService Logging Reference

This document is a reference for logging implementation in `GistGetService.cs`.

## Implemented Methods

### IConsoleService

The following methods are implemented in `src/GistGet/IConsoleService.cs`:

```csharp
/// <summary>
/// Starts a spinner progress display.
/// The spinner animates in the background and stops on Dispose.
/// </summary>
IDisposable WriteProgress(string message);

/// <summary>
/// Writes a step progress message (simple one-line output).
/// </summary>
void WriteStep(int current, int total, string message);

/// <summary>
/// Writes a success message.
/// </summary>
void WriteSuccess(string message);

/// <summary>
/// Writes an error message.
/// </summary>
void WriteError(string message);
```

### ConsoleService

The SpinnerProgress class and methods are implemented in `src/GistGet/Presentation/ConsoleService.cs`.

Spinner pattern: `["⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏"]`

---

## Usage Examples

### Spinner Progress

```csharp
using (consoleService.WriteProgress("Fetching package information from Gist..."))
{
    existingPackages = await gitHubService.GetPackagesAsync(...);
}
```

### Success Message

```csharp
consoleService.WriteSuccess($"{options.Id} has been installed and saved to Gist");
// Output: ✓ {options.Id} has been installed and saved to Gist
```

### Step Display

```csharp
consoleService.WriteStep(1, 10, "Installing package xxx...");
// Output: [1/10] Installing package xxx...
```

---

## Adding Logging to New Methods

// turbo
```powershell
dotnet build src\GistGet.slnx
```

// turbo
```powershell
dotnet test src\GistGet.slnx --no-build
```

### Patterns

1. **Gist fetch**: `using (consoleService.WriteProgress("Fetching package information from Gist..."))`
2. **Gist save**: `using (consoleService.WriteProgress("Saving package information to Gist..."))`
3. **Success**: `consoleService.WriteSuccess($"{id} has been completed")`
4. **winget passthrough**: No logging (delegate to winget)

### Notes

- For winget passthrough operations, let winget handle its own output
- Use `using` blocks for progress display to ensure cleanup on exceptions
- Maintain existing `[sync]` prefix for sync operations
