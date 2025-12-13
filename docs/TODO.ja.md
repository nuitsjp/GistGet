# TODO（仕様準拠・整合性）

このファイルは、`docs/SPEC.ja.md` と現状実装の差分（仕様不一致/実装漏れ/設計ガイドライン逸脱）を列挙するバックログです。

**最終更新**: 2025年12月13日（方針整理・優先順位再編）

実施したTODOは☑に変更してください。

---

## 設計方針

### GistGet の CLI オプション設計原則

GistGet は Gist 上の YAML でパッケージを **ID で一意に管理** するため、以下のオプションは **サポート対象外** とする：

| 除外オプション | 理由 |
|---------------|------|
| `-q,--query` | 曖昧検索は YAML 定義と矛盾 |
| `--name`, `--moniker`, `--tag`, `--cmd` | 同上 |
| `-m,--manifest` | ローカルマニフェストは Gist 管理対象外 |
| `-e,--exact` | ID 指定時は常に完全一致 |
| `-r,--rename` | ポータブル専用、Gist 管理対象外 |
| `--authentication-*` | winget 認証は GistGet 管理対象外 |
| `--product-code` | ID ベース管理と矛盾 |

これらが必要な場合は `winget` を直接実行するか、パススルーコマンドを使用。

---

## 🔴 Phase 1: 重大なバグ修正（データ損失防止）
### 1-1. YAML シリアライズ時のフィールド脱落
### 1-2. install の custom オプションが誤っている
### 1-3. SerializePackages の全属性テスト
- [x] `acceptPackageAgreements` / `acceptSourceAgreements` が保存されない（パッケージ生成時に反映漏れ）
  - 関連ファイル: `src/GistGet/GistGet/GistGetService.cs`
---

## 🟠 Phase 2: CLI オプション整備

### 2-1. install コマンドの CLI オプション追加

`InstallOptions` にプロパティはあるが、CLI で受け付けていないオプション：

| オプション | 追加要否 | 備考 |
|------------|:--------:|------|
| `--accept-package-agreements` | ✅ | Gist 保存対象 |
| `--accept-source-agreements` | ✅ | Gist 保存対象 |
| `--locale` | ✅ | Gist 保存対象 |
| `--ignore-security-hash` | ✅ | Gist 保存対象 (`AllowHashMismatch`) |

- [x] 上記4オプションを `CommandBuilder.BuildInstallCommand()` に追加
- 関連ファイル: `src/GistGet/GistGet/Presentation/CommandBuilder.cs`

### 2-2. install コマンドの追加オプション検討

以下は `InstallOptions` / `GistGetPackage` への追加も必要：

| オプション | 説明 | 対応 |
|------------|------|:----:|
| `-s,--source` | ソース指定 | ⚠️ 要検討（複数ソース運用時） |
| `--allow-reboot` | 再起動許可 | ⚠️ 要検討 |
| `--no-upgrade` | 既存時アップグレードスキップ | ⚠️ 要検討 |
| `--uninstall-previous` | 旧バージョン削除 | ⚠️ 要検討 |
| `--dependency-source` | 依存関係ソース | 低優先 |
| `--ignore-local-archive-malware-scan` | マルウェアスキャン無視 | 低優先 |

### 2-3. uninstall コマンドのオプション追加

現在 `--id` のみ。以下を追加検討：

| オプション | 説明 | 対応 |
|------------|------|:----:|
| `--scope` | スコープ（user/machine） | ✅ 追加 |
| `-i,--interactive` | 対話モード | ✅ 追加 |
| `-h,--silent` | サイレントモード | ✅ 追加 |
| `--force` | 強制実行 | ✅ 追加 |
| `--purge` | 完全削除（ポータブル） | ⚠️ 要検討 |
| `--preserve` | ファイル保持（ポータブル） | ⚠️ 要検討 |
| `-o,--log` | ログファイルパス | ⚠️ 要検討 |
| `--accept-source-agreements` | ソース契約同意 | ⚠️ 要検討 |

- [x] `UninstallOptions` record を作成
- [x] `CommandBuilder.BuildUninstallCommand()` を拡張
- [x] `GistGetService.UninstallAndSaveAsync()` を拡張
- 関連ファイル: `src/GistGet/GistGet/Presentation/CommandBuilder.cs`

### 2-4. upgrade コマンドのオプション追加

現在 `--id`, `--version` のみ。以下を追加：

| オプション | 説明 | Gist保存 | 対応 |
|------------|------|:--------:|:----:|
| `-i,--interactive` | 対話モード | ❌ | ✅ 追加 |
| `-h,--silent` | サイレントモード | ❌ | ✅ 追加 |
| `-o,--log` | ログファイルパス | ❌ | ✅ 追加 |
| `--custom` | カスタム引数 | ✅ | ✅ 追加 |
| `--override` | 引数上書き | ✅ | ✅ 追加 |
| `-l,--location` | インストール先 | ✅ | ✅ 追加 |
| `--scope` | スコープ | ✅ | ✅ 追加 |
| `-a,--architecture` | アーキテクチャ | ✅ | ✅ 追加 |
| `--installer-type` | インストーラタイプ | ✅ | ✅ 追加 |
| `--locale` | ロケール | ✅ | ✅ 追加 |
| `--ignore-security-hash` | ハッシュ不一致無視 | ✅ | ✅ 追加 |
| `--skip-dependencies` | 依存関係スキップ | ✅ | ✅ 追加 |
| `--accept-package-agreements` | パッケージ契約同意 | ✅ | ✅ 追加 |
| `--accept-source-agreements` | ソース契約同意 | ✅ | ✅ 追加 |
| `--force` | 強制実行 | ✅ | ✅ 追加 |
| `--allow-reboot` | 再起動許可 | ⚠️ | 要検討 |
| `--uninstall-previous` | 旧バージョン削除 | ⚠️ | 要検討 |
| `-u,--include-unknown` | バージョン不明も含める | ❌ | パススルー時のみ |
| `--include-pinned` | ピン済みも含める | ❌ | パススルー時のみ |

- [x] `UpgradeOptions` record を作成
- [x] `CommandBuilder.BuildUpgradeCommand()` を拡張
- [x] `GistGetService.UpgradeAndSaveAsync()` を拡張（Gist保存対象オプションの反映）
- 関連ファイル: `src/GistGet/GistGet/Presentation/CommandBuilder.cs`

### 2-5. pin add コマンドのオプション追加

| オプション | 説明 | 対応 |
|------------|------|:----:|
| `--blocking` | ブロッキング pin | ✅ 要実装 |
| `--gating` | ゲーティング pin | ✅ 要実装（`--version` と組合せ） |
| `--installed` | インストール済みバージョンに固定 | ⚠️ 要検討 |
| `--force` | 強制上書き | ✅ 追加（内部では使用済み） |

- [x] `CommandBuilder.BuildPinCommand()` の `add` サブコマンドを拡張
- [x] `pin add --force` が force 引数に関係なく常に付与され、CLI オプションが無効化されている
  - 関連ファイル: `src/GistGet/GistGet/GistGetService.cs`
- 関連ファイル: `src/GistGet/GistGet/Presentation/CommandBuilder.cs`

---

## 🟡 Phase 3: 未実装コマンド

### 3-1. export コマンド実装

- [x] `IGistGetService.ExportAsync()` メソッド追加
- [x] `GistGetService.ExportAsync()` 実装
- [x] `CommandBuilder.BuildExportCommand()` にハンドラ追加
- 関連ファイル: `src/GistGet/GistGet/IGistGetService.cs`, `GistGetService.cs`, `CommandBuilder.cs`

### 3-2. import コマンド実装

- [x] `IGistGetService.ImportAsync()` メソッド追加
- [x] `GistGetService.ImportAsync()` 実装
- [x] `CommandBuilder.BuildImportCommand()` にハンドラ追加
- 関連ファイル: 同上

### 3-3. パススルーコマンド追加

| winget コマンド | 対応 |
|----------------|:----:|
| `dscv3` | ☑️ 実装済（DSC v3 リソース） |
| `mcp` | ☑️ 実装済（MCP 情報） |

- [x] 必要に応じて `BuildWingetPassthroughCommands()` に追加
- 関連ファイル: `src/GistGet/GistGet/Presentation/CommandBuilder.cs`

---

## 🟡 Phase 4: ロジック修正

### 4-1. エラーハンドリング改善

- [x] `InstallAndSaveAsync` / `UninstallAndSaveAsync` / `UpgradeAndSaveAsync` が winget 失敗時に非ゼロ終了コードを返す
  - 現状: Gist を更新しないが、CLI としては正常終了
  - 期待: 非ゼロ終了コードを返す
  - 関連ファイル: `src/GistGet/GistGet/GistGetService.cs`

### 4-2. upgrade の pin 追従バージョン取得

- [x] upgrade 成功後の pin 追従で「更新可能バージョン（UsableVersion）」ではなく、upgrade 後のインストール済みバージョンを取得
  - 関連ファイル: `src/GistGet/GistGet/GistGetService.cs`, `Infrastructure/WinGetService.cs`

### 4-3. uninstall のローカル pin 残存問題

- [x] Gist 側の `pin` 有無だけでなく、ローカルの pin も確認して削除
  - 関連ファイル: `src/GistGet/GistGet/GistGetService.cs`

### 4-4. upgrade ID 未指定時のパススルー引数

- [x] `ParseResult.Tokens` 依存で引数再構成が不安定な問題を修正
  - 関連ファイル: `src/GistGet/GistGet/Presentation/CommandBuilder.cs`

### 4-5. パススルー引数のクォート/エスケープ

- [x] `string.Join(" ", args)` でスペースを含む引数が壊れる問題を修正
  - 関連ファイル: `src/GistGet/GistGet/Infrastructure/WinGetPassthroughRunner.cs`

---

## 📋 Phase 5: クリーンアップ

### 5-1. GistGetPackage の不要プロパティ削除

仕様書に定義されていないプロパティ（削除候補）:

- [x] `Mode` プロパティ削除
- [x] `Confirm` プロパティ削除
- [x] `WhatIf` プロパティ削除
- 関連ファイル: `src/GistGet/GistGet/GistGetPackage.cs`

### 5-2. Gist ファイル名の統一

- [x] `GitHubService` のデフォルトファイル名 `gistget-packages.yaml` を `packages.yaml` に統一
- 関連ファイル: `src/GistGet/GistGet/Infrastructure/GitHubService.cs`

### 5-3. csproj 整理

- [x] `Microsoft.Identity.Client` を削除（Octokit で認証しており不要）
- 関連ファイル: `src/GistGet/GistGet.csproj`

### 5-4. 仕様書（SPEC）の不整合修正

- [x] `sync` 節の `--pin` オプション記載を `--version` に修正
- [x] `sync` は Gist → ローカルの片方向同期であり、書き戻しを行わないことを明記
- 関連ファイル: `docs/SPEC.ja.md`

---

## 🟢 正しく実装されている機能

| 機能 | 状態 | 備考 |
|------|:----:|------|
| `auth login` | ✅ | Device Flow 認証 |
| `auth logout` | ✅ | 資格情報削除 |
| `auth status` | ✅ | トークン状態表示 |
| `sync` | ✅ | Gist 同期 |
| `upgrade` (ID 未指定) | ✅ | パススルー |
| `pin list` / `pin reset` | ✅ | パススルー |
| winget パススルー (11 コマンド) | ✅ | list, search, show 等 |

---

## 📋 テスト追加が必要な項目

- [ ] YAML シリアライズで全フィールドが保存されること（Phase 1）
- [ ] custom オプションの正しいパススルー（Phase 1）
- [ ] winget 失敗時のエラー伝播（Phase 4）
- [ ] sync の同期マトリクス
- [ ] export / import の動作（Phase 3 実装後）
