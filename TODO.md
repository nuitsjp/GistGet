# GistGet TODO List

## 最優先ルール

t_wada式TDDの徹底

## 📋 運用ルール

- **完了済みフェーズ**: 何をしたかの記録（変更不要）
- **残りタスク**: フェーズレベルの大まかな計画（全体把握用）
- **次フェーズ**: 具体的な手順チェックシート（実作業用）
- **1フェーズ完了**: 次フェーズを詳細化し、完了フェーズに移動

## 🏗️ アーキテクチャ リファクタリング

### Phase 1: 名前空間の再編成（Abstractionsの解体）
- **現状**: 単一`Abstractions/`フォルダに全インターフェースが集約
- **問題**: レイヤー構造不明確、インフラ層の外部システム混在、過度な抽象化
- **目標**: レイヤーベース名前空間設計への移行
- **優先度**: 高（アーキテクチャ基盤整備）

**実装手順**:
- [ ] `Abstractions/`フォルダを解体
- [ ] レイヤー別名前空間にインターフェース移動:
  - [ ] `ICommandService` → `Presentation/ICommandRouter.cs`
  - [ ] `IErrorMessageService` → `Presentation/IErrorMessageService.cs`
  - [ ] `IGistSyncService` → `Business/IGistSyncService.cs`
  - [ ] `IGitHubAuthService` → `Infrastructure/GitHub/IGitHubAuthService.cs`
  - [ ] `IWinGetClient` → `Infrastructure/WinGet/IWinGetClient.cs`
- [ ] インフラ層の外部システム別再編成:
  - [ ] `Infrastructure/WinGet/` フォルダ作成・ファイル移動
  - [ ] `Infrastructure/GitHub/` フォルダ作成・ファイル移動
  - [ ] `Infrastructure/Storage/` フォルダ作成・ファイル移動
- [ ] 名前空間変更に伴うusing文の更新
- [ ] テストファイルの名前空間更新

### Phase 2: アーキテクチャ層の再設計
- **現状**: 4層アーキテクチャ（プレゼンテーション・アプリケーション・ドメイン・インフラ）
- **問題**: ドメイン層が薄すぎ、Commands/Services の責務が曖昧、名称がルーター機能を表現していない
- **目標**: シンプルな3層アーキテクチャ（プレゼンテーション・ビジネスロジック・インフラ）
- **優先度**: 中（保守性向上）

**実装手順**:
- [ ] CommandService → CommandRouterに名称変更（ルーティング機能を明確化）
- [ ] Commands層の責務をUI制御のみに限定
- [ ] Services層にワークフロー制御機能を統合
- [ ] 現在のGistInputService等の細分化されたServiceを統合
- [ ] CommandからのService直接操作を廃止
- [ ] 新しい責務分離に基づくテストケースの更新

### 具体的なリファクタリング対象

#### GistSetCommand の責務分離
**現在**:
```csharp
// Commands層がService操作を直接制御
await _authService.IsAuthenticatedAsync();
await _gistManager.ValidateGistAccessAsync(gistId);
await _storage.SaveGistConfigurationAsync(config);
```

**目標**:
```csharp
// UI制御のみに特化
var input = await CollectUserInputAsync(gistId, fileName);
await _gistConfigService.ConfigureGistAsync(input);
DisplaySuccessMessage();
```

#### Service統合案
- `GistConfigService`: Gist設定のワークフロー全体
- `GistSyncService`: Gist同期のワークフロー全体
- `WinGetService`: WinGet操作のワークフロー全体

**Phase 1影響範囲** (名前空間再編成):
- `Abstractions/` フォルダ: 完全解体
- `Services/`, `Commands/`, `Models/` 等: 新名前空間への移動
- `Infrastructure/` フォルダ: 外部システム別サブフォルダ作成
- 全ソースファイル: using文の更新
- `Tests/` ディレクトリ: テスト名前空間の更新
- `AppHost.cs`: DI設定の名前空間更新

**Phase 2影響範囲** (責務再設計):
- `CommandService.cs` → `CommandRouter.cs`: ファイル名変更・クラス名変更
- `Commands/` ディレクトリ: 全Command クラス
- `Services/` ディレクトリ: Service 統合・責務再分離
- `Tests/` ディレクトリ: テスト戦略の見直し
- `AppHost.cs`: DI設定の更新（ICommandService → ICommandRouter）

## 🧪 テスト設計リファクタリング（t-wada式TDD対応）

### Phase 3: テスト名前空間の再編成
- **現状**: 単一テストプロジェクトに全テストが集約
- **目標**: レイヤーベーステスト構造への移行
- **優先度**: 中（テストアーキテクチャ整備）

**実装手順**:
- [ ] テストフォルダ構造をレイヤーベースに再編成:
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

