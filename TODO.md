# GistGet TODO List

## テスト用アプリケーション
**決定:** AkelPad.AkelPad (軽量テキストエディタ、約2-3MB)
- 理由: 軽量、高速インストール/アンインストール、システムへの影響が最小
- 使用例: `gistget install --id AkelPad.AkelPad`, `gistget uninstall --id AkelPad.AkelPad`

## 🎉 完了済みフェーズ

### Phase 0-6: MVP基盤完成 ✅
- **Phase 0-3**: MVPコア機能完了（パススルー、COM API、Gist同期スタブ）
- **Phase 4-4.5**: アーキテクチャ改善、COM API修正完了
- **Phase 5**: テスト基盤構築完了（xUnit + Shouldly、26テスト合格）
- **Phase 6**: t-wada式TDD完了（upgrade/sync/export/import の4サイクル完成）

**📊 現在の状態:**
- ✅ **実行ファイル**: `GistGet.exe`（AssemblyName変更済み）
- ✅ **COM API**: 完全動作（301パッケージ取得、検索機能確認済み）
- ✅ **テスト**: 26個のテスト全合格、0個失敗
- ✅ **アーキテクチャ**: DI、インターフェース分離、ログ機能完備

## 🚀 実装ロードマップ（順次実行）

### Phase 7: 基本動作の完成 ⏳
- [ ] **7.1 エラーハンドリング改善**
  - [ ] Red: エラー発生時の表示テスト
  - [ ] Green: ユーザーフレンドリーなエラーメッセージ実装
  - [ ] Refactor: エラー処理の統一化
- [ ] **7.2 プログレス表示**
  - [ ] Red: 長時間処理のプログレステスト
  - [ ] Green: インストール/アップグレード時の進捗表示
  - [ ] Refactor: プログレス処理の抽象化
- [ ] **7.3 非管理者モード対応**
  - [ ] Red: 管理者権限不要な操作のテスト
  - [ ] Green: 権限チェックと適切な処理分岐
  - [ ] Refactor: 権限管理の最適化

### Phase 8: 引数処理戦略の実装 (t-wada式TDD)
- [ ] **8.1 ルーティング判定のみの実装**
  - [ ] Red: 第1引数でのルーティング判定テスト作成
  - [ ] Green: 最小限のコマンド識別実装（第1引数のみチェック）
  - [ ] Refactor: ルーティングロジック最適化
- [ ] **8.2 System.CommandLine導入（Gist独自コマンドのみ）**
  - [ ] Red: sync/export/importコマンドの引数解析テスト
  - [ ] Green: Gist独自コマンドのみSystem.CommandLine実装
  - [ ] Refactor: パーサー構造改善
- [ ] **8.3 パススルー保証**
  - [ ] Red: WinGetコマンドの完全パススルーテスト
  - [ ] Green: 引数を一切加工せずに渡す実装
  - [ ] Refactor: パススルー処理の最適化

**重要な設計決定:**
- ✅ **WinGetコマンド**: 引数を**一切解釈せず**完全パススルー（search, list, show等）
- ✅ **Gist独自コマンド**: System.CommandLineで完全解析（sync, export, import）
- ✅ **ハイブリッド**: Gist同期有効時のinstall/uninstallは最小限の解析

### Phase 9: Gist同期機能 (t-wada式TDD)
- [ ] **9.1 GitHub OAuth認証**
  - [ ] Red: Device Flow認証テスト作成
  - [ ] Green: GitHub OAuth実装
  - [ ] Refactor: 認証フロー改善
- [ ] **9.2 Gist API呼び出し**
  - [ ] Red: Gist CRUD操作テスト作成
  - [ ] Green: 実際のGist API呼び出し実装
  - [ ] Refactor: API呼び出し最適化
- [ ] **9.3 YAML処理**
  - [ ] Red: YAML読み書きテスト作成
  - [ ] Green: パッケージ定義YAML化実装
  - [ ] Refactor: YAMLハンドリング改善
- [ ] **9.4 同期ロジック実装**
  - [ ] Red: パッケージリスト同期テスト
  - [ ] Green: Gistからのパッケージリスト取得と比較
  - [ ] Refactor: 同期処理の最適化
- [ ] **9.5 自動同期**
  - [ ] Red: install/uninstall時の自動同期テスト
  - [ ] Green: 操作後の自動Gist更新実装
  - [ ] Refactor: 同期タイミングの最適化
- [ ] **9.6 syncコマンド実装**
  - [ ] Red: syncコマンドの統合テスト
  - [ ] Green: Gistからローカルへの同期実装
  - [ ] Refactor: エラー処理とリトライ機能

### Phase 10: 最適化 (t-wada式TDD)
- [ ] **10.1 キャッシング機能**
  - [ ] Red: キャッシュ有効性テスト
  - [ ] Green: パッケージ情報のキャッシュ実装
  - [ ] Refactor: キャッシュ戦略の最適化
- [ ] **10.2 並列処理**
  - [ ] Red: 並列インストールテスト
  - [ ] Green: 複数パッケージの並列処理実装
  - [ ] Refactor: 並列度の調整
- [ ] **10.3 ログ機能強化**
  - [ ] Red: 詳細ログ出力テスト
  - [ ] Green: 構造化ログの実装
  - [ ] Refactor: ログレベルとフォーマット改善

## 📋 テスト戦略

### テストカバレッジ目標
```
レイヤー              | 現在 | 目標 | 優先度
---------------------|------|------|--------
ProcessRunner        | 60%  | 80%  | 高
CommandRouter        | 40%  | 60%  | 中
WinGetClient         | 30%  | 60%  | 高
ArgumentStrategy     | 0%   | 90%  | 最高
GistCommandParser    | 0%   | 85%  | 高
GistSyncService      | 0%   | 80%  | 高
```

### 単体テスト
- **ArgumentStrategy**: ルーティング判定ロジック（引数解析なし）
- **GistCommandParser**: Gist独自コマンドのSystem.CommandLine解析
- **PassthroughValidator**: 引数が加工されていないことの検証
- **WinGetComClient**: COM API呼び出し、データ取得機能
- **CommandService**: ルーティングロジック
- **GistSyncService**: YAML処理、Gist API呼び出し

### 統合テスト
- **E2E コマンド実行**: install, uninstall, sync, export, import
- **Gist連携**: 認証からデータ同期まで全体フロー
- **パススルー検証**: WinGetへの完全互換性確認

### テストダブル戦略
- **COM APIモック**: テスト環境での高速実行
- **HTTP クライアントモック**: Gist API呼び出しテスト
- **ファイルシステムモック**: YAML ファイル処理テスト
- **プロセスモック**: winget.exe呼び出しのシミュレーション

## ✅ 開発方針

### TDD原則（厳守）
- **Red-Green-Refactor サイクル厳守**
- **各機能は失敗するテストから開始**
- **最小限の実装でテストを通す**
- **動作確認後にリファクタリング**
- **1サイクルは小さく（1-2時間以内）**

### 避けるべきもの
- テスト無しでの機能追加
- 複数機能の同時実装
- 大きすぎるテストケース
- テスト通過前のリファクタリング
- 引数の不要な解析（パススルー時）

## 📝 リリースチェックリスト

### v1.0.0 リリース基準
- [ ] Phase 7-9 完了
- [ ] テストカバレッジ 70%以上
- [ ] ドキュメント整備
- [ ] GitHub Releasesへの自動デプロイ
- [ ] PowerShell版との互換性確認