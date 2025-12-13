# Repository Guidelines

## Primary Directive

- Think in English, interact with the user in Japanese.
- Plans and artifacts must be written in Japanese.
- Can execute GitHub CLI/Azure CLI. Will execute and verify them personally
  whenever possible.
- When modifying the implementation, strictly adhere to the t-wada style of
  Test-Driven Development (TDD). RED-GREEN-REFACTOR cycle must be followed
  without exception.

## Project Structure & Module Organization
- `src/GistGet`: CLI entry point (`Program.cs`) plus layered folders: `Application/Services` for auth, Gist sync, and package orchestration; `Infrastructure/{OS,Security,WinGet}` for process execution, credential storage, and WinGet access; `Presentation/CliCommandBuilder` for System.CommandLine setup; `Models` and `Utils` for shared types (e.g., `GistGetPackage`, `YamlHelper`).
- `src/GistGet.Tests`: xUnit test suites organized by feature (`Presentation/`, `Services/`, `Utils/`); coverage output lands in `TestResults/`.
- `scripts`: helper PowerShell tools such as `Run-Tests.ps1` (build + test + coverage) and `Run-AuthLogin.ps1` for GitHub device-flow login.
- `docs`: design/spec references (`docs/SPEC.ja.md`, `DESIGN.ja.md`, `YAML_SPEC.ja.md`) to keep behavior aligned with the contract.

## Build, Test, and Development Commands
- Restore/build: `dotnet build src/GistGet.slnx -c Debug` (or `Release`) to compile all projects.
- Run CLI: `dotnet run --project src/GistGet/GistGet.csproj -- <command>` (e.g., `-- auth login`, `sync`, `install <id>`).
- Tests + coverage: `dotnet test src/GistGet.Tests/GistGet.Tests.csproj -c Debug --collect:"XPlat Code Coverage" --results-directory TestResults`.
- Convenience: `.\scripts\Run-Tests.ps1 [-Configuration Release] [-CollectCoverage $false]` handles build, tests, and coverage in one call.

## Coding Style & Naming Conventions
- C# 12 on `net10.0-windows10.0.26100.0`; 4-space indentation, UTF-8 with CRLF line endings, `nullable` enabled. Prefer `var` for locals, `readonly` fields, and `async` suffixes on asynchronous methods.
- Naming: PascalCase for public types/methods; camelCase parameters; `_camelCase` private readonly fields. Keep service/infra abstractions in corresponding `I*` interfaces.
- Structure logic through constructors and dependency injection (no static singletons); favor small, composable methods and guard clauses for argument validation.

## Testing Guidelines
- Frameworks: xUnit + Moq + Shouldly. Use `[Fact]` for single cases and `[Theory]` with inline data for variants.
- Coverage: keep collecting via `--collect:"XPlat Code Coverage"`; review `TestResults/coverage.cobertura.xml` before merging. Add unit tests for new branches and edge cases (GitHub auth failures, WinGet errors, YAML parsing).
- Detailed coding guidelines: see [`.github/instructions/cs.test.instructions.md`](.github/instructions/cs.test.instructions.md) for file/class structure, naming conventions, AAA pattern, and sample code.

## Commit & Pull Request Guidelines
- Follow Conventional Commits seen in history (`feat:`, `fix:`, `chore:`, `docs:`). Write present-tense, imperative subjects and keep scopes small.
- Include: what changed, why, and how to validate (commands run, notable CLI output). Link issues when applicable.
- PRs should: summarize user-visible impact, attach screenshots or sample CLI invocations when behavior changes, and confirm tests/coverage were executed (`Run-Tests.ps1` or `dotnet test` command).

## Security & Configuration Tips
- Authentication uses GitHub device flow; never commit personal access tokens. `CredentialService` stores secrets in Windows Credential Manager—avoid alternative secret storage without discussion.
- Keep WinGet interactions deterministic: prefer explicit package IDs and versions in YAML; document any fallback logic. Update manifests and docs together when command semantics change.
