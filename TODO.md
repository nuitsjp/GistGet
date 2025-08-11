# GistGet TODO List

## テスト用アプリケーション
**決定:** AkelPad.AkelPad (軽量テキストエディタ、約2-3MB)
- 理由: 軽量、高速インストール/アンインストール、システムへの影響が最小
- 使用例: `gistget install --id AkelPad.AkelPad`, `gistget uninstall --id AkelPad.AkelPad`

## 🎉 完了済みフェーズ

### Phase 0-6: 基盤構築完了 ✅
- **Phase 0-3**: コア機能完了（パススルー、COM API、Gist同期スタブ）
- **Phase 4-4.5**: アーキテクチャ改善、COM API修正完了
- **Phase 5**: テスト基盤構築完了（xUnit + Shouldly、26テスト合格）
- **Phase 6**: t-wada式TDD完了（upgrade/sync/export/import の4サイクル完成）

### Phase 7: MVP検証完了 ✅
- GitHub Device Flow認証完全動作（nuitsjp/Atsushi Nakamura確認済み）
- export/importをREADME.md仕様に合わせてWinGet標準パススルーに修正
- COM API（install/uninstall/upgrade）正常動作
- 全機能統合動作確認済み

**📊 現在の状態:**
- ✅ **実行ファイル**: `GistGet.exe`（AssemblyName変更済み）
- ✅ **COM API**: 完全動作（301パッケージ取得、検索機能確認済み）
- ✅ **テスト**: 26個のテスト全合格、0個失敗
- ✅ **アーキテクチャ**: DI、インターフェース分離、ログ機能完備
- ✅ **GitHub認証**: Device Flow認証完全動作（nuitsjp/Atsushi Nakamura）
- ✅ **パススルー**: export/import等がWinGet標準動作で完全動作
- ✅ **README.md準拠**: 全コマンドがREADME.md仕様通りに動作

## 🚀 実装ロードマップ（順次実行）

### Phase 7.5: テスト追加（MVP確認後・事前認証前提）
**MVP動作確認後に実施:**
- [ ] 事前認証チェック機能のテスト作成
- [ ] 認証済み前提のGist API呼び出しテスト
- [ ] YAML変換のユニットテスト
- [ ] 統合テストの追加（`gistget auth`実行済み前提）

### Phase 8: 引数処理戦略の実装 (t-wada式TDD)
- [ ] **8.1 ルーティング判定**: 第1引数でのルーティング判定テスト・実装・最適化
- [ ] **8.2 System.CommandLine導入**: syncコマンドのみ引数解析テスト・実装・改善
- [ ] **8.3 パススルー保証**: WinGetコマンドの完全パススルーテスト・実装・最適化

### Phase 9: 基本動作の改善
- [ ] **9.1 エラーハンドリング改善**: エラー発生時の表示テスト・実装・統一化
- [ ] **9.2 プログレス表示**: 長時間処理のプログレステスト・実装・抽象化
- [ ] **9.3 非管理者モード対応**: 管理者権限不要な操作のテスト・実装・最適化

### Phase 10: Gist同期の高度化
- [ ] **10.1 自動同期**: install/uninstall時の自動同期テスト・実装・最適化
- [ ] **10.2 競合解決**: 同期競合のテストケース・マージ戦略実装・UI改善
- [ ] **10.3 複数デバイス対応**: デバイス別設定のテスト・デバイスID管理・設定統合

### Phase 11: 最適化
- [ ] **11.1 キャッシング機能**: キャッシュ有効性テスト・パッケージ情報キャッシュ実装・戦略最適化
- [ ] **11.2 並列処理**: 並列インストールテスト・複数パッケージ並列処理実装・並列度調整

## 📋 テスト戦略

### 認証環境
```bash
dotnet run -- auth status  # 認証状態確認
dotnet run -- auth         # 認証実行
```

### テストカテゴリ
- **Unit**: 認証不要、CI実行可能 (`dotnet test --filter "Category=Unit"`)
- **Local**: 事前認証済み、手動実行 (`dotnet test --filter "Category=Local"`)
- **Manual**: 事前認証済み、手動検証必須

## ✅ 開発方針

### 優先事項
- 事前認証の確実な実行
- 動くコードを最速で実装
- 利用者フィードバックを即座に反映
- dotnet runで即座に検証

### TDD原則（Phase 8以降）
- Red-Green-Refactor サイクル厳守
- 失敗するテストから開始
- 最小限の実装でテストを通す
- 1サイクルは小さく（1-2時間以内）

## 📝 リリースチェックリスト

### v0.8.0 MVP（Phase 7完了時点） ✅
- ✅ 事前認証環境の整備完了（`gistget auth`実行済み）
- ✅ GitHub Device Flow認証動作（利用者確認済み）
- ❌ Gist API基本操作確認（仕様変更によりパススルー実装に変更）
- ✅ export/importコマンド動作（WinGet標準パススルーとして確認済み）
- ✅ 手動E2Eテスト完了（利用者実施、README.md準拠）
- ✅ 基本的なドキュメント（認証手順含む、README.md更新済み）

### v1.0.0 リリース基準
- [ ] Phase 7-9 完了
- [ ] 事前認証済み前提のテストカバレッジ 70%以上
- [ ] 完全な自動化テスト（Unit + Local）
- [ ] 認証環境整備ドキュメント
- [ ] GitHub Releasesへの自動デプロイ
- [ ] PowerShell版との互換性確認

## 🔧 トラブルシューティング

| 問題 | 解決策 |
|------|--------|
| 認証エラー | `dotnet run -- auth` で再認証 |
| テストスキップ | `dotnet run -- auth status` で確認後認証 |
| Gist API失敗 | GitHub Personal Access Token のスコープ確認 |