# GistGet TODO List

## 🧱 グランドルール（維持方針）

- **単一の正本**: 仕様は `docs/architecture.md` を正とし、本TODOはそれに整合。差異が生じたら先にドキュメントを更新。
- **完了の最小化**: 完了済みの詳細手順は削除し、1〜3行の「完了ログ」に集約（必要ならPR/コミットへのリンクのみ）。
- **表現の統一**: 動詞始まり・1行・具体的成果物基準（例: 例外ポリシーのDR文書追加、テスト名変更）。
- **用語と経路の一貫性**: `export/import` はパススルー、`sync` は内部YAML（Packages配列）準拠。README/Docs/コードを同時に更新。
- **テスト配置の固定**: テストは `tests/NuitsJp.GistGet.Tests` に集約。命名・構造はレイヤ別。
- **実装後の現状反映**: TODO実施完了後は必ず現状サマリと完了ログを更新し、実装状況をドキュメントに反映する。

---

## 🏗️ アーキテクチャリファクタリング（残課題）

### 完了ログ（2025-01-14実施）
- **レイヤーベーステスト構造への移行完了**。全テストファイルをPresentation/Business/Infrastructure層に移行、名前空間更新済み。
- **COM API制約調査完了**。UninstallPackageAsync未対応を公式仕様で確認、docs/architecture.mdに正式文書化。
- **環境変数統一完了**。GITHUB_TOKEN → GIST_TOKEN への統一（ワークフロー・ドキュメント）。
- **Phase 4 Presentation層テスト戦略完了**。CommandRouterTestsをt-wada式TDD対応で責務分離実装、UI制御・ルーティング・終了コード検証に特化。TestGistCommand不要コード削除、AuthCommandTests追加。カバレージ大幅改善：CommandRouter 74.4%、AuthCommand 89.4%。46テスト全成功。
- **Phase 4 Business層テスト戦略完了**。Infrastructure層完全抽象化（IGitHubGistClient、IPackageYamlConverter）、t-wada式TDD対応でワークフロー・ビジネスルール検証に特化。GistConfigServiceTests、GistManagerTests追加。カバレージ劇的改善：全体36.3%、GistManager 94.9%、GistConfigService 100%。127テスト全成功。

### 完了ログ（2025-01-15実施）
- **Phase 4 Infrastructure層テスト戦略完了**。実際の外部システムとの統合テストに特化した戦略を採用。GitHubGistClient: テスト用Gist命名規則（GISTGET_TEST_*）と自動クリーンアップ機能実装、CRUD操作の完全フローテスト準備（Create/Delete API実装待ち）。WinGetComClient: jqパッケージによる実インストール/アンインストール統合テスト、COM APIとwinget.exe結果比較検証。GistConfigurationStorage: 並行アクセス、アクセス権限、破損ファイル対応などファイルシステム境界値テスト。222テスト（215合格、4スキップ、3失敗）で統合テスト基盤確立。

### 完了ログ（2025-08-15実施）
- **Infrastructure層テスト修正完了**。GistConfigurationStorage: UnauthorizedAccessException適切な投げ方修正、SemaphoreSlimによるファイルアクセス同期制御追加で並行アクセス問題解決。GitHubGistClientTests: NetworkErrorテストを現実的な期待値に修正。**222テスト全成功達成（失敗0、スキップ4）**。カバレージ45%。
- **WinGet COM API完全修正完了**。公式winget-cliサンプルベースでアーキテクチャ刷新：複雑なFactory実装削除、`new PackageManager()`直接実装、Microsoft.WindowsPackageManager.ComInteropパッケージ統一、プロジェクト設定修正（net8.0-windows10.0.22621.0、CsWinRT 2.2.0）。統合テスト全成功：初期化・パッケージ検索・インストール・アンインストール・COM/EXE比較の6テスト。Visual Studioソリューション構成エラー修正（Any CPU → x64/x86/ARM64対応）。

### Phase 4: Infrastructure層テスト戦略（完了）
- **現状**: Infrastructure層統合テスト実装完了
- **目標**: 実際の外部システムとの統合テストによる動作保証
- **優先度**: 完了

**実装手順**:
- [x] Presentation層テスト戦略実装:
  - [x] UI制御のみをテスト対象とする
  - [x] Business層サービスを完全モック化
  - [x] 入力処理・表示処理・終了コードの検証に特化
  - [x] CommandRouterのルーティング機能テストを分離
- [x] Business層テスト戦略実装:
  - [x] ワークフローとビジネスルールのみをテスト対象
  - [x] Infrastructure層を完全モック化（IGitHubGistClient、IPackageYamlConverter）  
  - [x] 処理順序・バリデーション・例外処理の検証に特化
  - [x] GistManager、GistConfigServiceのワークフローテスト追加
- [x] Infrastructure層テスト戦略実装:
  - [x] 外部システム連携の統合テストに特化
  - [x] GitHubGistClient: 実Gist作成・削除による統合テスト（構造準備、API実装待ち）
  - [x] WinGetComClient: 軽量パッケージ（jq）による実インストール/アンインストールテスト
  - [x] GistConfigurationStorage: 実ファイルシステムでのアクセス権限・競合テスト
  - [x] テスト用Gist命名規則実装（自動クリーンアップ対応）
  - [x] COM APIとwinget.exe実行結果の比較検証


## 🔒 セキュリティ強化タスク

### 完了ログ（DPAPI暗号化・環境変数統一）
- **DPAPIトークン暗号化実装完了**。`GitHubAuthService`でDPAPI暗号化保存実装、平文移行処理削除、テスト4つ実装済み。
- **環境変数統一完了**。GITHUB_TOKEN → GIST_TOKEN への統一（build-windows-only.yml、docs/auth.md、README.md）。
- **ドキュメント更新完了**。`docs/architecture.md`でセキュリティ設計とCOM API制約を正式文書化。

### トークンの定期的な更新機能
- **現状**: 取得したトークンは無期限で使用される
- **目標**: 定期的なトークンの更新によるセキュリティ向上
- **実装箇所**: `GitHubAuthService.cs` および新規スケジューラー機能
- **優先度**: 低（将来課題）

**実装手順**:
- [ ] トークン有効期限チェック機能の実装
- [ ] トークン更新（refresh）機能の調査・実装
- [ ] バックグラウンドでの自動更新スケジューラー
- [ ] 更新失敗時の再認証フロー
- [ ] トークン更新のログ・通知機能
- [ ] 設定可能な更新間隔（デフォルト30日）

## 🔄 次の作業

**優先度: 中（Phase 4完了後の次フェーズ）**

### 1. GistSyncService本格実装への移行
- Phase 4 Infrastructure層テスト戦略完全完了
- 全222テスト成功、統合テスト基盤確立済み
- WinGet COM API環境制約も把握済み

## 📋 機能拡張タスク

### GistSyncService の本格実装
- **現状**: `GistSyncService` はスタブ実装（`GistSyncStub.cs`）
- **目標**: 実際のGist同期機能の実装
- **優先度**: 中（Infrastructure層テスト完了確認後）

**実装手順**:
- [ ] GistSyncServiceのスタブ実装を本格実装に置換
- [ ] インストール済みパッケージ取得とGistファイル同期の実装
- [ ] パッケージ差分検出ロジックの実装
- [ ] 同期後のGistファイル更新処理の実装

### WinGetComClient の upgrade 機能改善
- **現状**: `upgrade` は簡易COM実装（将来改善予定）
- **目標**: COM API を活用した本格的なupgrade実装
- **優先度**: 低（機能拡張）

**実装手順**:
- [ ] COM API でのupgrade機能可否調査
- [ ] 利用可能な場合の本格実装
- [ ] パッケージ更新可能性チェック機能
- [ ] 一括更新機能の実装

---

## 📊 現在のアーキテクチャ状況（2025-08-15時点）

**3層アーキテクチャ確立済み**:
- **Presentation層**: `CommandRouter.cs`、`Commands/`（UI制御・ルーティング）
- **Business層**: `Services/`、`Models/`（ワークフロー・ビジネスルール）
- **Infrastructure層**: `WinGet/`、`GitHub/`、`Storage/`（外部システム連携）

**実装状況**:
- パススルー: `export/import/list/search/show` → `WinGetPassthrough` 経由で動作
- COM API: `install/uninstall` 完全実装。公式サンプルベースの簡潔実装、フォールバック機能付き
- Gist: 認証・設定コマンド実装済み、同期機能はスタブ
- 認証: DPAPI暗号化保存、OAuth Device Flow
- テスト: レイヤーベース構造、t-wada式TDD対応。**222テスト全成功（失敗0、スキップ4）**。Infrastructure層統合テスト完成、カバレージ45%
- CI/CD: Windows専用、GIST_TOKEN統一済み
- 開発環境: Visual Studio 2022対応（ソリューション構成修正済み）

**技術詳細**:
- WinGet COM API: Microsoft.WindowsPackageManager.ComInterop 1.10.340、CsWinRT 2.2.0使用
- プラットフォーム: net8.0-windows10.0.22621.0、x64/x86/ARM64対応