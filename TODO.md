# GistGet .NET 8 実装ロードマップ

## 概要
winget.exe完全準拠の.NET 8アプリケーション開発のロードマップ。現在は「CLIフォールバック削除とアーキテクチャ簡素化（フェーズ3.5）」を最優先で進行中。

---

## 🎯 現在の進捗サマリー
- ✅ 完了
  - フェーズ1: WinGetコマンド完全仕様書
  - フェーズ2: カスタム引数パーサー
  - フェーズ3: COM APIラッパー統合
- 🚨 進行中（最優先）
  - フェーズ3.5: CLIフォールバック削除とアーキテクチャ簡素化
- ⏳ 未着手
  - フェーズ4: Gist同期機能統合
  - フェーズ5: テストと品質保証

## 直近の最優先事項（フェーズ3.5）
- CLIフォールバック関連コードの削除
- WinGetComClientの簡素化（フォールバックと不要分岐の撤去）
- COM APIテスト基盤の最小セット導入（IComInteropWrapper）
- テストとドキュメントの整合更新

## マイルストーン
1. ✅ M1: ドキュメント完成
2. ✅ M2: 引数パーサー完成
3. ✅ M3: 基本5コマンド動作（install/list/upgrade/export/import）
4. ✅ M4: 全18コマンド実装（COM APIラッパー完了）
5. 🚨 M3.5: アーキテクチャ簡素化（現在）
6. ⏳ M5: Gist同期実装
7. ⏳ M6: プロダクション品質達成

---

## 実施実績（簡素）
- フェーズ1 成果物
  - docs/*: 仕様書・相互関係・バリデーション・使用例 一式
- フェーズ2 成果物
  - src/*: 18コマンド、エイリアス/サブコマンド、バリデーション、テスト34件
- フェーズ3 成果物
  - src/WinGetClient/*: IWinGetClient, WinGetComClient, Models, DI拡張
  - NuGet: Microsoft.WindowsPackageManager.ComInterop 1.11.430 統合済み

---

# 実行手順（上から順に実行）

## フェーズ3.5: CLIフォールバック削除とアーキテクチャ簡素化（最優先）
- 目的: 複雑性/コード量/条件分岐を削減し、テスト容易性と保守性を向上

### 1) 削除対象コンポーネント
```
削除:
├── WinGetCliClient.cs
├── Abstractions/
│   ├── IProcessRunner.cs
│   └── DefaultProcessRunner.cs
└── Tests/
    ├── WinGetCliClientTests.cs
    └── ProcessRunnerTests.cs
```

### 2) リファクタリングタスク
- WinGetComClientの簡素化
  - CLIフォールバックロジックの削除
  - 例外処理・初期化フローの簡素化
- インターフェース整理
  - IWinGetClientから不要メソッドの削除
  - IWinGetCliClient/IProcessRunner関連の削除
- COM APIテスト基盤の最小導入
  - IComInteropWrapper（抽象化）を追加
  - Moqで単体テスト可能に

### 3) 最小テスト追加（Red→Green→Refactor）
- インターフェース導入（IComInteropWrapper）
- ComInteropMockHelper（Moqセットアップ）
- WinGetComClientの基本動作テスト（CLI分岐廃止確認）

### 4) ドキュメント更新
- architecture.md（反映済み箇所の確認）
- README.md / API仕様の差分更新（CLI削除の明記）

### 5) 期待される効果
- コード量約30%削減、条件分岐大幅減、初期化負荷低下
- 単一実装で保守容易化、テスト容易性/品質向上

---

## フェーズ4: Gist同期機能統合
- 目的: PowerShell版機能との互換
- 設計/構成
```
src/GistSync/
├── IGistClient.cs
├── GistClient.cs
├── OAuthDeviceFlow.cs
├── TokenManager.cs
├── Models/
│   ├── GistFile.cs
│   └── SyncSettings.cs
└── Extensions/
    └── YamlSerializer.cs
```
- 機能
  - GitHub OAuth Device Flow
  - Gist API CRUD
  - DPAPI暗号化保存、環境変数（GIST_GET_*）
  - YAML互換、リスト同期、オフラインキャッシュ、再試行

---

## フェーズ5: テストと品質保証
- 目的: プロダクション品質（安定性/性能/互換）

### テスト戦略
```
tests/
├── Unit/
│   ├── Commands/                  （既存: 34）
│   └── WinGetClient/
│       ├── WinGetComClientTests.cs
│       ├── ComInteropWrapperTests.cs
│       └── Helpers/
│           └── ComInteropMockHelper.cs
├── Integration/
│   └── ComApiTests/
│       ├── ComApiIntegrationTests.cs
│       ├── ComApiTestFixture.cs
│       └── SkippableFactAttribute.cs
├── EndToEnd/
│   ├── WinGetCompat/
│   └── PowerShellCompat/
├── Performance/
│   ├── Benchmarks/
│   │   ├── ComApiBenchmarks.cs
│   │   └── BenchmarkConfig.cs
│   └── LoadTests/
└── Resources/
    ├── ComResourceLeakTests.cs
    └── DisposalTests.cs
```

### 実装優先順位
1) フェーズ3.5と並行
- IComInteropWrapper 定義
- ComInteropMockHelper 実装
- 基本単体テスト移行

2) フェーズ3.5完了後
- ComApiIntegrationTestFixture
- SkippableFact導入
- COM API統合テスト

3) フェーズ4前
- ベンチマーク
- リソースリークテスト
- CI/CD（GitHub Actions、Windowsランナー）

### テストデータ管理
```
tests/TestData/
├── Builders/
│   ├── WinGetPackageBuilder.cs
│   ├── InstallOptionsBuilder.cs
│   └── OperationResultBuilder.cs
├── Fixtures/
│   ├── TestPackageData.json
│   └── MockResponses.json
└── Helpers/
    ├── ComApiTestHelper.cs
    └── AssertionExtensions.cs
```

### 品質ゲート
- 単体テストカバレッジ 90%以上
- COM APIモックテスト 100%実装
- 統合テスト（Windows）実施
- winget.exe互換性/PowerShell版相互運用
- メモリ/COMリソース解放検証
- 例外安全性、Windows 10/11動作確認
- CI/CD（管理者権限テスト分離、定期ベンチ）

---

## 技術的決定事項（要点）
- 引数パーサー: System.CommandLine
- COM API: Microsoft.WindowsPackageManager.ComInterop 1.11.430
- フォールバック: CLI実行は廃止（COM専用化）
- テスト: xUnit + Shouldly + Moq、SkippableFact
- ベンチ: BenchmarkDotNet
- プログレス通知: IProgress<OperationProgress>

## アーキテクチャ簡素化の理由（抜粋）
- 二重実装/複雑性/パフォーマンス/テスト困難の解消
- 抽象化（IComInteropWrapper）でテスト容易性確保

## リスクと対策（抜粋）
- COM/Windows依存 → 要件明記、条件付き統合テスト
- 権限要求 → 昇格フロー整備
- 互換性 → YAML形式統一

## 参考リンク
- https://www.nuget.org/packages/Microsoft.WindowsPackageManager.ComInterop
- https://github.com/microsoft/winget-cli
- https://learn.microsoft.com/en-us/dotnet/standard/commandline/
- https://docs.github.com/en/apps/oauth-apps/building-oauth-apps/authorizing-oauth-apps