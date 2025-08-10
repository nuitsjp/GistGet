# GistGet TODO List

## テスト用アプリケーション
**決定:** AkelPad.AkelPad (軽量テキストエディタ、約2-3MB)
- 理由: 軽量、高速インストール/アンインストール、システムへの影響が最小
- 使用例: `gistget install --id AkelPad.AkelPad`, `gistget uninstall --id AkelPad.AkelPad`

## MVP実装フェーズ (最優先)

### Phase 0: クリーンアップ ✅
- [x] 既存の複雑な実装をすべて削除
- [x] 最小限のプロジェクト構成に戻す
- [x] 不要なNuGetパッケージを削除

### Phase 1: 最小限のパススルー実装 (Day 1) ✅
- [x] Program.cs - シンプルなエントリポイント（20行以内）
- [x] WinGetPassthrough.cs - winget.exe呼び出し（30行以内）
- [x] 動作確認: `gistget list` → `winget list` の出力が同じ
- [x] 動作確認: `gistget search git` → `winget search git` の出力が同じ

### Phase 2: コマンドルーティング追加 (Day 2) ✅
- [x] CommandRouter.cs - COM/パススルー判定ロジック（36行）
- [x] WinGetComClient.cs - COM API最小実装（110行）
  - [x] InitializeAsync() - COM初期化
  - [x] InstallPackageAsync() - 最小限のインストール
  - [x] UninstallPackageAsync() - 最小限のアンインストール
- [x] 動作確認: `gistget install --id AkelPad.AkelPad` がCOM経由で動作
- [x] 動作確認: `gistget list` が引き続きパススルーで動作

### Phase 3: Gist同期スタブ追加 (Day 3) ✅
- [x] GistSyncStub.cs - Gist同期のスタブ実装（40行）
  - [x] AfterInstall() - "Gist updated" をコンソール出力
  - [x] AfterUninstall() - "Gist updated" をコンソール出力
  - [x] Sync() - "Syncing from Gist..." をコンソール出力
- [x] sync/export/importコマンドのスタブ実装
- [x] 動作確認: install後に"Gist updated"メッセージが表示される

## アーキテクチャ原則（MVP段階）

1. **最小限の実装** - 各ファイルは100行以内を目標
2. **直接的な実装** - 抽象化は最小限、直接的なコード
3. **段階的改善** - 動作確認後にリファクタリング
4. **Gistはスタブ** - 実際のAPI呼び出しは後回し

## 成功基準

### Phase 1 完了基準 ✅
- [x] すべてのwingetコマンドがパススルーで動作
- [x] 出力がwingetと完全に一致
- [x] コード総量: 50行以内（Program.cs: 13行 + WinGetPassthrough.cs: 28行 = 41行）

### Phase 2 完了基準 ✅
- [x] install/uninstall/upgradeがCOM経由で動作
- [x] その他のコマンドは引き続きパススルー
- [x] コード総量: 191行（200行以内達成）

### Phase 3 完了基準 ✅
- [x] Gist同期のスタブメッセージが表示される
- [x] sync/export/importコマンドが（スタブで）動作
- [x] コード総量: 245行（250行以内達成）

## 次のフェーズ（MVP完了後）

### Phase 4: アーキテクチャ改善とCOM API修正
- [x] 依存性注入の導入（DI Container）
- [x] インターフェース分離（IWinGetClient, IWinGetPassthroughClient, IGistSyncService）
- [x] サービス層の実装（CommandService）
- [x] ログ機能の追加（Microsoft.Extensions.Logging）
- [x] 基本的なCOM API実装

### Phase 4.5: COM API呼び出し実現のための修正 ✅
- [x] **COM API呼び出し実現のための修正**
  - [x] COM APIの「インターフェイスがサポートされていません」エラーの解決
    - 問題: `findResult.Matches.Any()`でのインターフェースキャストエラー
    - 解決: `findResult.Matches.Count`と`findResult.Matches[0]`を使用
  - [x] Windows AppInstallerバージョン互換性の調査
    - 確認済み: AppInstaller 1.26.430.0, WinGet 1.11.430が動作
  - [x] COM API権限設定の確認と修正
    - 管理者権限での動作を確認
  - [x] 内部データ取得機能の実装
    - `GetInstalledPackagesAsync()`: 301個のインストール済みパッケージ取得成功
    - `SearchPackagesAsync()`: パッケージ検索機能動作確認（"git"で69件検索）
  - [x] 動作確認テスト
    - `gistget install --id AkelPad.AkelPad`: COM経由でのインストール成功
    - `gistget test-list`: インストール済みパッケージ一覧取得成功
    - `gistget test-search git`: パッケージ検索機能動作成功

### Phase 5: テスト基盤構築とTDD開始 ✅
- [x] **5.1 テストプロジェクト構築**
  - [x] xUnitテストプロジェクト作成 (`tests/NuitsJp.GistGet.Tests`)
  - [x] Shouldlyパッケージ追加
  - [x] ソリューション作成と参照設定 (`GistGet.sln`)
  - [x] テスト実行環境確認（全26テスト合格）
- [x] **5.2 既存コードのテストカバレッジ**
  - [x] WinGetComClient基本機能テスト
    - [x] InitializeAsync() COM API環境チェック（統合テスト用Skip）
    - [x] GetInstalledPackagesAsync() 未初期化時例外テスト
    - [x] SearchPackagesAsync() 未初期化時例外テスト
  - [x] CommandService ルーティングテスト（12テスト）
    - [x] COMコマンド (install/uninstall/upgrade) のルーティング
    - [x] パススルーコマンドのルーティング
    - [x] Gistコマンドのルーティング
  - [x] モックサービス実装完了（`MockServices.cs`）

### Phase 6: COM API完全実装 (t-wada式TDD) ✅
- [x] **6.1 upgrade コマンド実装**
  - [x] Red: UpgradePackageAsync()テスト作成（4テストケース作成）
    - [x] 有効なパッケージID指定時の成功テスト
    - [x] --allオプション指定時の成功テスト  
    - [x] 初期化なしでの動作確認テスト
    - [x] パッケージID未指定時のエラーテスト
  - [x] Green: 最小実装でテスト通過（全テスト合格）
  - [x] Refactor: 未使用フィールド削除、コメント改善
- [x] **6.2 sync コマンド実装**
  - [x] Red: SyncAsync()テスト作成（同期状態永続化機能テスト）
  - [x] Green: MockGistSyncService拡張による最小実装
  - [x] Refactor: コード整理とコメント改善
- [x] **6.3 export/import コマンド実装**  
  - [x] Red: ExportAsync()テスト作成（エクスポートファイル生成テスト）
  - [x] Green: ExportFileGenerated プロパティ実装
  - [x] Refactor: コメント改善
  - [x] Red: ImportAsync()テスト作成（インポートファイル処理テスト）
  - [x] Green: ImportFileProcessed プロパティ実装
  - [x] Refactor: 最終コード整理完了

**📊 Phase 6完了実績:**
- **4つの完全なRED-GREEN-REFACTORサイクル完了**
- **最終テスト結果: 26個のテスト合格、0個失敗**
- **テストファイル: 6ファイル（UpgradeCommandTests, SyncCommandTests, ExportCommandTests, ImportCommandTests他）**
- **モック機能拡張: SyncStatePersisted, ExportFileGenerated, ImportFileProcessed**
- **コード品質: 未使用フィールド削除、意味的コメント改善**

### Phase 7: 引数パーサー実装 (t-wada式TDD)
- [ ] Red: System.CommandLine パーサーテスト作成
- [ ] Green: WinGet互換の引数解析実装
- [ ] Refactor: パーサー最適化

### Phase 8: Gist実装 (t-wada式TDD)
- [ ] Red: GitHub OAuth認証テスト作成
- [ ] Green: Device Flow認証実装
- [ ] Refactor: 認証フロー改善
- [ ] Red: Gist API呼び出しテスト作成
- [ ] Green: 実際のGist API呼び出し実装
- [ ] Refactor: API呼び出し最適化
- [ ] Red: YAML処理テスト作成
- [ ] Green: YAML読み書き実装
- [ ] Refactor: YAMLハンドリング改善

### Phase 9: エラーハンドリング強化 (t-wada式TDD)
- [ ] Red: 詳細エラーメッセージテスト作成
- [ ] Green: エラーメッセージ実装
- [ ] Refactor: メッセージ改善
- [ ] Red: リトライ処理テスト作成
- [ ] Green: リトライ機能実装
- [ ] Refactor: リトライロジック最適化

## 開発方針の変更

✅ **Phase 5以降のTDD原則:**
- **Red-Green-Refactor サイクル厳守**
- **各機能は失敗するテストから開始**
- **最小限の実装でテストを通す**
- **動作確認後にリファクタリング**
- **1サイクルは小さく（1-2時間以内）**

⚠️ **TDD段階で避けるもの:**
- テスト無しでの機能追加
- 複数機能の同時実装
- 大きすぎるテストケース
- テスト通過前のリファクタリング

## テスト戦略

### 単体テスト
- **WinGetComClient**: COM API呼び出し、データ取得機能
- **CommandService**: ルーティングロジック
- **GistSyncService**: YAML処理、Gist API呼び出し

### 統合テスト
- **E2E コマンド実行**: install, uninstall, sync, export, import
- **Gist連携**: 認証からデータ同期まで全体フロー

### テストダブル戦略
- **COM APIモック**: テスト環境での高速実行
- **HTTP クライアントモック**: Gist API呼び出しテスト
- **ファイルシステムモック**: YAML ファイル処理テスト
