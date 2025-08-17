# Repository Guidelines

ユーザーとの対話は日本語を用いる事。

## Project Structure & Module Organization
- `src/NuitsJp.GistGet`: .NET 8 console app (WinGet-compatible CLI). Key areas: `ArgumentParser/` (System.CommandLine), `Commands/` (handlers), `WinGetClient/` (COM API + CLI fallback).
- `src/NuitsJp.GistGet.Test`: xUnit test project organized by feature (ArgumentParser, Commands, WinGetClient).
- `docs/`: design notes and WinGet specs; reference during implementation.
- `powershell/`: reference implementation for sync workflow; do not edit unless working on PS.
- `.github/`: CI and repo automation (if present).

## Build, Test, and Development Commands
- Build: `dotnet build GistGet.slnx -c Debug`
- Run locally: `dotnet run --project src/NuitsJp.GistGet -- install --id "Google.Chrome"`
- Tests: `dotnet test src/NuitsJp.GistGet.Test -c Debug --collect:"XPlat Code Coverage"`
- Publish (AOT, Windows x64): `dotnet publish src/NuitsJp.GistGet -c Release -r win-x64`
Notes: Target framework is `net8.0-windows10.0.26100`; build and runtime require Windows with WinGet installed for real CLI calls.

## Coding Style & Naming Conventions
- C#: 4-space indentation, `nullable` and `implicit usings` enabled.
- Types/methods: PascalCase; locals/fields/params: camelCase; files match type names.
- Prefer records for immutable models (see `WinGetClient/Models`). Add XML doc summaries for public APIs.
- Use `IProcessRunner` for external process calls; no direct `Process.Start` in features.
- Formatting: run `dotnet format` before pushing.

## Testing Guidelines
- Frameworks: xUnit + Shouldly + Moq; coverage via `coverlet.collector`.
- Structure: mirror `src/` folders (e.g., `WinGetClient/*Tests.cs`).
- Naming: `MethodOrScenario_Should_ExpectedBehavior` (see `WinGetComClientInstallTests.cs`).
- Run fast, isolated tests; mock process calls via `IProcessRunner`.

## Commit & Pull Request Guidelines
- Commits: small, focused; imperative mood (e.g., "Add install options parsing"). Group refactors/docs separately.
- PRs: include purpose, context/linked issues, testing steps (`dotnet build`, `dotnet test`), and sample CLI output if applicable.
- Requirements: all tests pass, no formatting diffs, update docs in `docs/` when behavior or flags change.

## Security & Configuration Tips
- Never commit secrets. Gist access uses OAuth Device Flow; tokens must be stored securely (e.g., DPAPI) and excluded from VCS.
- The app prefers COM API but falls back to WinGet CLI; ensure `winget` is available in PATH for local runs.
