# GistGet .NET 8 実装ロードマップ

## 概要
winget.exe完全準拠の.NET 8アプリケーション開発のロードマップ。現在は「COM API実装の完成とテスト基盤構築（フェーズ4）」を進行中。

---

## 🎯 現在の進捗サマリー
- ✅ 完了
  - フェーズ1: WinGetコマンド完全仕様書
  - フェーズ2: カスタム引数パーサー（18コマンド完全実装）
  - フェーズ3: COM API基本統合
  - フェーズ3.5: アーキテクチャ簡素化（CLIフォールバック削除）
- 🚨 進行中（最優先）
  - フェーズ4: COM API実装の完成とテスト基盤構築
- ⏳ 未着手
  - フェーズ5: Gist同期機能統合
  - フェーズ6: プロダクション品質保証

## 直近の最優先事項（フェーズ4）
- COM API主要メソッドの実装完成
- テスト基盤の構築（単体テスト・統合テスト）
- エラーハンドリングとログの強化
- パフォーマンス最適化

## マイルストーン
1. ✅ M1: ドキュメント完成
2. ✅ M2: 引数パーサー完成（18コマンド）
3. ✅ M3: COM API基本統合
4. ✅ M3.5: アーキテクチャ簡素化
5. 🚨 M4: COM API全機能実装（現在）
6. ⏳ M5: Gist同期実装
7. ⏳ M6: プロダクション品質達成

---

# 実行手順（上から順に実行）

## フェーズ4: COM API実装の完成とテスト基盤構築（現在進行中）

### 4.1 残りのCOM APIメソッド実装

#### 優先度1: 基本操作（必須）
- [ ] `SearchPackagesAsync` - パッケージ検索
  - [ ] FindPackagesOptionsの構築
  - [ ] カタログ接続とクエリ実行
  - [ ] 結果のマッピング（CatalogPackage → ドメインモデル）
- [ ] `ListInstalledPackagesAsync` - インストール済み一覧
  - [ ] ローカルカタログの取得
  - [ ] フィルタリング条件の適用
- [ ] `InstallPackageAsync` - パッケージインストール
  - [ ] InstallOptionsの設定
  - [ ] プログレス通知の実装
  - [ ] インストール結果の処理

#### 優先度2: 管理操作
- [ ] `UpgradePackageAsync` - パッケージ更新
- [ ] `UninstallPackageAsync` - パッケージ削除
- [ ] `GetPackageDetailsAsync` - 詳細情報取得

#### 優先度3: 高度な機能
- [ ] `ExportPackagesAsync` - 設定エクスポート
- [ ] `ImportPackagesAsync` - 設定インポート
- [ ] `GetAvailableUpgradesAsync` - 更新可能パッケージ一覧

### 4.2 テスト基盤構築

#### 単体テスト
```csharp
// WinGetComClientTests.cs
- [ ] InitializeAsync のテスト
- [ ] SearchPackagesAsync のテスト（モック使用）
- [ ] エラーハンドリングのテスト
- [ ] リソース管理（Dispose）のテスト
```

#### 統合テスト（Windows環境必須）
```csharp
// ComApiIntegrationTests.cs
[SkippableFact]
- [ ] 実際のCOM API初期化テスト
- [ ] 実パッケージ検索テスト
- [ ] インストール/アンインストールの往復テスト
```

### 4.3 エラーハンドリング強化
- [ ] COM例外の適切なラッピング
- [ ] ユーザーフレンドリーなエラーメッセージ
- [ ] リトライロジック（一時的エラー対応）
- [ ] 診断情報の充実

### 4.4 パフォーマンス最適化
- [ ] 非同期処理の最適化
- [ ] キャッシング戦略（カタログ情報）
- [ ] メモリ使用量の監視
- [ ] COM呼び出しの最小化

---

## フェーズ5: Gist同期機能統合

### 5.1 基本設計
```
src/GistSync/
├── IGistClient.cs              # Gist APIインターフェース
├── GistClient.cs                # Gist API実装
├── GistSyncService.cs           # 同期ロジック
├── Authentication/
│   ├── OAuthDeviceFlow.cs      # GitHub認証
│   └── TokenManager.cs         # トークン管理
└── Models/
    ├── GistPackageList.cs       # パッケージリスト
    └── SyncSettings.cs          # 同期設定
```

### 5.2 実装タスク
- [ ] GitHub OAuth Device Flow認証
- [ ] Gist CRUD操作
- [ ] YAML形式でのパッケージリスト管理
- [ ] 同期コマンドの追加（sync push/pull/status）
- [ ] オフラインキャッシュ
- [ ] 競合解決ロジック

---

## フェーズ6: プロダクション品質保証

### 6.1 テストカバレッジ
- [ ] 単体テストカバレッジ 90%以上
- [ ] 統合テスト（全コマンド）
- [ ] E2Eテスト（実際の使用シナリオ）
- [ ] パフォーマンステスト

### 6.2 ドキュメント
- [ ] APIドキュメント生成
- [ ] ユーザーガイド
- [ ] トラブルシューティングガイド
- [ ] 貢献者ガイド

### 6.3 CI/CD
- [ ] GitHub Actions設定
- [ ] 自動テスト実行
- [ ] コードカバレッジレポート
- [ ] リリース自動化

### 6.4 配布
- [ ] NuGetパッケージ化
- [ ] インストーラー作成
- [ ] Chocolatey/Scoopパッケージ
- [ ] dotnet toolパッケージ

---

## 技術スタック（確定）
- **フレームワーク**: .NET 8
- **引数パーサー**: System.CommandLine
- **COM API**: Microsoft.WindowsPackageManager.ComInterop 1.11.430
- **テスト**: xUnit + Moq + Shouldly
- **ベンチマーク**: BenchmarkDotNet
- **ログ**: Microsoft.Extensions.Logging
- **DI**: Microsoft.Extensions.DependencyInjection

## アーキテクチャ原則（確定）
- ✅ YAGNI原則の遵守（不要な抽象化を排除）
- ✅ COM APIの直接利用（中間レイヤーなし）
- ✅ Microsoft製の型をドメインモデルとして採用
- ✅ テスト可能性の確保（内部コンストラクタ経由）
- ✅ 薄いラッパーとしての本質に忠実

## 既知の課題と対策
| 課題 | 影響 | 対策 | 状態 |
|------|------|------|------|
| COM API Windows限定 | クロスプラットフォーム不可 | 要件として明記 | 受容済 |
| 管理者権限要求 | 一部操作で必要 | 昇格プロンプト実装 | 対応予定 |
| COM初期化失敗 | 環境依存エラー | 詳細な診断メッセージ | 実装済 |
| パッケージマネージャー互換性 | バージョン差異 | 最小バージョンチェック | 対応予定 |

## 参考リンク
- [Microsoft.WindowsPackageManager.ComInterop](https://www.nuget.org/packages/Microsoft.WindowsPackageManager.ComInterop)
- [WinGet CLI GitHub](https://github.com/microsoft/winget-cli)
- [System.CommandLine](https://learn.microsoft.com/en-us/dotnet/standard/commandline/)
- [GitHub OAuth Device Flow](https://docs.github.com/en/apps/oauth-apps/building-oauth-apps/authorizing-oauth-apps#device-flow)

---

## 変更履歴
- 2024-12-XX: フェーズ3.5完了、アーキテクチャ簡素化実施
- 2024-12-XX: フェーズ4開始、COM API実装継続
- 2024-12-XX: TODO.md全面改訂、現状に合わせて更新