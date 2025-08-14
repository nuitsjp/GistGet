# GistGet TODO List

## 🧱 グランドルール（維持方針）

- **単一の正本**: 仕様は `docs/architecture.md` を正とし、本TODOはそれに整合。差異が生じたら先にドキュメントを更新。
- **完了の最小化**: 完了済みの詳細手順は削除し、1〜3行の「完了ログ」に集約（必要ならPR/コミットへのリンクのみ）。
- **Start Here の明示**: 冒頭に「次にやること（Start Here）」を常設し、着手順の分かる5〜10項目に限定。
- **表現の統一**: 動詞始まり・1行・具体的成果物基準（例: 例外ポリシーのDR文書追加、テスト名変更）。
- **用語と経路の一貫性**: `export/import` はパススルー、`sync` は内部YAML（Packages配列）準拠。README/Docs/コードを同時に更新。
- **テスト配置の固定**: テストは `tests/NuitsJp.GistGet.Tests` に集約。命名・構造はレイヤ別。

## 🚦 次にやること（Start Here）

1. 例外と終了コードのポリシーを策定し `docs/architecture.md` にDRとして追記。
2. YAML（Packages配列）往復テストを追加（空/単/複/任意項目有無）。
3. Passthrough 出力のスナップショットテスト（正規化差分ゼロ）を追加。
4. GitHub Actions を Windows 専用に固定し、成果物パスを検証（READMEと一致）。
5. DPAPI によるトークン暗号化（保存/復旧/再認証）を実装し単体テスト追加。
6. `new-architecture.md` の重複情報を整理し、`architecture.md` への誘導を維持。

## 🧭 現状サマリ（README / docs 同期後）

- パススルー: `list/search/show/export/import` は `WinGetPassthrough` で動作。`CommandRouter` 明示ルーティング実装済み。
- COMクライアント: `Initialize` と `install` は COM 経由実装済み。`uninstall` は `IProcessWrapper` 経由フォールバック、`upgrade` は簡易実装。
- Gist: `GistSyncService` はスタブ。`gist set/status/show` コマンドあり。YAML は配列スキーマ（`Packages: - Id: ...`）で統一。
- 認証: `GitHubAuthService` あり。CI では `GIST_TOKEN` を使用する方針に更新（README 反映済み）。
- CI: Windows のみを対象（`windows-latest`）。Release 成果物パス修正済み（README 反映済み）。

---

## 🏗️ アーキテクチャ リファクタリング

### Phase 1: 名前空間の再編成（Abstractionsの解体）
- **現状**: 単一`Abstractions/`フォルダに全インターフェースが集約
- **問題**: レイヤー構造不明確、インフラ層の外部システム混在、過度な抽象化
- **目標**: レイヤーベース名前空間設計への移行
- **優先度**: 高（アーキテクチャ基盤整備）

### Phase 2: アーキテクチャ層の再設計 【完了ログ（要約）】
- 3層アーキテクチャへ移行し、CommandRouter/Services/Infrastructure の責務を確立。
- DI設定・GistConfig/GistSet の責務分離・関連テストの更新を完了。
- 参考: `AppHost.cs`, `Presentation/CommandRouter.cs`, `Business/*`。

## 次期フェーズ優先度

### Phase 1: 名前空間の再編成 【完了ログ（要約）】
- `Abstractions/` 不要。現3層構造が確立済み。追加作業なし。

### Phase 3: テスト設計リファクタリング（次期最優先）
**現状**: レイヤーベーステスト構造への移行が主要課題

### 完了ログ（整合性タスク）
- export/import パススルーの `CommandRouter` 明示ルーティング実装・E2Eテスト追加完了。
- WinGetComClient の `IProcessWrapper` 統一（`UninstallPackageAsync`、`FallbackToWingetExe`）完了。

追加の整合性タスク（高優先度）:
- [ ] `uninstall` の COM API 対応可否を確認し、不可なら正式にフォールバック設計を文書化
- [ ] 例外と終了コードのポリシー策定（表示/復帰値の一貫性）

## 🧪 テスト設計リファクタリング（t-wada式TDD対応）

### Phase 3: テスト名前空間の再編成
- **現状**: 単一テストプロジェクトに全テストが集約
- **目標**: レイヤーベーステスト構造への移行
- **優先度**: 中（テストアーキテクチャ整備）

**実装手順**:
- [ ] テストフォルダ構造をレイヤーベースに再編成（公式配置: `tests/NuitsJp.GistGet.Tests`）:
  - [ ] `Tests/Presentation/` フォルダ作成
    - [ ] `CommandRouterTests.cs` （現在のCommandServiceTests.cs）
    - [ ] `Commands/GistSetCommandTests.cs` 等のCommandテスト移動
  - [ ] `Tests/Business/` フォルダ作成  
    - [ ] `GistConfigServiceTests.cs` （新規統合Serviceテスト）
    - [ ] `GistSyncServiceTests.cs` 等のServiceテスト移動
  - [ ] `Tests/Infrastructure/` フォルダ作成
    - [ ] `WinGet/WinGetComClientTests.cs` 移動
    - [ ] `GitHub/GitHubAuthServiceTests.cs` 移動  
    - [ ] `Storage/GistConfigurationStorageTests.cs` 移動
- [ ] テスト名前空間の更新:
  - [ ] `NuitsJp.GistGet.Tests.Presentation`
  - [ ] `NuitsJp.GistGet.Tests.Business` 
  - [ ] `NuitsJp.GistGet.Tests.Infrastructure`
- [ ] モック実装の再編成:
  - [ ] `Tests/Mocks/Infrastructure/` フォルダ作成
    - [ ] `WinGet/MockWinGetClient.cs` 移動
    - [ ] `GitHub/MockGitHubAuthService.cs` 作成
    - [ ] `Storage/MockGistConfigurationStorage.cs` 作成
  - [ ] `Tests/Mocks/Business/` フォルダ作成
    - [ ] `MockGistConfigService.cs` 作成
    - [ ] `MockGistSyncService.cs` 移動・更新

**テスト観点の更新（README/docs 反映）**:
- [x] export/import は passthrough の E2E スモーク（`CommandRouter` → `IWinGetPassthroughClient` 呼び出しを検証）完了
- [ ] YAML 配列スキーマのシリアライズ/デシリアライズ往復テスト（空/単数/複数/オプション有無）
- [ ] Passthrough 出力のスナップショット比較（正規化後差分ゼロ）

### Phase 4: テスト戦略の層別分離
- **現状**: 混在したテスト責務（UI・ワークフロー・外部システムが同一テスト）
- **目標**: t-wada式TDD原則に基づく責務明確化
- **優先度**: 高（TDD品質向上）

**実装手順**:
- [ ] Presentation層テスト戦略実装:
  - [ ] UI制御のみをテスト対象とする
  - [ ] Business層サービスを完全モック化
  - [ ] 入力処理・表示処理・終了コードの検証に特化
  - [ ] CommandRouterのルーティング機能テストを分離
- [ ] Business層テスト戦略実装:
  - [ ] ワークフローとビジネスルールのみをテスト対象
  - [ ] Infrastructure層を完全モック化  
  - [ ] 処理順序・バリデーション・例外処理の検証に特化
  - [ ] 統合されたServiceクラスのワークフローテスト追加
- [ ] Infrastructure層テスト戦略実装:
  - [ ] 外部システム連携の個別テストに特化
  - [ ] 外部システムをモック/スタブ化
  - [ ] API呼び出し・データ変換・エラーハンドリングの検証
  - [ ] COM API、GitHub API、ファイルシステムの分離テスト

### Phase 5: t-wada式TDD テストケース再設計
- **現状**: 従来のテストケース設計
- **目標**: Red-Green-Refactorサイクル対応のテストケース再設計  
- **優先度**: 高（TDD品質向上）

**実装手順**:
- [ ] Redフェーズテストケース作成:
  - [ ] 各レイヤーで失敗するテストケースを先に作成
  - [ ] 期待する動作を明確に定義した失敗テスト
  - [ ] エラーハンドリングの失敗パターンテスト
- [ ] Greenフェーズテスト検証:
  - [ ] 最小限の実装でテストが通ることを確認
  - [ ] 各レイヤーの責務境界を明確にしたテスト
  - [ ] モック設定の最適化と依存関係の明確化
- [ ] Refactorフェーズテスト保護:
  - [ ] リファクタリング後もテストが保護されることを確認
  - [ ] テスト自体のリファクタリング（重複除去、可読性向上）
  - [ ] テストケース名の意図明確化

## 🔒 セキュリティ強化タスク

### トークンのDPAPI暗号化
- **現状**: GitHub認証トークンが平文で保存されている (`%APPDATA%\GistGet\token.json`)
- **目標**: Windows DPAPI (Data Protection API) による暗号化保存
- **実装箇所**: `GitHubAuthService.cs` の `SaveTokenAsync` / `LoadTokenAsync` メソッド
- **優先度**: 高（セキュリティ脆弱性）

**実装手順**:
- [ ] `System.Security.Cryptography.ProtectedData` を使用してトークンを暗号化
- [ ] `DataProtectionScope.CurrentUser` での暗号化実装
- [ ] 既存の平文トークンファイルからの移行処理
- [ ] 復号化失敗時の再認証プロンプト機能
- [ ] 暗号化保存のテストケース追加

### CI/CD とドキュメント整合
- [ ] GitHub Actions を `windows-latest` のみに限定（Windows ターゲティング依存）
- [ ] Release 成果物パスを `net8.0-windows10.0.26100/win-x64/publish/GistGet.exe` に固定
- [ ] 環境変数名を `GIST_TOKEN` に統一（README/ワークフロー/コード）
- [ ] docs 内の辞書スキーマ参照を配列スキーマに一掃（相互参照の確認）

### トークンの定期的な更新機能
- **現状**: 取得したトークンは無期限で使用される
- **目標**: 定期的なトークンの更新によるセキュリティ向上
- **実装箇所**: `GitHubAuthService.cs` および新規スケジューラー機能
- **優先度**: 中（セキュリティ向上）

**実装手順**:
- [ ] トークン有効期限チェック機能の実装
- [ ] トークン更新（refresh）機能の調査・実装
- [ ] バックグラウンドでの自動更新スケジューラー
- [ ] 更新失敗時の再認証フロー
- [ ] トークン更新のログ・通知機能
- [ ] 設定可能な更新間隔（デフォルト30日）

