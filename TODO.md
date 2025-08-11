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

- [ ] **GistConfiguration クラス作成**
  - [ ] `GistConfiguration.cs` 作成（ID、ファイル名、作成日時、最終アクセス日時）
  - [ ] JSON シリアライゼーション対応
  - [ ] バリデーション機能追加

- [ ] **GistConfigurationStorage クラス作成**
  - [ ] `SaveGistConfigurationAsync()` 実装（DPAPI暗号化）
  - [ ] `LoadGistConfigurationAsync()` 実装（DPAPI復号化）
  - [ ] `%APPDATA%\GistGet\gist.dat` パス管理
  - [ ] エラーハンドリング（ファイル破損、権限エラー等）

- [ ] **ユーザー入力処理**
  - [ ] GitHubでGist手動作成の案内表示
  - [ ] Gist IDの入力受付（必須）
  - [ ] ファイル名の入力受付（デフォルト: packages.yaml）
  - [ ] 入力値のバリデーション（Gist ID形式チェック等）

- [ ] **単体テスト作成**
  - [ ] 保存・読み込みテスト
  - [ ] 暗号化・復号化テスト
  - [ ] エラーケーステスト
  - [ ] ユーザー入力バリデーションテスト

### 7.5.2 YAML操作基盤
- [ ] **PackageDefinition クラス作成**
  - [ ] `Id`, `Version`, `Uninstall` プロパティ
  - [ ] バリデーション機能

- [ ] **PackageCollection クラス作成**
  - [ ] `List<PackageDefinition>` 管理
  - [ ] 追加・削除・検索メソッド

- [ ] **PackageYamlConverter クラス作成**
  - [ ] `ToYaml()` メソッド実装（YamlDotNet使用）
  - [ ] `FromYaml()` メソッド実装
  - [ ] エラーハンドリング（不正YAML等）

- [ ] **単体テスト作成**
  - [ ] YAML変換往復テスト
  - [ ] 不正データエラーテスト

### 7.5.3 Gist CRUD操作
- [ ] **GitHubGistClient クラス作成**
  - [ ] 既存 GitHubAuthService 活用
  - [ ] `GetFileContentAsync()` 実装
  - [ ] `UpdateFileContentAsync()` 実装
  - [ ] `ExistsAsync()` 実装（Gist存在確認）

- [ ] **GistManager クラス作成**
  - [ ] `GetGistPackagesAsync()` プライベート関数
  - [ ] `UpdateGistPackagesAsync()` プライベート関数
  - [ ] `IsConfiguredAsync()` 設定状態確認
  - [ ] 統合エラーハンドリング

- [ ] **統合テスト作成**
  - [ ] 事前認証・Gist設定チェック機能
  - [ ] 実際のGist API呼び出しテスト（`[Trait("Category", "Local")]`）

### 7.5.4 Gist管理コマンド実装
- [ ] **GistSetCommand クラス作成**
  - [ ] `gistget gist set --gist-id abc123 --file packages.yaml`
  - [ ] `gistget gist set` （対話形式での入力受付）
  - [ ] GitHubでのGist作成手順案内
  - [ ] 認証チェック
  - [ ] Gist存在確認
  - [ ] 設定保存

- [ ] **GistStatusCommand クラス作成**
  - [ ] `gistget gist status`
  - [ ] 設定状態表示
  - [ ] 最終アクセス日時表示

- [ ] **GistShowCommand クラス作成**
  - [ ] `gistget gist show`
  - [ ] Gist内容表示
  - [ ] YAML整形表示

- [ ] **CommandRouter 更新**
  - [ ] `gist` サブコマンドのルーティング追加
  - [ ] System.CommandLine パーサー統合

### 7.5.5 完了確認
- [ ] **手動テスト実行**
  - [ ] `dotnet run -- auth` 認証確認
  - [ ] GitHubでテスト用Gist作成
  - [ ] `dotnet run -- gist set --gist-id [実際のID] --file packages.yaml`
  - [ ] `dotnet run -- gist set` （対話形式テスト）
  - [ ] `dotnet run -- gist status` 設定確認
  - [ ] `dotnet run -- gist show` 内容表示確認

- [ ] **テスト実行**
  - [ ] `dotnet test --filter "Category=Unit"` （新規ユニットテスト）
  - [ ] `dotnet test --filter "Category=Local"` （事前設定前提）

- [ ] **ドキュメント更新**
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