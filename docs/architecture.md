# GistGet アーキテクチャ設計

## 0. MVP実装アーキテクチャ（現在のフェーズ）

### MVP設計方針
- **最小限の実装**: 動作確認を最優先
- **段階的改善**: 複雑性を後から追加
- **直接的なコード**: 抽象化を避ける

### MVP Phase 1: パススルーのみ（50行）
```
Program.cs (20行)
  └→ WinGetPassthrough.cs (30行)
       └→ winget.exe
```

**Program.cs例:**
```csharp
// 最小限のエントリポイント
class Program
{
    static async Task<int> Main(string[] args)
    {
        var passthrough = new WinGetPassthrough();
        return await passthrough.ExecuteAsync(args);
    }
}
```

### MVP Phase 2: ルーティング追加（200行）
```
Program.cs
  └→ CommandRouter.cs (50行)
       ├→ WinGetComClient.cs (100行) [install/uninstall/upgrade]
       │    └→ COM API
       └→ WinGetPassthrough.cs (30行) [その他]
            └→ winget.exe
```

**CommandRouter.cs例:**
```csharp
public class CommandRouter
{
    public async Task<int> RouteAsync(string[] args)
    {
        var command = args.FirstOrDefault();
        
        if (command is "install" or "uninstall" or "upgrade")
        {
            // COM経由
            var comClient = new WinGetComClient();
            return await comClient.ExecuteAsync(args);
        }
        else
        {
            // パススルー
            var passthrough = new WinGetPassthrough();
            return await passthrough.ExecuteAsync(args);
        }
    }
}
```

### MVP Phase 3: Gistスタブ追加（250行）
```
Program.cs
  └→ CommandRouter.cs
       ├→ WinGetComClient.cs
       │    └→ GistSyncStub.cs (30行) [ログ出力のみ]
       └→ WinGetPassthrough.cs
```

**GistSyncStub.cs例:**
```csharp
public class GistSyncStub
{
    public void AfterInstall(string packageId)
    {
        Console.WriteLine($"[Gist] Updated: Added {packageId}");
    }
    
    public void Sync()
    {
        Console.WriteLine("[Gist] Syncing from Gist... (stub)");
        Console.WriteLine("[Gist] 3 packages would be installed");
    }
}
```

### MVP実装の利点

1. **即座に動作確認可能** - 50行で基本動作
2. **段階的に複雑性を追加** - 必要な部分だけ実装
3. **失敗してもやり直しが簡単** - コード量が少ない
4. **本質的な課題が見える** - 実際に動かして初めて分かる問題

---

## 1. 設計方針の変更

### 従来の課題
- WinGetの薄いラッパーとして実装し、すべてCOM呼び出しで代替しようと考えていた
- しかし、search/listは結果を画面に表示するにあたってコンソールの幅を把握して表示を調整しており、簡単にラップすることが困難

### 新方針：ハイブリッドアーキテクチャ
- Gistで端末間の同期が必要な機能だけCOM経由で呼び出し
- それ以外はバイパスしてwingetを呼ぶだけの方針に変更

## 2. アーキテクチャ詳細

### A. システム構成

```
┌─────────────────────────────────────────┐
│            CLI Interface                 │ Program.cs
├─────────────────────────────────────────┤
│         Command Router                   │ コマンド分類・ルーティング
├──────────────┬──────────────────────────┤
│  COM利用     │    パススルー             │
│  (Gist同期)  │    (表示・管理系)         │
├──────────────┼──────────────────────────┤
│ WinGet COM   │   WinGet CLI             │
│   Client     │    Client                │
├──────────────┼──────────────────────────┤
│ COM API      │   winget.exe             │
└──────────────┴──────────────────────────┘
```

### B. コマンドルーティング戦略

```csharp
public class CommandRouter
{
    private static readonly HashSet<string> GistSyncCommands = new()
    {
        "install", "uninstall", "upgrade",  // Gist定義更新
        "sync", "export", "import"           // Gist同期専用
    };
    
    private static readonly HashSet<string> PassthroughCommands = new()
    {
        "search",  // 表示が複雑
        "list",    // コンソール幅依存
        "show",    // 詳細表示
        "source",  // 管理系
        "settings" // 設定画面起動
    };
    
    public async Task<int> RouteAsync(string[] args)
    {
        var command = args.FirstOrDefault()?.ToLower();
        
        if (GistSyncCommands.Contains(command))
        {
            // COM API経由で処理 + Gist同期
            return await HandleGistSyncCommand(args);
        }
        else
        {
            // winget.exeへパススルー
            return await PassthroughToWinGet(args);
        }
    }
}
```

### C. ハイブリッド実装の利点

1. **実装の簡素化**: 表示系の複雑な処理を再実装する必要がない
2. **互換性の維持**: wingetの出力形式が完全に保たれる
3. **保守性の向上**: wingetのアップデートに自動追従
4. **開発効率**: Gist同期機能に集中できる

### D. 最小限のCOM API実装

```csharp
public interface IWinGetClient
{
    // Gist同期に必要な機能のみ
    Task<IReadOnlyList<InstalledPackage>> GetInstalledPackagesAsync();
    Task<InstallResult> InstallPackageAsync(string packageId, string version = null);
    Task<UninstallResult> UninstallPackageAsync(string packageId);
    Task<UpgradeResult> UpgradePackageAsync(string packageId);
    
    // それ以外はwinget.exeへのパススルー
    Task<int> PassthroughAsync(string[] args);
}

public class HybridWinGetClient : IWinGetClient
{
    private readonly IComApiClient _comClient;
    private readonly IWinGetCliClient _cliClient;
    
    public async Task<IReadOnlyList<InstalledPackage>> GetInstalledPackagesAsync()
    {
        // COM API経由で正確なパッケージ情報を取得
        return await _comClient.GetInstalledPackagesAsync();
    }
    
    public async Task<int> PassthroughAsync(string[] args)
    {
        // search, show, list等の表示系コマンドは直接winget.exeへ
        return await _cliClient.ExecuteAsync(args);
    }
}
```

## 3. Gist同期機能

### A. 同期データ形式（PowerShell版準拠）

```yaml
# packages.yaml
Packages:
  - Id: Microsoft.VisualStudioCode
    Version: 1.85.0  # バージョン固定（省略可）
  - Id: Git.Git
  - Id: Microsoft.PowerToys
    Version: 0.76.0
```

### B. 同期パターン

| コマンド | 同期方向 | 説明 |
|----------|----------|------|
| `sync` | Gist → Local | Gist定義にあってローカルにないパッケージをインストール |
| `export` | Gist → Local | Gistから定義ファイルをダウンロード |
| `import` | Local → Gist | 現在の環境をスキャンしてGistへアップロード |
| `install/uninstall/upgrade` | Local → Gist | 操作後にGist定義を自動更新 |

### C. GitHub認証フロー

```
1. device_code と verification_uri を取得
   ↓
2. ブラウザ起動してユーザー認証
   ↓
3. access_token をポーリングで取得
   ↓
4. Windows DPAPI で暗号化保存
   ↓
5. Gist API呼び出し（Authorization: Bearer <token>）
```

## 4. 実装計画

### A. 開発フェーズ

| フェーズ | 内容 | 期間 | 状態 |
|---------|------|------|------|
| **Phase 1** | CLIパススルー基盤構築 | 1日 | 予定 |
| **Phase 2** | COM API最小実装（export/import用） | 3日 | 予定 |
| **Phase 3** | Gist同期機能（OAuth認証含む） | 3日 | 予定 |
| **Phase 4** | syncコマンド実装 | 2日 | 予定 |

### B. 技術スタック

- **フレームワーク**: .NET 8（自己完結型）
- **COM API**: Microsoft.WindowsPackageManager.ComInterop
- **引数パーサー**: System.CommandLine
- **HTTP通信**: HttpClient（GitHub API用）
- **YAML処理**: YamlDotNet（Gist同期用）
- **暗号化**: Windows DPAPI（トークン保存用）

### C. パッケージ化

- 自己完結型実行ファイル（.NET 8）
- GitHub Actions による CI/CD
- リリース時の自動ビルド・テスト
- 将来の拡張性よりも現在の単純性を優先

### 2. COM APIの直接利用
- 中間変換レイヤーを排除
- Microsoft製の型をドメインオブジェクトとして直接使用
- WinGetの薄いラッパーとしての本質に忠実

### 3. テスト可能性の確保
- 内部コンストラクタ経由でのテスト用依存性注入
- COM API操作の適切なモック化
- 環境依存テストの条件付き実行

### 4. Microsoft製型の直接採用
- `CatalogPackage`, `FindPackagesOptions`, `InstallResult`等の直接使用
- 独自のドメインモデルへの変換を避ける
- 型安全性とWinGet互換性の両立

### 5. 単一責任の徹底
- 各レイヤーの責務を明確に分離
- 横断的関心事の適切な分離
- コマンド単位での独立性確保

## コア・アーキテクチャ・コンポーネント

### 1. エントリポイント (Program.cs)

**責務**: アプリケーション初期化、依存性注入、エラーハンドリング

- .NET Generic Host を使用した依存性注入コンテナの構築
- System.CommandLine による引数解析とコマンド実行の統合
- グローバルエラーハンドリングとログ設定

### 2. 引数解析レイヤー (WinGetArgumentParser)

**責務**: WinGet CLI完全準拠の引数解析とバリデーション

主要機能：
- 18種類のコマンド（install, list, upgrade等）の完全サポート
- コマンドエイリアス（add→install, ls→list等）
- サブコマンド階層（source add/list/update等）
- 相互排他性・条件付きバリデーション
- グローバルオプション（--help, --version等）

### 3. コマンドハンドラーレイヤー

**責務**: 各WinGetコマンドの実行ロジック

- **BaseCommandHandler**: 共通インフラストラクチャ
- **具象ハンドラー**: InstallCommandHandler, ListCommandHandler等、各コマンド固有の処理

### 4. WinGetクライアントレイヤー

**責務**: WinGetの薄いラッパーとしてCOM APIを直接操作

- **IWinGetClient**: 公開インターフェース（COM型を直接返す）
- **WinGetComClient**: COM APIの直接呼び出し（中間変換なし）

**設計原則**:
- Microsoft製のCOM型（CatalogPackage等）をドメインオブジェクトとして直接使用
- 不要な型変換や抽象化レイヤーを排除
- WinGetの薄いラッパーとしての本質に忠実

### 5. データモデル

**責務**: Microsoft COM API型を直接ドメインオブジェクトとして使用

- **COM型をドメインモデルとして採用**: CatalogPackage, FindPackagesOptions, InstallResult等
- **表示形式の調整**: 必要に応じてFormatterクラスで対応
- **WinGetとの完全互換**: Microsoftが定義した型をそのまま活用

## 主要クラス概要

### Program クラス
- アプリケーション設定とブートストラップ
- 依存性注入の構成
- CommandLineConfigurationによる引数解析実行

### WinGetArgumentParser クラス
```csharp
public class WinGetArgumentParser : IWinGetArgumentParser
{
    // System.CommandLineのRootCommandを構築
    public Command BuildRootCommand()
    
    // 各コマンドの構築メソッド
    private Command BuildInstallCommand()
    private Command BuildListCommand()
    // ... その他18コマンド
}
```

### BaseCommandHandler クラス
```csharp
public abstract class BaseCommandHandler
{
    protected static IServiceProvider? ServiceProvider { get; private set; }
    
    public abstract Task<int> ExecuteAsync(/* パラメータ */);
}
```

### WinGetComClient クラス
```csharp
using Microsoft.Management.Deployment;

public class WinGetComClient : IWinGetClient, IDisposable
{
    private readonly ILogger<WinGetComClient> _logger;
    private PackageManager? _packageManager;
    
    // COM APIを直接操作（中間レイヤーなし）
    public async Task<IReadOnlyList<CatalogPackage>> SearchPackagesAsync(FindPackagesOptions options)
    {
        // 直接COM APIを呼び出し、COM型をそのまま返す
        var catalogRef = _packageManager!.GetPackageCatalogByName("winget");
        var connectResult = await catalogRef.ConnectAsync();
        var searchResult = await connectResult.PackageCatalog.FindPackagesAsync(options);
        
        return searchResult.Matches.Select(m => m.CatalogPackage).ToList();
    }
}
```

### IWinGetClient インターフェース
```csharp
using Microsoft.Management.Deployment;

public interface IWinGetClient
{
    Task<bool> InitializeAsync(CancellationToken cancellationToken = default);
    
    // COM型を直接返す（型変換なし）
    Task<IReadOnlyList<CatalogPackage>> SearchPackagesAsync(
        FindPackagesOptions options,
        IProgress<PackageOperationProgressState>? progress = null,
        CancellationToken cancellationToken = default);
    
    Task<InstallResult> InstallPackageAsync(
        CatalogPackage package,
        InstallOptions options,
        IProgress<InstallProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
```

## 設計パターンと原則

### 1. 依存性注入パターン
- .NET Generic Hostによる統一されたDIコンテナ
- 最小限のインターフェース分離（過度な抽象化を回避）

### 2. 直接操作パターン
- COM APIを直接呼び出し（中間レイヤーなし）
- Microsoft製型をドメインオブジェクトとして採用
- 薄いラッパーとしての本質に忠実

### 3. 例外処理パターン
- COM API専用の明確なエラーハンドリング
- 環境設定問題の明確な診断メッセージ
- 段階的エラー回復とユーザーガイダンス

### 4. コマンドパターン
- 各WinGetコマンドを独立したコマンドハンドラーとして実装
- 共通ベースクラスによる統一されたインターフェース

### 5. 最小設計原則（YAGNI）
- 必要最小限の抽象化のみ実装
- 型変換レイヤーの完全削除
- Microsoftの型定義を直接活用

## システム統合

### Microsoft.WindowsPackageManager.ComInterop統合
- バージョン: 1.11.430
- COM初期化: PackageManagerFactory.CreatePackageManager()
- 非同期操作: Task-based Async Pattern
- プログレス通知: IProgress&lt;OperationProgress&gt;
- 直接COM API呼び出しによる最適化

### System.CommandLine統合
- 引数解析フレームワーク
- コマンド階層とサブコマンドサポート
- 相互排他性とバリデーションルール

## エラーハンドリング戦略

### 階層的エラーハンドリング
1. **Program.cs**: 最上位例外キャッチとログ出力
2. **CommandHandler**: コマンド実行時の例外処理
3. **WinGetComClient**: COM API直接呼び出しの例外処理

### 明確なエラー処理
- COM API失敗時の詳細な診断情報提供
- 環境設定問題の明確な特定とガイダンス
- デバッグ支援のための充実したログ情報

## ログとモニタリング

### 統合ログ基盤
- Microsoft.Extensions.Logging使用
- 構造化ログでの詳細な処理情報記録
- COM API実行の透明性とデバッグ支援

### 診断情報
- ClientInfo による実行環境情報取得
- COM API初期化状況の把握
- WinGetバージョン互換性チェック

## セキュリティ考慮事項

### COM APIセキュリティ
- COM オブジェクト初期化の安全性確保
- 適切なCOM リソース管理

### 入力検証
- コマンドライン引数の厳密なバリデーション
- ValidationEngineによるルールベース検証

### 認証・認可（Gist同期）
- GitHub OAuth Device Flow による安全な認証
- Windows DPAPI による暗号化トークン保存
- 最小権限の原則（Gistスコープのみ）

## パフォーマンス最適化

### COM API戦略
- ネイティブCOM API直接利用による最適化
- 外部プロセス不要による高速化とリソース効率

### 非同期処理
- 全操作のTask-based Async Pattern対応
- CancellationToken による処理中断サポート

### リソース管理
- IDisposable による適切なリソース解放
- COM オブジェクトの確実な解放

### キャッシング戦略
- パッケージカタログ情報の適切なキャッシュ
- 頻繁なCOM API呼び出しの最小化
- メモリ使用量とパフォーマンスのバランス

## 既知の課題と対策

| 課題 | 影響 | 対策 | 状態 |
|------|------|------|------|
| COM API Windows限定 | クロスプラットフォーム不可 | 要件として明記、Windows専用として受容 | 受容済 |
| 管理者権限要求 | 一部操作で必要 | 昇格プロンプト実装、権限チェック | 対応予定 |
| COM初期化失敗 | 環境依存エラー | 詳細な診断メッセージ、環境チェック | 実装済 |
| パッケージマネージャー互換性 | バージョン差異によるAPI変更 | 最小バージョンチェック、互換性テスト | 対応予定 |
| COM リソースリーク | メモリ使用量増加 | 適切なDispose実装、リソース監視 | 実装中 |
| 大量パッケージ処理 | パフォーマンス劣化 | 非同期処理、ページング、進捗通知 | 対応予定 |

## 将来の拡張性

### Gist同期アーキテクチャ
```
src/GistSync/
├── IGistClient.cs              # GitHub API抽象化
├── GistClient.cs               # 実装
├── Authentication/
│   ├── OAuthDeviceFlow.cs      # OAuth認証
│   └── TokenManager.cs         # トークン管理
├── Models/
│   ├── GistPackageList.cs      # パッケージリスト
│   └── SyncSettings.cs         # 同期設定
└── Services/
    ├── GistSyncService.cs      # 同期ロジック
    └── ConflictResolver.cs     # 競合解決
```

### 設計考慮事項
- YAML形式でのパッケージリスト互換性
- オフラインキャッシュとオンライン同期の両立
- 複数デバイス間の競合解決戦略
- PowerShell版との相互運用性確保

## COM API実装のテスト戦略

### レイヤー別テスト方針

各アーキテクチャレイヤーに対応したテスト戦略：

#### 1. CLI Interface Layer (Program.cs)
**テスト種別**: E2Eテスト  
**方針**: 
- 実際のコマンドライン引数での動作確認
- 依存性注入コンテナの構築検証
- グローバルエラーハンドリングの確認

```csharp
[Fact]
public async Task Program_WithValidArguments_ShouldReturnZeroExitCode()
{
    // Arrange
    var args = new[] { "list", "--name", "git" };
    
    // Act
    var exitCode = await Program.Main(args);
    
    // Assert
    Assert.Equal(0, exitCode);
}
```

#### 2. Argument Parser Layer (WinGetArgumentParser)
**テスト種別**: 単体テスト  
**方針**: 
- System.CommandLineの設定検証
- 引数解析ロジックの網羅的テスト
- バリデーションルールの確認

```csharp
[Theory]
[InlineData(new[] { "install", "--id", "Git.Git" }, true)]
[InlineData(new[] { "install", "--id", "Git.Git", "--query", "git" }, false)] // 相互排他
public void ParseArguments_ShouldValidateCorrectly(string[] args, bool isValid)
{
    // 引数解析とバリデーションのテスト
}
```

#### 3. Command Handler Layer
**テスト種別**: 統合テスト (モック利用)  
**方針**: 
- IWinGetClientをモックして各ハンドラーのロジック検証
- エラーハンドリングとユーザーへのメッセージ確認

```csharp
[Fact]
public async Task InstallCommandHandler_WithValidPackage_ShouldReturnSuccess()
{
    // Arrange
    var mockClient = new Mock<IWinGetClient>();
    mockClient.Setup(x => x.InstallPackageAsync(It.IsAny<InstallOptions>(), null, default))
              .ReturnsAsync(OperationResult.Success("Installed"));
    
    // Act & Assert
    var handler = new InstallCommandHandler();
    var result = await handler.ExecuteAsync(/* parameters */);
    Assert.Equal(0, result);
}
```

#### 4. WinGet Client Layer (IWinGetClient → WinGetComClient)
**テスト種別**: 統合テスト + 結合テスト  
**方針**: 
- 実COM API環境での動作確認
- winget.exe直接呼び出しでの前提条件設定・結果検証
- COM API初期化とリソース管理の検証

##### 4-1. 基本機能の統合テスト
```csharp
[SkippableFact]
[Trait("Category", "Integration")]
public async Task InitializeAsync_Should_Successfully_Initialize_COM_API()
{
    Skip.IfNot(IsWinGetEnvironmentAvailable(), "WinGet環境が利用不可");
    
    // Arrange
    var client = CreateRealWinGetComClient();
    
    // Act
    var result = await client.InitializeAsync();
    
    // Assert
    Assert.True(result, "COM API should be available");
}
```

##### 4-2. 複雑なシナリオの結合テスト
```csharp
[SkippableFact]
[Trait("Category", "Integration")]
public async Task UpgradePackage_WithDowngradedPackage_ShouldUpgradeToLatest()
{
    Skip.IfNot(IsWinGetEnvironmentAvailable() && IsElevated(), "管理者権限とWinGet環境が必要");
    
    // Arrange - winget.exeで前提条件設定
    const string testPackageId = "Microsoft.WindowsTerminal.Preview";
    const string downgradedVersion = "1.18.2681.0";
    
    await EnsurePackageDowngradedAsync(testPackageId, downgradedVersion);
    
    var client = CreateRealWinGetComClient();
    await client.InitializeAsync();
    
    // Act - COM APIでアップグレード実行
    var upgradeOptions = new UpgradeOptions { Id = testPackageId };
    var result = await client.UpgradePackageAsync(upgradeOptions);
    
    // Assert - winget.exeで結果確認
    Assert.True(result.IsSuccess);
    var currentVersion = await GetInstalledVersionAsync(testPackageId);
    Assert.NotEqual(downgradedVersion, currentVersion);
    
    // Cleanup
    await RestorePackageStateAsync(testPackageId);
}

private async Task EnsurePackageDowngradedAsync(string packageId, string targetVersion)
{
    // winget.exeを直接呼び出してパッケージを特定バージョンにダウングレード
    var process = Process.Start(new ProcessStartInfo
    {
        FileName = "winget.exe",
        Arguments = $"install --id {packageId} --version {targetVersion} --force",
        UseShellExecute = false,
        CreateNoWindow = true
    });
    await process.WaitForExitAsync();
}

private async Task<string> GetInstalledVersionAsync(string packageId)
{
    // winget.exeでインストール済みバージョンを確認
    var process = Process.Start(new ProcessStartInfo
    {
        FileName = "winget.exe",
        Arguments = $"list --id {packageId} --exact",
        UseShellExecute = false,
        CreateNoWindow = true,
        RedirectStandardOutput = true
    });
    
    var output = await process.StandardOutput.ReadToEndAsync();
    await process.WaitForExitAsync();
    
    // Parse version from output
    return ParseVersionFromWinGetOutput(output);
}
```

#### 5. Microsoft WinGet API Layer (COM型直接操作)
**テスト種別**: 結合テスト  
**方針**: 
- IComInteropWrapperの実装検証
- COM API直接呼び出しの動作確認
- リソースリークとエラーハンドリング検証

```csharp
[SkippableFact]
[Trait("Category", "Integration")]
public void ComInteropWrapper_CreatePackageManager_ShouldReturnValidInstance()
{
    Skip.IfNot(IsWinGetEnvironmentAvailable(), "WinGet COM API環境が必要");
    
    // Arrange
    var wrapper = new ComInteropWrapper();
    
    // Act
    var packageManager = wrapper.CreatePackageManager();
    
    // Assert
    Assert.NotNull(packageManager);
    
    // Cleanup
    // COM リソースの適切な解放確認
}
```

### テスト環境とヘルパー

#### 環境チェックヘルパー
```csharp
public static class TestEnvironmentHelper
{
    public static bool IsWinGetEnvironmentAvailable()
    {
        return OperatingSystem.IsWindows() 
            && Environment.OSVersion.Version.Major >= 10
            && IsAppInstallerInstalled()
            && IsComApiAvailable();
    }
    
    public static bool IsElevated()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
    
    private static bool IsAppInstallerInstalled()
    {
        // App Installerパッケージの存在確認
        try
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "winget.exe",
                Arguments = "--version",
                UseShellExecute = false,
                CreateNoWindow = true
            });
            process?.WaitForExit();
            return process?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
```

### テスト実行戦略

| レイヤー | テスト種別 | 実行頻度 | 環境要件 | 重要度 |
|---------|-----------|----------|----------|-------|
| CLI Interface | E2E | CI/CD | Windows + WinGet | 高 |
| Argument Parser | 単体 | 毎回 | なし | 高 |
| Command Handler | 統合(Mock) | 毎回 | なし | 高 |
| WinGet Client | 統合+結合 | CI/CD | Windows + 管理者権限 | 最高 |
| COM API | 結合 | CI/CD | Windows + WinGet COM | 高 |

### CI/CDでのテスト実行

```yaml
# GitHub Actions設定例
test-strategy:
  strategy:
    matrix:
      test-type: [unit, integration, e2e]
  runs-on: windows-latest
  steps:
    - name: 単体テスト (Argument Parser層)
      if: matrix.test-type == 'unit'
      run: dotnet test --filter "Category=Unit"
      
    - name: 統合テスト (Command Handler層)
      if: matrix.test-type == 'integration'  
      run: dotnet test --filter "Category=Integration&Category!=RequiresElevation"
      
    - name: 結合テスト (COM API層)
      if: matrix.test-type == 'e2e'
      run: |
        # 管理者権限で実行
        Start-Process powershell -Verb RunAs -ArgumentList @(
          "dotnet test --filter 'Category=Integration&Category=RequiresElevation'"
        )
```