# GistGet TODO List

## 📋 運用ルール

- **完了済みフェーズ**: 何をしたかの記録（変更不要）
- **残りタスク**: フェーズレベルの大まかな計画（全体把握用）
- **次フェーズ**: 具体的な手順チェックシート（実作業用）
- **1フェーズ完了**: 次フェーズを詳細化し、完了フェーズに移動

## ✅ 完了済みフェーズ

### Phase 0-7: MVP基盤完成
- **基本実装**: パススルー、COM API基盤、コマンドルーティング
- **テスト基盤**: xUnit + Shouldly、26テスト合格
- **GitHub認証**: Device Flow認証完全動作確認
- **アーキテクチャ**: DI、インターフェース分離、ログ機能
- **実行ファイル**: GistGet.exe動作確認
- **ドキュメント**: README.md、architecture.md整備

## 📅 残りタスク（フェーズレベル）

### Phase 7.5: Gist管理基盤実装
- Gist設定の安全な保存・取得機能
- YAML ↔ C#オブジェクト変換基盤
- Gist CRUD操作のプライベート関数
- 事前設定前提のテスト環境整備

### Phase 8: 引数処理戦略実装
- コマンドルーティング判定改善
- System.CommandLine導入
- パススルー保証確保

### Phase 9: 基本動作改善
- エラーハンドリング統一
- プログレス表示実装
- 非管理者モード対応

### Phase 10: Gist同期高度化
- install/uninstall時の自動同期
- 同期競合解決
- 複数デバイス対応

### Phase 11: 最適化・リリース準備
- キャッシング機能
- 並列処理
- リリース品質向上

## 🎯 次フェーズ詳細: Phase 7.5 Gist管理基盤実装

### 7.5.1 Gist設定管理機能

テストに利用するGistの情報を実装時にユーザーに問い合わせて対応を開始してください。じゃないと実装・テストが困難なためです。

- [x] **GistConfiguration クラス作成**
  - [x] `GistConfiguration.cs` 作成（ID、ファイル名、作成日時、最終アクセス日時）
  - [x] JSON シリアライゼーション対応
  - [x] バリデーション機能追加

- [x] **GistConfigurationStorage クラス作成**
  - [x] `SaveGistConfigurationAsync()` 実装（DPAPI暗号化）
  - [x] `LoadGistConfigurationAsync()` 実装（DPAPI復号化）
  - [x] `%APPDATA%\GistGet\gist.dat` パス管理
  - [x] エラーハンドリング（ファイル破損、権限エラー等）

- [x] **ユーザー入力処理**
  - [x] GitHubでGist手動作成の案内表示
  - [x] Gist IDの入力受付（必須）
  - [x] ファイル名の入力受付（デフォルト: packages.yaml）
  - [x] 入力値のバリデーション（Gist ID形式チェック等）

- [x] **単体テスト作成**
  - [x] 保存・読み込みテスト
  - [x] 暗号化・復号化テスト
  - [x] エラーケーステスト
  - [x] ユーザー入力バリデーションテスト

### 7.5.2 YAML操作基盤
- [x] **PackageDefinition クラス作成**
  - [x] `Id`, `Version`, `Uninstall` プロパティ
  - [x] バリデーション機能

- [x] **PackageCollection クラス作成**
  - [x] `List<PackageDefinition>` 管理
  - [x] 追加・削除・検索メソッド

- [x] **PackageYamlConverter クラス作成**
  - [x] `ToYaml()` メソッド実装（YamlDotNet使用）
  - [x] `FromYaml()` メソッド実装
  - [x] エラーハンドリング（不正YAML等）

- [x] **単体テスト作成**
  - [x] YAML変換往復テスト
  - [x] 不正データエラーテスト

### 7.5.3 Gist CRUD操作
- [x] **GitHubGistClient クラス作成**
  - [x] 既存 GitHubAuthService 活用
  - [x] `GetFileContentAsync()` 実装
  - [x] `UpdateFileContentAsync()` 実装
  - [x] `ExistsAsync()` 実装（Gist存在確認）

- [x] **GistManager クラス作成**
  - [x] `GetGistPackagesAsync()` プライベート関数
  - [x] `UpdateGistPackagesAsync()` プライベート関数
  - [x] `IsConfiguredAsync()` 設定状態確認
  - [x] 統合エラーハンドリング

- [x] **統合テスト作成**
  - [x] 事前認証・Gist設定チェック機能（Localカテゴリ/条件付き実行）
  - [x] 実際のGist API呼び出しテスト（`[Trait("Category", "Local")]` や Skip 指定で隔離）

### 7.5.4 Gist管理コマンド実装
- [ ] **GistSetCommand クラス作成**（未実装）
  - [ ] `gistget gist set --gist-id abc123 --file packages.yaml`
  - [ ] `gistget gist set` （対話形式での入力受付）
  - [ ] GitHubでのGist作成手順案内
  - [ ] 認証チェック
  - [ ] Gist存在確認
  - [ ] 設定保存

- [ ] **GistStatusCommand クラス作成**（未実装）
  - [ ] `gistget gist status`
  - [ ] 設定状態表示
  - [ ] 最終アクセス日時表示

- [ ] **GistShowCommand クラス作成**（未実装）
  - [ ] `gistget gist show`
  - [ ] Gist内容表示
  - [ ] YAML整形表示

- [ ] **CommandRouter 更新**（未実装）
  - [ ] `gist` サブコマンドのルーティング追加
  - [ ] System.CommandLine パーサー統合

### 7.5.5 完了確認
- [ ] **手動テスト実行**（コマンド系未実装のため保留）
  - [ ] `dotnet run -- auth` 認証確認
  - [ ] GitHubでテスト用Gist作成
  - [ ] `dotnet run -- gist set --gist-id [実際のID] --file packages.yaml`
  - [ ] `dotnet run -- gist set` （対話形式テスト）
  - [ ] `dotnet run -- gist status` 設定確認
  - [ ] `dotnet run -- gist show` 内容表示確認

- [ ] **テスト実行**
  - [x] `dotnet test --filter "Category=Unit"` 相当のユニットは追加済み（実行は随時）
  - [ ] `dotnet test --filter "Category=Local"` （認証/Gist前提・環境依存のため手動実施）

- [ ] **ドキュメント更新**（コマンド実装後に反映）
  - [ ] README.md に Gist設定手順追加
  - [ ] architecture.md に実装詳細追加

## テスト用アプリケーション
**決定:** AkelPad.AkelPad (軽量テキストエディタ、約2-3MB)
- 理由: 軽量、高速インストール/アンインストール、システムへの影響が最小
- 使用例: `gistget install --id AkelPad.AkelPad`, `gistget uninstall --id AkelPad.AkelPad`

## 🔧 トラブルシューティング

| 問題 | 解決策 |
|------|--------|
| 認証エラー | `dotnet run -- auth` で再認証 |
| Gist設定エラー | `dotnet run -- gist status` で確認後再設定 |
| Gist ID不明 | GitHubでGist作成後、URLからID取得 |
| Gist作成方法不明 | `dotnet run -- gist set` で作成手順案内表示 |
| テストスキップ | 事前認証・Gist設定の両方を実行 |
| YAML解析エラー | Gist内容をブラウザで確認、手動修正 |