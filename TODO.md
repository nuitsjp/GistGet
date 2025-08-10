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

### Phase 4: 引数パーサー実装
- System.CommandLineの導入
- WinGet互換の引数解析

### Phase 5: Gist実装
- GitHub OAuth認証
- 実際のGist API呼び出し
- YAML処理

### Phase 6: エラーハンドリング強化
- 詳細なエラーメッセージ
- ログ機能
- リトライ処理

## 注意事項

⚠️ **MVP段階では以下を避ける:**
- 過度な抽象化（インターフェース、基底クラス）
- 複雑な設計パターン
- 包括的なエラーハンドリング
- ユニットテスト（動作確認を優先）
- ログフレームワーク

✅ **MVP段階で重視する:**
- 動作すること
- シンプルであること
- 読みやすいこと
- 段階的に改善可能であること
