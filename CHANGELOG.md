# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.2.3] - 2026-01-01

### Fixed

- WinGetマニフェストの`InstallationMetadata`でバッチファイルを`launch`として正しく指定

## [1.2.2] - 2026-01-01

### Fixed

- WinGetポータブルパッケージでバッチファイルランチャーを使用し、DLL依存関係の問題を解決
- リンク経由での実行時にWinGet COM APIが正常に動作するように修正

## [1.2.1] - 2025-12-31

### Fixed

- WinGetマニフェストに`InstallationMetadata`セクションを追加し、インストール済みパッケージ情報が正しく取得できるように修正

## [1.2.0] - 2025-12-27

### Added

- `gist`コマンドを追加（Gistに保存されているパッケージを一覧表示）

### Fixed

- SingleFile発行時にWinGet COM依存DLLが正しく除外されない問題を修正

### Changed

- PublishArtifactsTestsの書式と初期化警告を修正
- ReSharper Global Toolsを2025.3.1にアップデート

## [1.1.0] - 2025-12-26

### Added

- `upgrade`コマンドに`--all`オプションを追加（全パッケージの一括アップグレードが可能に）

### Fixed

- 同期時の進捗表示を改善

### Changed

- `EnsureLocalPackage`の`FindById`フォールバックを削除（リファクタリング）

## [1.0.8] - 2025-12-25

### Fixed

- GitHubリリースノートの文字化け問題を修正
- 単一ファイル発行でwinget検出が失敗する問題を修正
- コンソール出力のプレフィックス表示を明確化
- コード品質（inspectcode）の問題を解消

## [1.0.7] - 2025-12-25

### Fixed

- WinGetリリース時のハッシュ不一致問題を修正（GitHub Actionsとの競合を解消）
- インストール後のPATH更新に関するトラブルシューティングをREADMEに追加

### Changed

- リリースフローをPublish-WinGet.ps1に統一（release.ymlを削除）

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

[Unreleased]: https://github.com/nuitsjp/GistGet/compare/v1.2.3...HEAD
[1.2.3]: https://github.com/nuitsjp/GistGet/compare/v1.2.2...v1.2.3
[1.2.2]: https://github.com/nuitsjp/GistGet/compare/v1.2.1...v1.2.2
[1.2.1]: https://github.com/nuitsjp/GistGet/compare/v1.2.0...v1.2.1
[1.2.0]: https://github.com/nuitsjp/GistGet/compare/v1.1.0...v1.2.0
[1.1.0]: https://github.com/nuitsjp/GistGet/compare/v1.0.8...v1.1.0
[1.0.8]: https://github.com/nuitsjp/GistGet/compare/v1.0.7...v1.0.8
[1.0.7]: https://github.com/nuitsjp/GistGet/compare/v1.0.5...v1.0.7
[1.0.5]: https://github.com/nuitsjp/GistGet/compare/v1.0.4...v1.0.5
[1.0.4]: https://github.com/nuitsjp/GistGet/releases/tag/v1.0.4
[1.0.1]: https://github.com/nuitsjp/GistGet/releases/tag/v1.0.1
[1.0.0]: https://github.com/nuitsjp/GistGet/releases/tag/v1.0.0
