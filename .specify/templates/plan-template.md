# 実装計画: [FEATURE]

**ブランチ**: `[###-feature-name]` | **日付**: [DATE] | **仕様**: [link]

**入力**: `/specs/[###-feature-name]/spec.md` の機能仕様

**注記**: このテンプレートは `/speckit-plan` コマンドで埋める。

## 概要

[機能仕様から、主要要件と技術アプローチを要約する]

## 技術コンテキスト

**言語/バージョン**: C# 14 / net10.0-windows10.0.26100.0

**主要依存関係**: System.CommandLine、Spectre.Console、Windows Package Manager

**ストレージ**: GitHub Gist の `GistGet.yaml`、Windows Credential Manager（該当時）

**テスト**: xUnit、Moq、Shouldly

**対象プラットフォーム**: Windows 10/11、Windows SDK 10.0.26100.0 以降

**プロジェクト種別**: .NET CLI

**性能目標**: [機能固有の測定可能な目標、または該当なし]

**制約**: 日本語成果物、t-wada 式 TDD、明示的な winget 引数、フォールバックなし

**規模/範囲**: [対象コマンド、対象サービス、対象ユーザーストーリーを記載]

## 憲章チェック

*ゲート: Phase 0 調査前に必ず通過。Phase 1 設計後にも再確認する。*

- [ ] 仕様、計画、タスク、説明文は日本語で書かれている。
- [ ] 新しい振る舞いに対して、先に失敗するテストを書く計画になっている。
- [ ] RED、GREEN、REFACTOR の順序がタスクに反映されている。
- [ ] 実操作とプレビューが必要な副作用を識別し、分離方法を記載している。
- [ ] 既存の DI、命名、ファイル構成、リソース管理に従う方針になっている。
- [ ] 明示要求のない後方互換性、フォールバック、過度な抽象化を追加していない。

## プロジェクト構成

### ドキュメント（この機能）

```text
specs/[###-feature]/
├── plan.md              # このファイル（/speckit-plan の出力）
├── research.md          # Phase 0 の出力
├── data-model.md        # Phase 1 の出力
├── quickstart.md        # Phase 1 の出力
├── contracts/           # Phase 1 の出力
└── tasks.md             # Phase 2 の出力（/speckit-tasks で作成）
```

### ソースコード（リポジトリルート）

```text
src/GistGet/
├── Application/Services/
├── Infrastructure/
├── Models/
├── Presentation/
└── Utils/

src/GistGet.Tests/
├── Presentation/
├── Services/
└── Utils/
```

**構成判断**: [この機能で追加または変更する実パスを記載する]

## 複雑性追跡

> **憲章チェック違反を正当化する必要がある場合のみ記入する**

| 違反 | 必要な理由 | 却下した単純な代替案と理由 |
|------|------------|----------------------------|
| [例: 新しい抽象化] | [現在必要な理由] | [既存サービスでは不十分な理由] |
