# 実装計画: sync dry-run

**ブランチ**: `0002-speckit-plan` | **日付**: 2026-05-28 | **仕様**: [spec.md](./spec.md)

**入力**: `specs/001-sync-dry-run/spec.md` の機能仕様

## 概要

`sync` コマンドに `--dry-run` フラグを追加し、同期元とローカル状態の差分を実操作なしで確認できるようにする。既存の `sync`、`--url`、`--file` の読み込み経路は維持し、dry-run 時だけインストール、アンインストール、アップグレード、ピン変更、Gist 保存を行わない分岐を追加する。表示は既存の CLI 層で扱い、サービスはプレビュー結果を `SyncResult` に集約して返す。

## 技術コンテキスト

**言語/バージョン**: C# 14 / net10.0-windows10.0.26100.0

**主要依存関係**: System.CommandLine、Spectre.Console、Windows Package Manager

**ストレージ**: GitHub Gist の `GistGet.yaml`、Windows Credential Manager（通常の Gist 読み込み時）

**テスト**: xUnit、Moq、Shouldly、Spectre.Console.Testing

**対象プラットフォーム**: Windows 10/11、Windows SDK 10.0.26100.0 以降

**プロジェクト種別**: .NET CLI

**性能目標**: dry-run は 1 回の実行で同期元パッケージとローカル状態を分類し、通常の状態取得回数を増やさない。

**制約**: 日本語成果物、t-wada 式 TDD、明示的な winget 引数、フォールバックなし、dry-run では外部状態を変更しない。

**規模/範囲**: `sync` コマンド、`IGistGetService.SyncAsync`、`GistGetService.SyncAsync`、`SyncResult`、同期表示、関連単体テストを対象にする。JSON 出力や対話確認は対象外。

## 憲章チェック

*ゲート: Phase 0 調査前に必ず通過。Phase 1 設計後にも再確認する。*

- [x] 仕様、計画、タスク、説明文は日本語で書かれている。
- [x] 新しい振る舞いに対して、先に失敗するテストを書く計画になっている。
- [x] RED、GREEN、REFACTOR の順序がタスクに反映されている。
- [x] 実操作とプレビューが必要な副作用を識別し、分離方法を記載している。
- [x] 既存の DI、命名、ファイル構成、リソース管理に従う方針になっている。
- [x] 明示要求のない後方互換性、フォールバック、過度な抽象化を追加していない。

## プロジェクト構成

### ドキュメント（この機能）

```text
specs/001-sync-dry-run/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── sync-dry-run.md
└── tasks.md
```

### ソースコード（リポジトリルート）

```text
src/NuitsJp.GistGet/
├── GistGetService.cs
├── IGistGetService.cs
├── SyncResult.cs
├── Resources/
│   ├── Messages.resx
│   ├── Messages.ja.resx
│   └── Messages.Designer.cs
└── Presentation/
    └── CommandBuilder.cs

src/NuitsJp.GistGet.Test/
├── GistGetServiceTest.cs
└── Presentation/
    └── CommandBuilderTests.cs
```

**構成判断**: 既存の `sync` 実装が `GistGetService` と `CommandBuilder` に集約されているため、新しいサービスや静的ヘルパーは追加しない。差分分類は既存の同期処理から副作用のない判定として切り出し、結果保持は `SyncResult` を最小拡張する。

## Phase 0: 調査結果

[research.md](./research.md) に記録済み。未解決の明確化項目はない。

## Phase 1: 設計結果

- データモデル: [data-model.md](./data-model.md)
- CLI 契約: [contracts/sync-dry-run.md](./contracts/sync-dry-run.md)
- クイックスタート: [quickstart.md](./quickstart.md)
- Agent context: `AGENTS.md` の Spec Kit 参照をこの計画へ更新済み

## Phase 1 後の憲章再チェック

- [x] 設計成果物は日本語で書かれている。
- [x] TDD は `CommandBuilderTests` と `GistGetServiceTest` の失敗テストから開始する方針で明記されている。
- [x] dry-run の副作用禁止対象は契約とデータモデルで明示されている。
- [x] 既存の DI 境界を維持し、新しい抽象化を追加しない。
- [x] フォールバックや追加出力形式は対象外としている。

## 複雑性追跡

憲章違反はない。
