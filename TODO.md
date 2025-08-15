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



## 🚀 次の作業：Command-Console分離リファクタリング

**優先度**: 高（Phase 0：syncコマンド実装の前提条件）

### Phase 0: Command-Console分離基盤構築

**設計原則**: CommandからUI詳細を完全分離し、コマンド固有の高レベルインターフェースによる抽象化を実現

**実装手順**:
- [x] Console基盤作成（IConsoleBase, ConsoleBase） - **完了**
- [x] Syncコマンド分離（ISyncConsole, SyncConsole, Sync/SyncCommand移動） - **部分完了（基盤作成済み）**
- [ ] Authコマンド分離（IAuthConsole, AuthConsole, Auth/AuthCommand移動）
- [ ] Gist関連コマンド分離（IGistConfigConsole, GistConfigConsole, 各コマンド移動）
- [ ] CommandRouter更新（新しい名前空間対応）
- [ ] AppHost.cs更新（DI設定）
- [ ] 単体テスト更新（高レベルインターフェースモック）

**現在の実装状況（引き継ぎ用）**:
- **Console基盤完了**: `IConsoleBase.cs`, `ConsoleBase.cs` 作成済み（共通エラー処理・進捗表示）
- **Sync分離完了**: `ISyncConsole.cs`, `SyncConsole.cs`, `SyncCommand.cs` 完全実装済み
  - 場所: `src/NuitsJp.GistGet/Presentation/Sync/`
  - 旧SyncCommand削除済み（`Presentation/Commands/SyncCommand.cs`）
  - 高レベル抽象化完了（`NotifySyncStarting()`, `ShowSyncResultAndGetAction()`, `ConfirmRebootWithPackageList()`等）
  - SyncUserActionエナム定義済み（Continue, SkipReboot, ForceReboot, Cancel）
- **残りのコマンド分離**:
  - AuthCommand分離（IAuthConsole, AuthConsole, Auth/AuthCommand移動）
  - Gist関連コマンド分離（IGistConfigConsole, GistConfigConsole, 各コマンド移動）
- **設定ファイル更新待ち**:
  - CommandRouter: using文更新とSyncCommandコメント復旧
  - AppHost.cs: SyncCommandのDI登録復旧と新Console実装の登録

**第三者引き継ぎのためのファイル構造（現在の状態）**:
```
src/NuitsJp.GistGet/Presentation/
├── Console/                    # 新規作成済み
│   ├── IConsoleBase.cs        # 共通基盤インターフェース ✅
│   └── ConsoleBase.cs         # 共通基盤実装 ✅
├── Sync/                      # 新規作成済み  
│   ├── ISyncConsole.cs        # Sync固有インターフェース ✅
│   ├── SyncConsole.cs         # Sync実装 ✅
│   └── SyncCommand.cs         # 新Syncコマンド ✅
└── Commands/                  # 既存（分離対象）
    ├── AuthCommand.cs         # ⚠ 要分離
    ├── GistSetCommand.cs      # ⚠ 要分離
    ├── GistStatusCommand.cs   # ⚠ 要分離
    └── GistShowCommand.cs     # ⚠ 要分離
```

**重要**: 現在の`SyncCommand`は新しい設計で完全実装済みだが、`CommandRouter`と`AppHost.cs`の参照がまだ古い名前空間を指しているため、ビルドエラーが発生する。次の作業者は、まず残りのコマンド分離を完了させ、その後DI設定を更新する必要がある。

**ビルドエラー状況**:
```
D:\GistGet\src\NuitsJp.GistGet\Presentation\CommandRouter.cs(21,22): error CS0246: 
型または名前空間の名前 'SyncCommand' が見つかりませんでした
```
- 原因: 古い `using NuitsJp.GistGet.Presentation.Commands;` が新しい `NuitsJp.GistGet.Presentation.Sync` を参照していない
- 解決方法: 全コマンド分離完了後、CommandRouterのusing文とDI設定を一括更新

**その他のConsole.WriteLine名前空間エラー**:
- ConsoleBase.cs でIProgressIndicatorインターフェースが未定義
- 解決方法: IProgressIndicatorインターフェースを追加するか、SimpleProgressIndicatorを単純化

**詳細な次回作業手順（第三者向け）**:

1. **IProgressIndicatorインターフェース修正**:
   ```csharp
   // src/NuitsJp.GistGet/Presentation/Console/IProgressIndicator.cs として作成
   public interface IProgressIndicator : IDisposable
   {
       void UpdateMessage(string message);
       void Complete();
   }
   ```

2. **AuthCommand分離パターン**（SyncCommandと同様）:
   ```csharp
   // src/NuitsJp.GistGet/Presentation/Auth/IAuthConsole.cs
   public interface IAuthConsole : IConsoleBase
   {
       void ShowAuthInstructions(string deviceCode, string userCode, string verificationUri);
       bool ConfirmTokenStorage();
       void NotifyAuthSuccess();
       void NotifyAuthFailure(string reason);
   }
   
   // src/NuitsJp.GistGet/Presentation/Auth/AuthConsole.cs - 実装
   // src/NuitsJp.GistGet/Presentation/Auth/AuthCommand.cs - 移動＋リファクタ
   ```

3. **GistConfig分離パターン**:
   ```csharp
   // src/NuitsJp.GistGet/Presentation/GistConfig/IGistConfigConsole.cs
   public interface IGistConfigConsole : IConsoleBase
   {
       void ShowCurrentConfiguration(string gistId, string fileName);
       (string gistId, string fileName) RequestGistConfiguration();
       void NotifyConfigurationSaved();
   }
   ```

4. **CommandRouter更新**:
   ```csharp
   // using文追加
   using NuitsJp.GistGet.Presentation.Sync;
   using NuitsJp.GistGet.Presentation.Auth;
   using NuitsJp.GistGet.Presentation.GistConfig;
   
   // コンストラクタ引数とフィールドのコメント復旧
   ```

5. **AppHost.cs DI登録**:
   ```csharp
   // Console services
   services.AddSingleton<ISyncConsole, SyncConsole>();
   services.AddSingleton<IAuthConsole, AuthConsole>();
   services.AddSingleton<IGistConfigConsole, GistConfigConsole>();
   
   // Commands (名前空間変更後)
   services.AddSingleton<NuitsJp.GistGet.Presentation.Sync.SyncCommand>();
   services.AddSingleton<NuitsJp.GistGet.Presentation.Auth.AuthCommand>();
   ```

**重要な設計注意点**:
- **高レベル抽象化**: 各コンソールインターフェースは`Console.WriteLine`レベルではなく、業務レベルの操作（`ShowAuthInstructions`, `ConfirmRebootWithPackageList`など）を提供
- **テスタビリティ**: 高レベルインターフェースにより、テストでの詳細UI制御が不要
- **多言語対応準備**: 将来的にConsole実装を差し替えることで多言語化対応可能
- **責務分離**: Commandクラスは業務フローに専念、UI詳細はConsole実装に委譲

**完了確認方法**:
1. `dotnet build` でエラー0になること
2. 全既存テストが通ること
3. 新しいコンソール抽象化でSyncCommandが正常動作すること

## 🔄 Phase 5：syncコマンド実装

**優先度**: 高（Command-Console分離完了後）

### Phase 5: syncコマンド実装
- **現状**: Command-Console分離部分完了、syncコマンド仕様書作成完了
- **目標**: docs/gistget/sync.mdに基づくsyncコマンドの完全実装
- **実装範囲**: SyncCommand（ISyncConsole使用済み）、GistSyncService本格実装、SyncPlanモデル、再起動処理

**実装手順**:
- [ ] SyncPlanモデル作成 (Business層)
- [ ] SyncResultモデル作成 (Business層)
- [ ] GistSyncService本格実装 (GistSyncStub.csを置換)
- [x] SyncCommand作成 (Presentation/Sync層、ISyncConsole使用) - **完了**
- [ ] CommandRouterに"sync"コマンド追加（名前空間変更対応）
- [ ] 再起動処理実装 (CheckRebootRequired, ExecuteRebootAsync)
- [ ] 差分検出ロジック実装 (DetectDifferences)
- [ ] syncコマンドテスト作成 (ISyncConsoleモック使用)
- [ ] DI登録更新 (AppHost.cs)
- [ ] 動作確認とテスト実行

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
- **Presentation層**: `CommandRouter.cs`、`Commands/`（UI制御・ルーティング）
- **Business層**: `Services/`、`Models/`（ワークフロー・ビジネスルール）
- **Infrastructure層**: `WinGet/`、`GitHub/`、`Storage/`（外部システム連携）

**実装状況**:
- パススルー: `export/import/list/search/show` 動作確認済み
- COM API: `install/uninstall` 完全実装。222テスト全成功
- Gist: 認証・設定コマンド実装済み、**syncコマンド仕様書作成完了**
- **Command-Console分離**: 部分実装済み（Sync基盤完了、残りのコマンド分離作業中）
- 認証: DPAPI暗号化保存、OAuth Device Flow
- テスト: レイヤーベース構造、t-wada式TDD対応。Infrastructure層統合テスト完成
- CI/CD: Windows専用、GIST_TOKEN統一済み
- 開発環境: Visual Studio 2022対応

**技術詳細**:
- WinGet COM API: Microsoft.WindowsPackageManager.ComInterop 1.10.340、CsWinRT 2.2.0使用
- プラットフォーム: net8.0-windows10.0.22621.0、x64/x86/ARM64対応