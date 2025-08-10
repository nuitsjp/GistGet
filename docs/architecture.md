# GistGet .NET アーキテクチャ設計書

## 概要

GistGetは、WinGetの機能を.NET 8で実装し、GitHub Gistとの同期機能を提供するツールです。本書では現在の実装アーキテクチャについて詳述します。

## アーキテクチャ全体像

本アプリケーションは、以下の主要レイヤーで構成されています：

```
┌─────────────────────────────────────────┐
│            CLI Interface                 │ Program.cs
├─────────────────────────────────────────┤
│        Argument Parser Layer            │ WinGetArgumentParser
├─────────────────────────────────────────┤
│         Command Handler Layer           │ BaseCommandHandler + 具象ハンドラ
├─────────────────────────────────────────┤
│         WinGet Client Layer            │ IWinGetClient → WinGetComClient
├─────────────────────────────────────────┤
│        Microsoft WinGet API            │ PackageManager (COM型を直接使用)
└─────────────────────────────────────────┘
```

## 技術スタック

### コアフレームワーク
- **フレームワーク**: .NET 8
- **引数パーサー**: System.CommandLine
- **COM API**: Microsoft.WindowsPackageManager.ComInterop 1.11.430
- **依存性注入**: Microsoft.Extensions.DependencyInjection
- **ログ**: Microsoft.Extensions.Logging

### テスト・品質保証
- **テストフレームワーク**: xUnit
- **モック**: Moq
- **アサーション**: Shouldly
- **ベンチマーク**: BenchmarkDotNet
- **条件付きテスト**: SkippableFact（Xunit.SkippableFact）

### 追加ライブラリ
- **YAML処理**: YamlDotNet（Gist同期用）
- **HTTP通信**: HttpClient（GitHub API用）
- **暗号化**: Windows DPAPI（トークン保存用）

## アーキテクチャ原則

### 1. YAGNI原則の厳格な遵守
- 不要な抽象化レイヤーを排除
- 現在必要な機能のみを実装
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