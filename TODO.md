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

### Phase 7: 🔴 MVP検証: GitHub認証とGist連携（手動テスト中心） ⏳

**アプローチ: 実装→dotnet run→利用者検証の高速サイクル**
- ⚡ **xUnitテストは後回し**（動作確認後に追加）
- 🤝 **利用者への操作依頼を積極的に活用**
- 🔄 **dotnet runで即座に動作確認**

#### Step 1: GitHub Device Flow認証 ✅
- ✅ Octokit.NET追加、OAuth Device Flow実装完了
- ✅ トークン保存・読み込み機能完了
- ✅ 利用者認証確認済み（nuitsjp/Atsushi Nakamura）

#### Step 2: Gist読み取りテスト ✅
- ✅ TestGistCommand実装完了
- ✅ Gist API読み取り機能確認済み（63個のGist取得成功）
- ✅ ユーザー情報・Gist詳細表示機能動作確認済み

#### Step 3: export実装（20分以内）
```bash
# インストール済みパッケージをGistへ
dotnet run -- export
```
**利用者への依頼:**
1. GitHubのGistページを開いてください
2. "winget-packages.yaml"という新しいGistが作成されたか確認
3. 内容が正しいYAML形式か確認してください

**実装内容:**
- [ ] YamlDotNet追加 (`dotnet add package YamlDotNet`)
- [ ] COM APIでパッケージ一覧取得
- [ ] YAML形式に変換
- [ ] Gistとして作成/更新

#### Step 4: import実装（20分以内）
```bash
# 別のPCを想定したテスト
dotnet run -- import --dry-run
```
**利用者への依頼:**
1. インポートされるパッケージ一覧を確認してください
2. 実際にインストールして良いパッケージか判断してください
3. OKなら`--dry-run`なしで実行してください

**実装内容:**
- [ ] Gistから"winget-packages.yaml"取得
- [ ] YAMLパース
- [ ] `--dry-run`でリスト表示のみ
- [ ] 実行時はwinget installを呼び出し

#### Step 5: 統合動作確認（利用者主導）
**利用者への実施依頼:**
```bash
# 1. 認証状態確認
dotnet run -- auth status

# 2. 新しいアプリをインストール
dotnet run -- install --id AkelPad.AkelPad

# 3. Gistへ自動同期されるか確認
# （ブラウザでGist確認をお願いします）

# 4. 別のアプリもテスト
dotnet run -- install --id Microsoft.PowerToys

# 5. 最終的なexport
dotnet run -- export --verbose
```

**成功基準（利用者確認）:**
- [ ] GitHub認証が完了する
- [ ] Gistの作成/更新ができる
- [ ] YAMLフォーマットが正しい
- [ ] import時にパッケージがインストールされる

### Phase 7.5: テスト追加（MVP確認後）
**MVP動作確認後に実施:**
- [ ] 認証フローのモックテスト作成
- [ ] Gist API呼び出しのテスト
- [ ] YAML変換のユニットテスト
- [ ] 統合テストの追加

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
GitHubAuthService    | 0%   | 手動検証 | 80%      | MVP後
GistApiClient        | 0%   | 手動検証 | 85%      | MVP後
YamlSerializer       | 0%   | 手動検証 | 90%      | MVP後
ProcessRunner        | 60%  | 60%     | 80%      | 中
CommandRouter        | 40%  | 40%     | 60%      | 低
WinGetClient         | 30%  | 30%     | 60%      | 中
ArgumentStrategy     | 0%   | 50%     | 90%      | 中
```

### Device Flow自動化テスト戦略
1. **MVP段階（Phase 7）**
   - 手動検証のみ
   - 利用者フィードバック重視
   - 成功/失敗パスの文書化

2. **自動化段階（Phase 7.5以降）**
   - MVP確認後にテスト追加
   - CI/CDでのDevice Flowシミュレーション
   - 統合テストの完全自動化

## ✅ 開発方針

### MVP最優先事項
- **動くコードを最速で** ← 最重要
- **利用者フィードバックを即座に反映**
- **テストは動作確認後に追加**
- **dotnet runで即座に検証**

### TDD原則（Phase 8以降で厳守）
- **Red-Green-Refactor サイクル厳守**
- **各機能は失敗するテストから開始**
- **最小限の実装でテストを通す**
- **動作確認後にリファクタリング**
- **1サイクルは小さく（1-2時間以内）**

### 避けるべきもの
- MVP段階での過度なテスト作成
- 完璧を求めすぎる
- 利用者フィードバックの無視
- 複雑な実装

## 📝 リリースチェックリスト

### v0.8.0 MVP（Phase 7完了時点）
- [ ] GitHub Device Flow認証動作（利用者確認済み）
- [ ] Gist API基本操作確認（利用者確認済み）
- [ ] export/importコマンド動作（利用者確認済み）
- [ ] 手動E2Eテスト完了（利用者実施）
- [ ] 基本的なドキュメント

### v1.0.0 リリース基準
- [ ] Phase 7-9 完了
- [ ] テストカバレッジ 70%以上
- [ ] 完全な自動化テスト
- [ ] ドキュメント整備
- [ ] GitHub Releasesへの自動デプロイ
- [ ] PowerShell版との互換性確認