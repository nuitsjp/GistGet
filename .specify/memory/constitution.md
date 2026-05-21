<!--
Sync Impact Report
Version change: 0.0.0 (template) → 1.0.0
Modified principles:
- テンプレート原則 1 → I. 日本語での一貫した成果物
- テンプレート原則 2 → II. Windows Package Manager 向け .NET CLI
- テンプレート原則 3 → III. t-wada 式 TDD（非交渉）
- テンプレート原則 4 → IV. 破壊的操作の明示分離
- テンプレート原則 5 → V. 単純さと既存構造の尊重
Added sections:
- 技術標準
- 開発ワークフロー
Removed sections:
- なし
Templates requiring updates:
- ✅ .specify/templates/plan-template.md
- ✅ .specify/templates/spec-template.md
- ✅ .specify/templates/tasks-template.md
- ✅ .specify/templates/checklist-template.md
- ⚠ .specify/templates/commands/*.md（ディレクトリなし）
Follow-up TODOs:
- なし
-->
# GistGet 憲章

## Core Principles

### I. 日本語での一貫した成果物
すべての対話、仕様、計画、タスク、チェックリスト、コミットメッセージ、PR説明は
日本語で記述しなければならない。ユーザーに見える文言、開発ドキュメント、Spec Kit
成果物も日本語を標準とする。これにより、意思決定、レビュー、履歴の文脈を同じ言語で
追跡できる状態を保つ。

### II. Windows Package Manager 向け .NET CLI
GistGet は Windows Package Manager 向けの .NET CLI として実装しなければならない。
対象は `net10.0-windows10.0.26100.0`、言語は C# 14、CLI 構築は System.CommandLine、
表示は Spectre.Console を使用する。既存の `src/GistGet` と `src/GistGet.Tests` の
構成、DI、命名、リソース管理に従い、Windows と winget の前提を曖昧にしない。

### III. t-wada 式 TDD（非交渉）
新しい振る舞いは、必ず失敗するテストを先に追加してから最小実装を行い、その後に
リファクタリングする。RED-GREEN-REFACTOR の順序を破ってはならない。テストには
xUnit、Moq、Shouldly を使い、ユーザーストーリーや受け入れ条件ごとに独立して検証
できる単位を作る。テストを追加できない変更は、その理由を計画とPR説明に明記しなければ
ならない。

### IV. 破壊的操作の明示分離
インストール、アンインストール、同期、pin 変更、Gist 更新など副作用を持つ機能では、
実操作とプレビューを明確に分離しなければならない。プレビューは外部状態を変更しては
ならず、テストで副作用が発生しないことを検証する。実操作は対象パッケージ、Gist、
winget 引数を明示し、暗黙のフォールバックや互換処理を追加しない。

### V. 単純さと既存構造の尊重
明示要求がない限り、後方互換性、フォールバック、過度な抽象化を追加してはならない。
KISS と YAGNI を優先し、既存の DI、命名規則、ファイル配置、サービス境界に沿って
最小の変更で実装する。新しい抽象化は、重複や複雑さを明確に減らす場合に限って導入する。

## 技術標準

- 言語とランタイムは C# 14 と `net10.0-windows10.0.26100.0` を使用する。
- CLI は System.CommandLine、コンソール表示は Spectre.Console を使用する。
- テストは xUnit、Moq、Shouldly を使用する。
- ロジックはコンストラクター注入とインターフェイスで構成し、静的シングルトンを避ける。
- パッケージ ID、バージョン、winget 引数、Gist YAML は明示的かつ決定的に扱う。
- 秘密情報やトークンをリポジトリへ保存してはならない。認証情報は既存の資格情報管理に従う。

## 開発ワークフロー

すべての機能は、仕様でユーザーストーリーと受け入れ条件を定義してから計画する。計画では
憲章チェックを通過し、技術選定、テスト方針、副作用の有無、プレビュー要件を明記する。
タスクは RED、GREEN、REFACTOR の順に分割し、各ストーリーが独立して検証できる形にする。

実装では、失敗するテストを確認してから最小実装を行う。リファクタリングはテストが通った後に
行い、既存の構造を壊さない。レビューでは、憲章違反、未検証の副作用、不要なフォールバック、
過度な抽象化がないことを確認する。

## Governance

この憲章は GistGet の開発規範として、他の慣習やテンプレートより優先される。変更する場合は、
変更理由、影響を受ける原則、更新対象テンプレート、移行が必要な成果物を明記しなければ
ならない。

バージョンはセマンティックバージョニングに従う。原則の削除や意味の再定義は MAJOR、
原則または必須セクションの追加や実質的な拡張は MINOR、文言整理や明確化は PATCH とする。
初回採択版は 1.0.0 とする。

すべての仕様、計画、タスク、PR は憲章チェックを通過しなければならない。違反を許容する場合は、
計画の複雑性追跡に理由と却下した単純な代替案を記録する。憲章更新時は、関連テンプレートと
ランタイムガイドの整合性を同時に確認する。

**Version**: 1.0.0 | **Ratified**: 2026-05-21 | **Last Amended**: 2026-05-21
