# GistGet .NET 8 実装ロードマップ

## 概要

winget.exe完全準拠の.NET 8アプリケーション開発に向けた詳細なロードマップです。

---

## 🎯 現在の進捗サマリー

### 完了済み
- ✅ **フェーズ1**: WinGetコマンド完全仕様書作成 (100%)
- ✅ **フェーズ2**: カスタム引数パーサー実装 (100%)
- 🔄 **フェーズ3**: COM APIラッパー実装 (60% - 基本構造完了)

### 進行中
- 🚧 **フェーズ3**: COM API詳細実装とパッケージ操作

### 未着手
- ⏳ **フェーズ4**: Gist同期機能統合
- ⏳ **フェーズ5**: テストと品質保証

---

## フェーズ1: WinGetコマンド完全仕様書作成 ✅ **完了**

**期間**: 1-2週間 → **完了**  
**目標**: winget.exeの全コマンド・オプション完全準拠ドキュメント

### 成果物（完了）
- docs/
  - [winget-commands-spec.md](docs/winget-commands-spec.md) ✅ 全18コマンド詳細仕様書
  - [winget-arguments-matrix.md](docs/winget-arguments-matrix.md) ✅ 引数相互関係マトリックス
  - [winget-validation-rules.md](docs/winget-validation-rules.md) ✅ バリデーションルール定義
  - [winget-examples.md](docs/winget-examples.md) ✅ 使用例集

---

## フェーズ2: カスタム引数パーサー実装 ✅ **完了**

**期間**: 2-3週間 → **完了**  
**目標**: WinGet完全準拠の引数解析エンジン

### 成果物（完了）
```
src/
├── NuitsJp.GistGet.sln                    ✅ ソリューション構成
├── NuitsJp.GistGet/
│   ├── Commands/                          ✅ 全18コマンドハンドラー
│   ├── Options/                           ✅ オプションモデル完全実装
│   ├── Validation/                        ✅ バリデーションエンジン
│   └── Program.cs                         ✅ System.CommandLine統合
└── NuitsJp.GistGet.Tests/
    ├── Commands/                          ✅ コマンドテスト
    ├── Options/                           ✅ オプションテスト
    └── Validation/                        ✅ バリデーションテスト
```

### 実装済み機能
- ✅ System.CommandLineによる18コマンド構造
- ✅ コマンドエイリアス (add→install, ls→list, update→upgrade)
- ✅ サブコマンド階層 (source add/list/update/remove/reset/export)
- ✅ グローバルオプション完全対応
- ✅ 相互排他性・条件付きバリデーション
- ✅ 包括的テストスイート (34テスト全て成功)

---

## フェーズ3: COM APIラッパー実装 🔄 **進行中 (60%)**

**期間**: 3-4週間  
**目標**: Microsoft.WindowsPackageManager.ComInterop完全活用

### 完了済み (60%)
```
src/WinGetClient/
├── IWinGetClient.cs              ✅ 公開インターフェース定義
├── WinGetComClient.cs            ✅ 基本構造とCLIフォールバック
├── WinGetCliClient.cs            ✅ CLIラッパー実装
├── Models/
│   ├── WinGetPackage.cs          ✅ パッケージモデル
│   ├── OperationResult.cs        ✅ 操作結果モデル
│   └── Options/                  ✅ オプションモデル群
└── Extensions/
    └── ServiceCollectionExt.cs   ✅ DI統合
```

### 実装済み機能
- ✅ Microsoft.WindowsPackageManager.ComInterop統合 (1.11.430)
- ✅ プロジェクト設定最適化 (net8.0-windows10.0.26100, x64)
- ✅ IWinGetClient完全インターフェース定義
- ✅ 自動CLIフォールバック機構
- ✅ 依存性注入 (DI) 統合
- ✅ Import/Export CLIフォールバック実装とテスト

### 残タスク (40%)

#### 3.1 パッケージ操作実装 【最優先】
- [ ] **Install実装** - COM API + プログレス通知
- [ ] **List実装** - インストール済みパッケージ列挙
- [ ] **Upgrade実装** - アップグレード可能パッケージ検出と更新
- [ ] **Uninstall実装** - アンインストール処理
- [ ] **Search実装** - パッケージ検索とフィルタリング
- [ ] **Show実装** - パッケージ詳細情報取得

#### 3.2 ソース管理実装
- [ ] **Source Add** - カスタムソース追加
- [ ] **Source List** - ソース一覧取得
- [ ] **Source Update** - ソース情報更新
- [ ] **Source Remove** - ソース削除
- [ ] **Source Reset** - デフォルトソースリセット

#### 3.3 高度な機能実装
- [ ] **Pin管理** - パッケージバージョン固定
- [ ] **Configure** - DSC構成適用
- [ ] **Download** - インストーラーダウンロード
- [ ] **Repair** - パッケージ修復

#### 3.4 品質向上
- [ ] **エラーハンドリング強化** - 詳細なエラー情報
- [ ] **ログ機構実装** - ILogger統合
- [ ] **非同期処理最適化** - CancellationToken対応
- [ ] **進捗レポート** - IProgress<T>実装
- [ ] **COM API統合テスト** - 実環境テスト
- [ ] **パフォーマンス最適化** - 接続プール、キャッシュ

---

## フェーズ4: Gist同期機能統合 ⏳ **未着手**

**期間**: 2-3週間  
**目標**: PowerShell版機能との完全互換

### 計画タスク
```
src/GistSync/
├── IGistClient.cs               # Gist APIインターフェース
├── GistClient.cs                # GitHub API実装
├── OAuthDeviceFlow.cs           # Device Flow認証
├── TokenManager.cs              # トークン暗号化管理
├── Models/
│   ├── GistFile.cs              # Gistファイルモデル
│   └── SyncSettings.cs          # 同期設定
└── Extensions/
    └── YamlSerializer.cs        # YAML互換性
```

### 実装予定機能
- [ ] GitHub OAuth Device Flow認証
- [ ] GitHub Gist API CRUD操作
- [ ] Windows DPAPI暗号化保存
- [ ] 環境変数管理 (GIST_GET_*)
- [ ] YAML形式互換性 (PowerShell版)
- [ ] パッケージリスト同期
- [ ] オフラインキャッシュ
- [ ] 再試行機構

---

## フェーズ5: テストと品質保証 ⏳ **未着手**

**期間**: 2週間  
**目標**: プロダクション品質の達成

### テスト戦略
```
tests/
├── Unit/                    # 単体テスト (目標: 90%+カバレッジ)
│   ├── Commands/           ✅ 実装済み (34テスト)
│   ├── WinGetClient/       ⏳ COM APIテスト
│   └── GistSync/           ⏳ 同期機能テスト
├── Integration/            # 統合テスト
│   ├── ComApiTests/        ⏳ COM API統合
│   ├── CliTests/           ⏳ CLI互換性
│   └── GistTests/          ⏳ Gist同期
├── EndToEnd/              # E2Eテスト
│   ├── WinGetCompat/       ⏳ winget.exe比較
│   └── PowerShellCompat/   ⏳ PS版互換性
└── Performance/           # パフォーマンステスト
    ├── Benchmarks/         ⏳ ベンチマーク
    └── LoadTests/          ⏳ 負荷テスト
```

### 品質ゲート
- [ ] 単体テストカバレッジ 90%以上
- [ ] winget.exe動作互換性検証
- [ ] PowerShell版GistGet相互運用テスト
- [ ] メモリリーク検証 (dotMemory)
- [ ] 例外安全性確認
- [ ] Windows 10/11環境テスト
- [ ] CI/CDパイプライン (GitHub Actions)

---

## 📊 進捗ダッシュボード

### コンポーネント別進捗
| コンポーネント | 進捗 | 状態 | 次のアクション |
|--------------|------|------|--------------|
| ドキュメント | 100% | ✅ 完了 | - |
| 引数パーサー | 100% | ✅ 完了 | - |
| COM APIラッパー | 60% | 🔄 進行中 | パッケージ操作実装 |
| Gist同期 | 0% | ⏳ 未着手 | OAuth実装から開始 |
| テスト | 30% | 🔄 部分的 | COM APIテスト追加 |

### マイルストーン
1. ✅ **M1: ドキュメント完成** (完了)
2. ✅ **M2: 引数パーサー完成** (完了)
3. 🔄 **M3: 基本5コマンド動作** (install, list, upgrade, export, import)
4. ⏳ **M4: 全18コマンド実装**
5. ⏳ **M5: Gist同期実装**
6. ⏳ **M6: プロダクション品質達成**

### 今週の優先タスク (2024年12月第4週)
1. 🔴 **Install/List/Upgrade COM API実装** - 基本パッケージ操作
2. 🟡 **COM API統合テスト作成** - 動作検証
3. 🟡 **エラーハンドリング強化** - ユーザビリティ向上

---

## 🚀 次のステップ

### 即座に着手すべきタスク
1. **WinGetComClient.InstallAsync()実装**
   - PackageManagerFactory初期化
   - パッケージ検索とマッチング
   - インストール実行とプログレス通知

2. **WinGetComClient.ListAsync()実装**
   - インストール済みパッケージ列挙
   - フィルタリング条件適用
   - 結果のマッピング

3. **統合テスト環境構築**
   - テスト用パッケージ選定
   - モック/スタブ戦略決定
   - CI/CD統合準備

---

## 📝 メモ

### 技術的決定事項
- **引数パーサー**: System.CommandLine採用 (ConsoleAppFrameworkから変更)
- **COM API**: Microsoft.WindowsPackageManager.ComInterop 1.11.430使用
- **フォールバック**: COM API失敗時は自動的にCLI実行
- **テストフレームワーク**: xUnit + Moq

### リスクと対策
| リスク | 影響度 | 対策 |
|--------|--------|------|
| COM API不安定性 | 高 | CLIフォールバック実装済み |
| Windows依存性 | 中 | 仕様として明記、将来的にMono検討 |
| 管理者権限要求 | 中 | 権限昇格フロー最適化予定 |
| PowerShell版互換性 | 低 | YAML形式統一で対応予定 |

### 参考リンク
- [Microsoft.WindowsPackageManager.ComInterop](https://www.nuget.org/packages/Microsoft.WindowsPackageManager.ComInterop)
- [WinGet CLI GitHub](https://github.com/microsoft/winget-cli)
- [System.CommandLine Documentation](https://learn.microsoft.com/en-us/dotnet/standard/commandline/)
- [GitHub OAuth Device Flow](https://docs.github.com/en/apps/oauth-apps/building-oauth-apps/authorizing-oauth-apps)