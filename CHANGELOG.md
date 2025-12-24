# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.5] - 2025-12-24

### Fixed

- `install`後にGistへ保存するバージョンを実際のインストール済みバージョンに修正
- `sync`結果の出力でマークアップをエスケープし、記号を含むIDやエラーでも表示が崩れないよう修正

## [1.0.4] - 2025-12-21

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

[Unreleased]: https://github.com/nuitsjp/GistGet/compare/v1.0.5...HEAD
[1.0.5]: https://github.com/nuitsjp/GistGet/compare/v1.0.4...v1.0.5
[1.0.4]: https://github.com/nuitsjp/GistGet/releases/tag/v1.0.4
[1.0.1]: https://github.com/nuitsjp/GistGet/releases/tag/v1.0.1
[1.0.0]: https://github.com/nuitsjp/GistGet/releases/tag/v1.0.0
