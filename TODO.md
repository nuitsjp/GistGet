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

**📊 現在の状態:**
- ✅ **実行ファイル**: `GistGet.exe`（AssemblyName変更済み）
- ✅ **COM API**: 完全動作（301パッケージ取得、検索機能確認済み）
- ✅ **テスト**: 26個のテスト全合格、0個失敗
- ✅ **アーキテクチャ**: DI、インターフェース分離、ログ機能完備

## 🚀 実装ロードマップ（順次実行）

### Phase 7: 🔴 MVP必須: GitHub認証とGist API (最優先) ⏳

**実装戦略:**
- **Device Flow採用理由**: ブラウザー自動化不要、CI/CD対応可能、ヘッドレス環境対応
- **MVP段階**: 手動検証とコラボレーションによる実装
- **自動化段階**: Device Flowのポーリング処理により完全自動化可能

#### 7.1 GitHub Device Flow認証（手動検証OK）
- [ ] **Red**: Device Flow認証フローのテスト作成
  - [ ] デバイスコード取得のモックテスト
  - [ ] ユーザーコード表示のテスト
  - [ ] ポーリング処理のテスト
- [ ] **Green**: 最小限の認証実装
  - [ ] GitHub Device Flow API呼び出し
  - [ ] コンソールへのユーザーコード表示
  - [ ] 手動でのブラウザー認証（MVP段階）
- [ ] **Refactor**: 認証フロー改善
  - [ ] トークン保存・再利用
  - [ ] エラーハンドリング

#### 7.2 Gist API基本操作（CRUD）
- [ ] **Red**: Gist操作テスト作成
  - [ ] Gist作成テスト
  - [ ] Gist読み取りテスト
  - [ ] Gist更新テスト
- [ ] **Green**: Octokit.NET統合
  - [ ] NuGetパッケージ追加
  - [ ] 基本的なGist CRUD実装
  - [ ] 手動検証による動作確認
- [ ] **Refactor**: API呼び出しの抽象化

#### 7.3 YAML処理とGist同期
- [ ] **Red**: YAML⇔Gist変換テスト
  - [ ] パッケージリストのYAML化テスト
  - [ ] YAMLからのパッケージ復元テスト
- [ ] **Green**: YamlDotNet統合
  - [ ] パッケージリストのシリアライズ
  - [ ] Gistへの保存実装
- [ ] **Refactor**: データモデル最適化

#### 7.4 MVP統合テスト（手動検証含む）
- [ ] **統合シナリオ**:
  1. `gistget auth` - Device Flow認証（手動でブラウザー操作）
  2. `gistget export` - 現在のパッケージリストをGistへ
  3. `gistget import` - Gistからパッケージリスト取得
  4. `gistget sync` - 双方向同期

**自動化への移行計画:**
```csharp
// MVP段階（手動検証）
Console.WriteLine($"ブラウザーで以下を開いてください: {verificationUri}");
Console.WriteLine($"コードを入力: {userCode}");
// 手動で認証完了を待つ

// 自動化段階（Device Flowのポーリング）
while (!authenticated) {
    var response = await PollForToken(deviceCode);
    if (response.IsAuthenticated) break;
    await Task.Delay(interval);
}
```

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

### Phase 9: 基本動作の改善
- [ ] **9.1 エラーハンドリング改善**
  - [ ] Red: エラー発生時の表示テスト
  - [ ] Green: ユーザーフレンドリーなエラーメッセージ実装
  - [ ] Refactor: エラー処理の統一化
- [ ] **9.2 プログレス表示**
  - [ ] Red: 長時間処理のプログレステスト
  - [ ] Green: インストール/アップグレード時の進捗表示
  - [ ] Refactor: プログレス処理の抽象化
- [ ] **9.3 非管理者モード対応**
  - [ ] Red: 管理者権限不要な操作のテスト
  - [ ] Green: 権限チェックと適切な処理分岐
  - [ ] Refactor: 権限管理の最適化

### Phase 10: Gist同期の高度化
- [ ] **10.1 自動同期**
  - [ ] Red: install/uninstall時の自動同期テスト
  - [ ] Green: 操作後の自動Gist更新実装
  - [ ] Refactor: 同期タイミングの最適化
- [ ] **10.2 競合解決**
  - [ ] Red: 同期競合のテストケース
  - [ ] Green: マージ戦略の実装
  - [ ] Refactor: 競合解決UI
- [ ] **10.3 複数デバイス対応**
  - [ ] Red: デバイス別設定のテスト
  - [ ] Green: デバイスIDによる管理
  - [ ] Refactor: 設定の統合

### Phase 11: 最適化
- [ ] **11.1 キャッシング機能**
  - [ ] Red: キャッシュ有効性テスト
  - [ ] Green: パッケージ情報のキャッシュ実装
  - [ ] Refactor: キャッシュ戦略の最適化
- [ ] **11.2 並列処理**
  - [ ] Red: 並列インストールテスト
  - [ ] Green: 複数パッケージの並列処理実装
  - [ ] Refactor: 並列度の調整

## 📋 テスト戦略

### MVP優先テストカバレッジ
```
コンポーネント         | 現在 | MVP目標 | 最終目標 | 優先度
---------------------|------|---------|----------|--------
GitHubAuthService    | 0%   | 60%     | 80%      | 最高
GistApiClient        | 0%   | 70%     | 85%      | 最高
YamlSerializer       | 0%   | 80%     | 90%      | 高
ProcessRunner        | 60%  | 60%     | 80%      | 中
CommandRouter        | 40%  | 40%     | 60%      | 低
WinGetClient         | 30%  | 30%     | 60%      | 中
ArgumentStrategy     | 0%   | 50%     | 90%      | 中
```

### Device Flow自動化テスト戦略
1. **MVP段階（Phase 7）**
   - モックによる認証フロー検証
   - 手動でのE2Eテスト（開発者コラボレーション）
   - 成功/失敗パスの文書化

2. **自動化段階（Phase 10以降）**
   - GitHub APIのテストモード活用
   - CI/CDでのDevice Flowシミュレーション
   - 統合テストの完全自動化

## ✅ 開発方針

### MVP最優先事項
- **GitHub認証なしではMVPとは言えない** ← 最重要認識
- **手動検証でも良いので動作する実装を優先**
- **完璧な自動化より、まず動くものを**

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
- MVP段階での過度な自動化追求

## 📝 リリースチェックリスト

### v0.8.0 MVP（Phase 7完了時点）
- [ ] GitHub Device Flow認証動作
- [ ] Gist API基本操作確認
- [ ] export/importコマンド動作
- [ ] 手動E2Eテスト完了
- [ ] 基本的なドキュメント

### v1.0.0 リリース基準
- [ ] Phase 7-9 完了
- [ ] テストカバレッジ 70%以上
- [ ] 完全な自動化テスト
- [ ] ドキュメント整備
- [ ] GitHub Releasesへの自動デプロイ
- [ ] PowerShell版との互換性確認