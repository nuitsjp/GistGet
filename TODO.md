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
- **Infrastructure層テスト修正完了**。222テスト全成功。WinGet COM API完全修正。Visual Studioソリューション構成修正。
- **syncコマンド仕様書作成完了**。docs/gistget/sync.mdを新規作成。Gist→ローカルの一方向同期、再起動処理、Mermaidシーケンス図、クラス設計を含む詳細仕様。docs/architecture.md最新化。
- **Command-Console分離アーキテクチャ設計完了**。docs/architecture.mdに新セクション2.3追加、TODO.mdにPhase 0設定。高レベル抽象化によるUI詳細隠蔽設計確立。
- **Phase 0: Command-Console分離基盤構築完了**。Console抽象化レイヤー実装（IConsoleBase, ConsoleBase）、Auth分離（IAuthConsole, AuthConsole, AuthCommand）、GistConfig分離（IGistConfigConsole, GistConfigConsole, 各コマンドクラス）、Sync分離（ISyncConsole, SyncConsole, SyncCommand）完了。CommandRouter名前空間対応、DI循環依存解決、旧Commands削除。187テスト全成功、git commit完了。
- **Phase 1: syncコマンド実装完了**。GistSyncService本格実装（差分検出、同期実行、再起動処理）、SyncPlan/SyncResultモデル作成、t-wada式TDDテスト30個（GistSyncServiceTests 16個、SyncCommandTests 14個）全成功。PC再起動問題解決のためIOsServiceアーキテクチャ導入（Infrastructure/Os層、テスト時モック化）。syncコマンド完全動作対応。



## 🔄 次の作業：機能拡張・改良

**優先度**: 中（Phase 1完了により基盤確立）

### Phase 2: syncコマンド拡張機能

**現状**: syncコマンド基本実装完了（Gist→ローカル一方向同期、再起動処理、テスト完備）  
**目標**: 仕様書の拡張機能実装とUI改良  
**実装範囲**: --dry-run、--force、--no-restart、進捗表示、エラー詳細表示

**実装手順**:
- [ ] --dry-run実装 (プレビュー機能)
- [ ] --force実装 (確認プロンプトスキップ)  
- [ ] --no-restart実装 (手動再起動委譲)
- [ ] 進捗表示UI実装 (SyncConsole拡張)
- [ ] エラー詳細表示改良
- [ ] 複数Gistファイル対応検討
- [ ] パフォーマンス最適化

## 🔒 将来課題：セキュリティ強化

**優先度**: 低（sync実装後の将来課題）

### 完了ログ（DPAPI暗号化・環境変数統一）
- **DPAPIトークン暗号化実装完了**。
- **環境変数統一完了**。GITHUB_TOKEN → GIST_TOKEN。
- **セキュリティ設計文書化完了**。

### トークン定期更新機能
- トークン有効期限チェック機能
- トークン更新機能調査・実装
- 自動更新スケジューラー

## 📋 機能拡張タスク

**優先度**: 低（sync実装後の機能拡張）

### WinGetComClient upgrade機能改善
- COM API活用本格的upgrade実装
- パッケージ更新可能性チェック
- 一括更新機能

---

## 📊 現在のアーキテクチャ状況（2025-08-15時点）

**3層アーキテクチャ確立済み**:
- **Presentation層**: `CommandRouter.cs`、Auth/GistConfig/Sync各分離フォルダ、Console抽象化レイヤー（UI制御・ルーティング）
- **Business層**: `Services/`、`Models/`（ワークフロー・ビジネスルール）
- **Infrastructure層**: `WinGet/`、`GitHub/`、`Storage/`（外部システム連携）

**実装状況**:
- パススルー: `export/import/list/search/show` 動作確認済み
- COM API: `install/uninstall` 完全実装。222テスト全成功
- **Gist**: 認証・設定・**sync**コマンド完全実装（Gist→ローカル一方向同期、再起動処理、差分検出）
- **Command-Console分離**: Phase 0完了（Auth、GistConfig、Sync全分離、CommandRouter対応、DI設定完了）
- **syncコマンド**: Phase 1完了（GistSyncService、SyncPlan/Result、IOsService、30テスト全成功）
- 認証: DPAPI暗号化保存、OAuth Device Flow
- テスト: レイヤーベース構造、t-wada式TDD対応。Infrastructure層統合テスト完成。217テスト全成功
- CI/CD: Windows専用、GIST_TOKEN統一済み
- 開発環境: Visual Studio 2022対応

**技術詳細**:
- WinGet COM API: Microsoft.WindowsPackageManager.ComInterop 1.10.340、CsWinRT 2.2.0使用
- プラットフォーム: net8.0-windows10.0.22621.0、x64/x86/ARM64対応