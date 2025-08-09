# GistGet .NET 8 実装ロードマップ

## 概要

winget.exe完全準拠の.NET 8アプリケーション開発に向けた詳細なロードマップです。

---

## 🎯 現在の進捗サマリー

### 完了済み
- ✅ **フェーズ1**: WinGetコマンド完全仕様書作成 (100%)
- ✅ **フェーズ2**: カスタム引数パーサー実装 (100%)
- ✅ **フェーズ3**: COM APIラッパー実装 (100% - 完了)

### 進行中
- 🚨 **フェーズ3.5**: CLIフォールバック削除とアーキテクチャ簡素化 【最優先】

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

## フェーズ3: COM APIラッパー実装 ✅ **完了 (100%)**

**期間**: 3-4週間  
**目標**: Microsoft.WindowsPackageManager.ComInterop完全活用

### 完了済み (100%)
```
src/WinGetClient/
├── IWinGetClient.cs              ✅ 公開インターフェース定義
├── WinGetComClient.cs            ✅ COM API統合実装
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
- ✅ 依存性注入 (DI) 統合

---

## 🚨 フェーズ3.5: CLIフォールバック削除とアーキテクチャ簡素化 **【最優先】**

**期間**: 1週間  
**目標**: 複雑性を削減し、保守性を向上

### 削除対象コンポーネント
```
削除予定:
├── WinGetCliClient.cs            ❌ CLIラッパー削除
├── Abstractions/
│   ├── IProcessRunner.cs         ❌ プロセス実行抽象化削除
│   └── DefaultProcessRunner.cs   ❌ デフォルト実装削除
└── Tests/
    ├── WinGetCliClientTests.cs   ❌ CLIテスト削除
    └── ProcessRunnerTests.cs     ❌ プロセステスト削除
```

### リファクタリングタスク
- [ ] **WinGetComClient簡素化**
  - [ ] CLIフォールバックロジック削除
  - [ ] エラーハンドリング簡素化
  - [ ] 初期化フロー最適化

- [ ] **インターフェース整理**
  - [ ] IWinGetClient から不要なメソッド削除
  - [ ] IWinGetCliClient インターフェース削除
  - [ ] IProcessRunner 関連インターフェース削除

- [ ] **COM APIテスト基盤構築**
  - [ ] IComInteropWrapper インターフェース作成
  - [ ] ComInteropMockHelper ヘルパークラス実装
  - [ ] テストフィクスチャ作成

- [ ] **テスト更新**
  - [ ] CLIフォールバック関連テスト削除
  - [ ] COM API専用テストに集中
  - [ ] Moqを使用した統一的なテスト実装
  - [ ] 統合テストフィクスチャ追加

- [ ] **ドキュメント更新**
  - [x] architecture.md更新（CLIフォールバック削除を反映）
  - [x] architecture.md更新（COM APIテスト戦略追加）
  - [ ] README.md更新
  - [ ] API仕様書更新

### 期待される効果
- **コード量削減**: 約30%のコード削減見込み
- **複雑性低減**: 条件分岐の大幅削減
- **テスト簡素化**: モック不要、実装テストに集中
- **パフォーマンス向上**: 不要な初期化処理削除
- **保守性向上**: シンプルな単一実装
- **テスト品質向上**: COM API専用のテスト戦略確立

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

## フェーズ5: テストと品質保証 ⏳ **未着手→詳細化**

**期間**: 2-3週間（延長）  
**目標**: プロダクション品質の達成

### COM APIテスト戦略（更新）
```
tests/
├── Unit/                           # 単体テスト (目標: 90%+カバレッジ)
│   ├── Commands/                  ✅ 実装済み (34テスト)
│   ├── WinGetClient/              
│   │   ├── WinGetComClientTests.cs      # Moqベーステスト
│   │   ├── ComInteropWrapperTests.cs    # ラッパーテスト
│   │   └── Helpers/                     # テストヘルパー
│   │       └── ComInteropMockHelper.cs  # Moqセットアップヘルパー
│   └── GistSync/                  ⏳ 同期機能テスト
├── Integration/                    # 統合テスト
│   ├── ComApiTests/              
│   │   ├── ComApiIntegrationTests.cs    # 実COM APIテスト
│   │   ├── ComApiTestFixture.cs         # テスト環境設定
│   │   └── SkippableFactAttribute.cs    # 条件付き実行
│   └── GistTests/                 ⏳ Gist同期
├── EndToEnd/                      # E2Eテスト
│   ├── WinGetCompat/              ⏳ winget.exe比較
│   └── PowerShellCompat/          ⏳ PS版互換性
├── Performance/                   # パフォーマンステスト
│   ├── Benchmarks/               
│   │   ├── ComApiBenchmarks.cs          # COM APIベンチマーク
│   │   └── BenchmarkConfig.cs           # ベンチマーク設定
│   └── LoadTests/                 ⏳ 負荷テスト
└── Resources/                     # リソーステスト
    ├── ComResourceLeakTests.cs           # メモリリーク検証
    └── DisposalTests.cs                  # リソース解放検証
```

### テスト実装優先順位
1. **即座に実装（フェーズ3.5と並行）**
   - [ ] IComInteropWrapper インターフェース定義
   - [ ] ComInteropMockHelper 実装
   - [ ] 基本的な単体テスト移行

2. **フェーズ3.5完了後**
   - [ ] ComApiIntegrationTestFixture 実装
   - [ ] 条件付きテスト（SkippableFact）導入
   - [ ] COM API統合テスト実装

3. **フェーズ4前**
   - [ ] パフォーマンスベンチマーク確立
   - [ ] リソースリークテスト実装
   - [ ] CI/CDパイプライン設定

### テストデータ管理
```
tests/TestData/
├── Builders/                      # テストデータビルダー
│   ├── WinGetPackageBuilder.cs
│   ├── InstallOptionsBuilder.cs
│   └── OperationResultBuilder.cs
├── Fixtures/                      # 共有フィクスチャ
│   ├── TestPackageData.json
│   └── MockResponses.json
└── Helpers/                       # テストヘルパー
    ├── ComApiTestHelper.cs
    └── AssertionExtensions.cs
```

### 品質ゲート（更新）
- [ ] 単体テストカバレッジ 90%以上
- [ ] COM APIモックテスト 100%実装
- [ ] 統合テスト（Windows環境）実施
- [ ] winget.exe動作互換性検証
- [ ] PowerShell版GistGet相互運用テスト
- [ ] メモリリーク検証 (dotMemory)
- [ ] COM リソース解放検証
- [ ] 例外安全性確認
- [ ] Windows 10/11環境テスト
- [ ] CI/CDパイプライン (GitHub Actions)
  - [ ] Windows環境での自動テスト
  - [ ] 管理者権限テストの分離実行
  - [ ] パフォーマンステストの定期実行

---

## 📊 進捗ダッシュボード

### コンポーネント別進捗
| コンポーネント | 進捗 | 状態 | 次のアクション |
|--------------|------|------|--------------|
| ドキュメント | 100% | ✅ 完了 | - |
| 引数パーサー | 100% | ✅ 完了 | - |
| COM APIラッパー | 100% | ✅ 完了 | - |
| **CLIフォールバック削除** | 10% | 🚨 **最優先** | **テスト基盤構築から開始** |
| **COM APIテスト基盤** | 0% | 🚨 **最優先** | **IComInteropWrapper作成** |
| Gist同期 | 0% | ⏳ 待機中 | フェーズ3.5完了後 |
| テスト | 40% | 🔄 部分的 | テスト基盤構築中 |

### マイルストーン
1. ✅ **M1: ドキュメント完成** (完了)
2. ✅ **M2: 引数パーサー完成** (完了)
3. ✅ **M3: 基本5コマンド動作** (install, list, upgrade, export, import) - 完了
4. ✅ **M4: 全18コマンド実装** - COM APIラッパー完了
5. 🚨 **M3.5: アーキテクチャ簡素化** - **現在の最優先事項**
6. ⏳ **M5: Gist同期実装**
7. ⏳ **M6: プロダクション品質達成**

### 今週の優先タスク (2025年1月第3週)
1. 🔴 **CLIフォールバック完全削除** - WinGetCliClient.cs削除
2. 🔴 **プロセス実行抽象化削除** - IProcessRunner関連削除
3. 🔴 **WinGetComClient簡素化** - フォールバックロジック削除
4. 🟡 **テスト更新** - 削除後のテスト調整
5. 🟡 **ドキュメント更新** - README.md, API仕様書

---

## 🚀 次のステップ

### 即座に着手すべきタスク (フェーズ3.5)
1. **COM APIテスト基盤構築**
   ```csharp
   // 新規作成: src/WinGetClient/Abstractions/IComInteropWrapper.cs
   public interface IComInteropWrapper
   {
       Task<PackageManager> CreatePackageManagerAsync();
       // 他のCOM操作...
   }
   ```

2. **テストヘルパー実装**
   ```csharp
   // 新規作成: tests/Unit/WinGetClient/Helpers/ComInteropMockHelper.cs
   public static class ComInteropMockHelper
   {
       public static Mock<IComInteropWrapper> CreateDefaultMock()
       {
           // デフォルトのモック動作設定
       }
   }
   ```

3. **CLIフォールバック削除**
   ```bash
   # 削除対象ファイル
   rm src/WinGetClient/WinGetCliClient.cs
   rm src/WinGetClient/Abstractions/IProcessRunner.cs
   rm src/WinGetClient/Abstractions/DefaultProcessRunner.cs
   ```

4. **WinGetComClient リファクタリング**
   - IComInteropWrapper 依存性注入
   - CLIフォールバック削除
   - テスト可能な設計へ

5. **テストスイート更新**
   - モックベースの単体テスト作成
   - 統合テストフィクスチャ実装
   - 条件付きテスト導入

### フェーズ3.5完了後の計画 (フェーズ4)
1. **IGistClient インターフェース設計**
   - GitHub Gist API操作の抽象化
   - CRUDオペレーション定義
   - 認証フロー統合

2. **OAuth Device Flow実装**
   - GitHub Apps認証
   - トークン取得・保存・更新
   - Windows DPAPI暗号化

3. **YAML互換性実装**
   - PowerShell版GistGetPackageクラス互換
   - YamlDotNet統合
   - シリアライゼーション最適化

---

## 📝 メモ

### 技術的決定事項
- **引数パーサー**: System.CommandLine採用 (ConsoleAppFrameworkから変更)
- **COM API**: Microsoft.WindowsPackageManager.ComInterop 1.11.430使用
- ~~**フォールバック**: COM API失敗時は自動的にCLI実行~~ → **削除決定**
- **テストフレームワーク**: xUnit + Shouldly + Moq
- **COM APIテスト**: IComInteropWrapper抽象化によるテスト可能設計
- **テストダブル**: Moqのみ使用（インメモリ実装は不要）
- **統合テスト**: SkippableFact による条件付き実行
- **ベンチマーク**: BenchmarkDotNet による性能測定
- ~~**プロセス抽象化**: IProcessRunner統合でテスト可能性向上~~ → **削除決定**
- **プログレス通知**: IProgress<OperationProgress>実装
- **エラーハンドリング**: 詳細なErrorDetailsとException情報

### アーキテクチャ簡素化の理由
| 問題点 | 影響 | 解決策 |
|--------|------|--------|
| CLIフォールバックの複雑性 | 保守困難、テスト複雑化 | **削除** |
| 実際の利用頻度が低い | 開発リソースの無駄 | **COM API専用化** |
| 二重実装の負担 | バグの温床 | **単一実装** |
| パフォーマンス劣化 | 初期化オーバーヘッド | **直接COM呼び出し** |
| テスト困難性 | COM直接依存 | **IComInteropWrapper抽象化** |

### リスクと対策
| リスク | 影響度 | 対策 |
|--------|--------|------|
| COM API依存性 | 高 | 明確な環境要件ドキュメント化 |
| Windows依存性 | 中 | 仕様として明記 |
| 管理者権限要求 | 中 | 権限昇格フロー最適化 |
| PowerShell版互換性 | 低 | YAML形式統一で対応 |

### 参考リンク
- [Microsoft.WindowsPackageManager.ComInterop](https://www.nuget.org/packages/Microsoft.WindowsPackageManager.ComInterop)
- [WinGet CLI GitHub](https://github.com/microsoft/winget-cli)
- [System.CommandLine Documentation](https://learn.microsoft.com/en-us/dotnet/standard/commandline/)
- [GitHub OAuth Device Flow](https://docs.github.com/en/apps/oauth-apps/building-oauth-apps/authorizing-oauth-apps)