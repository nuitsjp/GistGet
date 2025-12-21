# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.1] - 2025-12-21

### Fixed

- Release workflow: Added artifact download step in winget-publish job
- Release workflow: Fixed duplicate workflow triggers on tag push

## [1.0.0] - 2025-12-20

### Added

- Initial release
- GitHub Device Flow authentication (`auth login`, `auth logout`, `auth status`)
- Package synchronization via GitHub Gist (`sync`)
- Package management commands (`install`, `uninstall`, `upgrade`)
- Version pinning support (`pin add`, `pin remove`)
- Package export/import functionality (`export`, `import`)
- Passthrough support for winget commands (`list`, `search`, `show`, `source`, `settings`, etc.)
- YAML-based configuration management (`GistGet.yaml`)
- Secure credential storage using Windows Credential Manager

[Unreleased]: https://github.com/nuitsjp/GistGet/compare/v1.0.1...HEAD
[1.0.1]: https://github.com/nuitsjp/GistGet/releases/tag/v1.0.1
[1.0.0]: https://github.com/nuitsjp/GistGet/releases/tag/v1.0.0
