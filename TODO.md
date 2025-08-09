# GistGet .NET 8 実装ロードマップ

## 概要

winget.exe完全準拠の.NET 8アプリケーション開発に向けた詳細なロードマップです。

---

## フェーズ1: WinGetコマンド完全仕様書作成 【最優先】

**期間**: 1-2週間  
**目標**: winget.exeの全コマンド・オプション完全準拠ドキュメント

```
成果物:
├── docs/
│   ├── winget-commands-spec.md     # 全コマンド仕様書
│   ├── winget-arguments-matrix.md  # 引数相互関係マトリックス  
│   ├── winget-validation-rules.md  # バリデーションルール定義
│   └── winget-examples.md          # 使用例集

検証項目:
- 全18コマンド + サブコマンドの網羅
- 引数の相互排他性・依存関係の完全把握
- エラーパターンと例外処理の明文化
- 実際のwinget.exeでの動作確認
```

### 詳細タスク
- [x] winget --help の全コマンド調査
- [x] 各コマンドの --help 詳細調査 (install, list, upgrade, source, settings等)
- [x] サブコマンド構造の完全マッピング (source add/list/update/remove等)
- [x] 引数の相互排他性調査 (--id vs --name vs --query)
- [x] 条件付きオプション調査 (--include-unknown requires --upgrade-available)
- [x] エラーパターン調査とメッセージカタログ作成
- [x] 実際のwinget.exeでのテストケース実行・検証

### 成果物（完了）
- [x] `docs/winget-commands-spec.md` - 全18コマンド詳細仕様書作成完了
- [x] `docs/winget-arguments-matrix.md` - 引数相互関係マトリックス作成完了
- [x] `docs/winget-validation-rules.md` - バリデーションルール定義作成完了
- [x] `docs/winget-examples.md` - 使用例集作成完了

---

## フェーズ2: カスタム引数パーサー実装

**期間**: 2-3週間  
**目標**: WinGet完全準拠の引数解析エンジン

```
実装範囲:
├── src/ArgumentParser/
│   ├── WinGetArgumentParser.cs      # メイン引数パーサー
│   ├── CommandValidators/           # コマンド別バリデーター
│   ├── OptionModels/               # オプション定義クラス群
│   └── ValidationEngine.cs        # 複雑な依存関係チェック

技術仕様:
- ConsoleAppFramework基盤 + カスタム拡張
- System.CommandLine移行も検討
- 引数解析の100%自動テストカバレッジ
- winget.exeとのパラメータ互換性保証
```

### 詳細タスク
- [x] プロジェクト構成とソリューション作成
- [x] ConsoleAppFramework vs System.CommandLineの技術調査
- [x] 基本コマンドルーティング実装 (install, list, upgrade)
- [x] オプションモデルクラス設計・実装
- [x] 相互排他性チェック機構実装
- [x] 条件付きバリデーション機構実装
- [x] エイリアス処理 (ls→list, add→install, update→upgrade)
- [x] サブコマンド階層処理 (source add, settings export)
- [ ] 包括的単体テストスイート作成
- [ ] winget.exeとの動作比較テスト

### 成果物（完了）
- [x] `src/NuitsJp.GistGet.sln` - Visual Studioソリューション作成完了
- [x] `src/NuitsJp.GistGet/` - メインプロジェクト構成完了
- [x] `src/NuitsJp.GistGet.Tests/` - テストプロジェクト構成完了
- [x] `docs/argument-parser-tech-comparison.md` - System.CommandLine採用決定完了
- [x] System.CommandLineによる基本18コマンド構造実装完了
- [x] コマンドエイリアス (add→install, ls→list, update→upgrade) 実装完了
- [x] グローバルオプション実装完了
- [x] **オプションモデルクラス完全実装** - 全主要コマンド対応
- [x] **ValidationEngine実装** - 包括的引数バリデーションシステム
- [x] **ValidationRules実装** - WinGet完全準拠バリデーションルール
- [x] **相互排他性・条件付きバリデーション実装** - 完全なWinGet互換性
- [x] **サブコマンド階層構造実装** - `source add/list/update/remove/reset/export`, `settings export/set/reset`

---

## フェーズ3: COM APIラッパー実装

**期間**: 3-4週間
**目標**: Microsoft.WindowsPackageManager.ComInterop完全活用

```
実装構成:
├── src/WinGetClient/
│   ├── IWinGetClient.cs           # 公開インターフェース
│   ├── WinGetComClient.cs         # COM API実装
│   ├── WinGetCliClient.cs         # CLIフォールバック  
│   ├── Models/                    # 結果・オプションモデル
│   └── Extensions/                # COM API拡張メソッド

重要機能:
- COM API → CLI自動フォールバック  
- 非同期処理とキャンセレーション対応
- 詳細進捗レポート (IProgress<T>)
- 包括的エラーハンドリング
```

### 詳細タスク
- [ ] Microsoft.WindowsPackageManager.ComInterop NuGetパッケージ統合
- [ ] IWinGetClient インターフェース設計・実装
- [ ] COM API基本操作実装 (PackageManagerFactory初期化)
- [ ] パッケージ検索・インストール・アップグレード実装
- [ ] ソース管理機能実装
- [ ] 設定管理機能実装
- [ ] エクスポート・インポート機能実装
- [ ] CLI フォールバック機構実装
- [ ] エラーハンドリングとログ機構
- [ ] 非同期処理とキャンセレーション対応
- [ ] 進捗レポート機構 (IProgress<T>)
- [ ] COM API統合テスト
- [ ] パフォーマンステストとメモリリーク検証

---

## フェーズ4: Gist同期機能統合  

**期間**: 2-3週間
**目標**: [PowerShell版](./powershell/)機能との完全互換

```
統合範囲:
- OAuth Device Flow認証
- Gistファイル読み書き (CRUD)
- パッケージリスト同期 (export/import準拠)
- 環境変数管理 (GIST_GET_*)
- トークン暗号化保存 (DPAPI)

互換性保証:
- PowerShell版で作成したGistとの相互運用
- 同一環境変数・設定ファイル共有
- YAML形式完全互換
```

### 詳細タスク
- [ ] GitHub OAuth Device Flow実装
- [ ] GitHub Gist API クライアント実装
- [ ] トークン暗号化保存 (Windows DPAPI)
- [ ] 環境変数管理 (GIST_GET_GIST_ID, GIST_GET_GIST_FILE_NAME)
- [ ] YAML シリアライゼーション (PowerShell版互換)
- [ ] パッケージリスト同期機能 (export → Gist → import)
- [ ] PowerShell版との相互運用テスト
- [ ] エラーハンドリングと再試行機構
- [ ] オフライン動作とキャッシュ機構

---

## フェーズ5: テストと品質保証

**期間**: 2週間
**目標**: プロダクション対応品質の達成

```
テスト戦略:
├── tests/
│   ├── Unit/                    # 単体テスト (90%+ カバレッジ)
│   ├── Integration/             # COM API統合テスト  
│   ├── EndToEnd/               # 実際のwinget.exe比較テスト
│   └── Performance/            # パフォーマンステスト

品質指標:
- 全WinGetコマンドの動作確認
- PowerShell版との互換性テスト
- メモリリーク・例外安全性検証  
- Windows 10/11での動作確認
```

### 詳細タスク
- [ ] 包括的単体テストスイート整備
- [ ] COM API統合テスト
- [ ] winget.exeとの動作比較テスト (E2E)
- [ ] PowerShell版GistGet互換性テスト
- [ ] パフォーマンステストとベンチマーク
- [ ] メモリリーク検証
- [ ] 例外安全性テスト
- [ ] Windows 10/11環境でのテスト
- [ ] CI/CDパイプライン構築
- [ ] ドキュメント整備とサンプル作成

---

## 開発優先順位と技術的判断

### 最優先事項 (P0)
1. **ドキュメント作成**: WinGetコマンド仕様書が全ての基盤
2. **引数パーサー**: WinGet準拠が品質の核心
3. **COM API安定性**: フォールバック機構で可用性確保

### 段階的実装方針
- **MVP (Minimum Viable Product)**: 基本コマンド5つ (install, list, upgrade, export, import)
- **フル機能**: 全18コマンド完全対応
- **拡張機能**: PowerShell版超越機能 (性能改善、UI/UX向上)

### 技術的制約と対策
| 制約 | 影響 | 対策 |
|------|------|------|
| COM API不安定性 | 実行時エラー | CLI自動フォールバック |
| Windows依存性 | クロスプラットフォーム制限 | 仕様明記、将来Mono対応検討 |
| 管理者権限要求 | UX劣化 | 権限昇格フローの最適化 |
| .NET 8要求 | 配布複雑化 | 自己完結型展開 |

---

## 進捗管理

### 完了チェックリスト
- [x] フェーズ1: WinGetコマンド完全仕様書作成
- [ ] フェーズ2: カスタム引数パーサー実装
- [ ] フェーズ3: COM APIラッパー実装
- [ ] フェーズ4: Gist同期機能統合
- [ ] フェーズ5: テストと品質保証

### マイルストーン
1. **ドキュメント完成**: 全WinGetコマンド仕様確定
2. **MVP完成**: 基本5コマンド動作確認
3. **フル機能完成**: 全18コマンド実装
4. **PowerShell版互換**: 相互運用確認
5. **プロダクション品質**: テスト・品質保証完了

### 品質ゲート
- ✅ 引数解析100%テストカバレッジ
- ✅ winget.exe動作互換性
- ✅ PowerShell版GistGet互換性
- ✅ COM API安定性検証
- ✅ メモリリーク・例外安全性確認