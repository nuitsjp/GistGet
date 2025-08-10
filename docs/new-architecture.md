# GistGet .NET 8 新アーキテクチャ設計

## 1. 現在のMVP評価と課題

### MVP実装の成果

**実装完了項目（245行、3ファイル）:**
- ✅ **CommandRouter**: COM/パススルー判定ロジック（48行）
- ✅ **WinGetComClient**: COM API最小実装（112行） 
- ✅ **GistSyncStub**: Gist同期スタブ（40行）
- ✅ **WinGetPassthrough**: winget.exe直接呼び出し（36行）

**動作確認済み機能:**
- ✅ install/uninstall/upgrade → COM API経由 + Gist更新通知
- ✅ list/search/show → winget.exe直接パススルー
- ✅ sync/export/import → スタブメッセージ出力
- ✅ 標準出力・標準エラーの適切な継承
- ✅ コンソール幅調整の自動継承

### MVP実装の優れた点

1. **動作確認の速さ**: 短時間でコア機能を確認
2. **アーキテクチャ判断**: パススルー vs COM判定戦略の妥当性確認
3. **出力継承の検証**: wingetの表示機能が問題なく動作することを確認
4. **段階的実装**: 複雑性を少しずつ追加する手法の有効性

### 明らかになった課題と改善点

#### 1. アーキテクチャ課題
- **引数解析の不備**: 現在は単純な文字列比較のみ
- **エラーハンドリングの不足**: COM初期化失敗や権限不足への対応不十分
- **テスト戦略の欠如**: 単体テスト・統合テストの仕組みがない
- **設定管理の不在**: トークン保存や環境設定の管理機構がない

#### 2. コード品質課題
- **重複処理**: 各COM操作で似たようなエラーハンドリング
- **依存性注入の不在**: テスト可能性とモジュール性の問題
- **リソース管理**: COM オブジェクトのライフサイクル管理が不適切
- **ログ機能の欠如**: デバッグと運用時の問題切り分けが困難

#### 3. 機能課題
- **認証機能の不在**: GitHub OAuth Device Flowの実装必要
- **YAML処理の不在**: PowerShell版との互換性確保必要
- **競合解決**: 複数デバイス間の同期競合対策不足

## 2. 新アーキテクチャの設計方針

### 基本原則

1. **MVPで実証された戦略の継承**
   - COM/パススルーハイブリッド方式の継続
   - 最小限の中間レイヤー
   - wingetの出力形式完全保持

2. **エンタープライズ品質への昇格**
   - 包括的なテスト戦略
   - 適切なエラーハンドリング
   - 拡張可能なアーキテクチャ

3. **PowerShell版との互換性確保**
   - YAML形式の完全互換
   - パッケージパラメータの全面対応
   - OAuth認証フローの統一

## 3. レイヤード・アーキテクチャ

```
┌─────────────────────────────────────────────────────────┐
│                    CLI Interface                        │
│  Program.cs + System.CommandLine + DI Container        │
├─────────────────────────────────────────────────────────┤
│              Application Services                       │
│   CommandService + ValidationService + ConfigService   │
├─────────────────────────────────────────────────────────┤
│               Domain Services                           │
│  PackageService + GistSyncService + AuthService        │
├─────────────────────────┬───────────────────────────────┤
│      WinGet Client      │       GitHub Client           │
│                         │                               │
│  ┌─────────────────┐    │    ┌─────────────────────┐    │
│  │  COM API Client │    │    │   OAuth Client      │    │
│  │  (install/etc.) │    │    │   (Device Flow)     │    │
│  └─────────────────┘    │    └─────────────────────┘    │
│  ┌─────────────────┐    │    ┌─────────────────────┐    │
│  │ Passthrough     │    │    │   Gist API Client  │    │
│  │ (list/search)   │    │    │   (CRUD)            │    │
│  └─────────────────┘    │    └─────────────────────┘    │
├─────────────────────────┼───────────────────────────────┤
│       Data Layer        │       Configuration           │
│                         │                               │
│  ┌─────────────────┐    │    ┌─────────────────────┐    │
│  │  YAML Processor │    │    │   Token Manager     │    │
│  │  (Packages)     │    │    │   (DPAPI)           │    │
│  └─────────────────┘    │    └─────────────────────┘    │
└─────────────────────────┴───────────────────────────────┘
```

## 4. 主要コンポーネント詳細

### 4.1 CLI Interface Layer

**責務**: 引数解析・DI・グローバルエラーハンドリング

```csharp
// Program.cs
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var services = CreateServiceProvider();
        var commandService = services.GetRequiredService<ICommandService>();
        
        try
        {
            return await commandService.ExecuteAsync(args);
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Unhandled exception occurred");
            return 1;
        }
    }
    
    private static ServiceProvider CreateServiceProvider()
    {
        return new ServiceCollection()
            .AddSingleton<ICommandService, CommandService>()
            .AddSingleton<IWinGetClient, WinGetClient>()
            .AddSingleton<IGistClient, GistClient>()
            .AddSingleton<IAuthService, AuthService>()
            .AddLogging(builder => builder.AddConsole())
            .BuildServiceProvider();
    }
}
```

### 4.2 Application Services Layer

#### CommandService（MVPのCommandRouterを発展）

```csharp
public interface ICommandService
{
    Task<int> ExecuteAsync(string[] args);
}

public class CommandService : ICommandService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<CommandService> _logger;
    private readonly IWinGetClient _wingetClient;
    private readonly IGistSyncService _gistSync;

    public async Task<int> ExecuteAsync(string[] args)
    {
        var command = ParseCommand(args);
        
        return command.Type switch
        {
            CommandType.WinGetPassthrough => await ExecutePassthrough(command),
            CommandType.WinGetWithGistSync => await ExecuteWithGistSync(command),
            CommandType.GistOnly => await ExecuteGistOnly(command),
            _ => throw new ArgumentException($"Unknown command type: {command.Type}")
        };
    }
    
    private async Task<int> ExecuteWithGistSync(ParsedCommand command)
    {
        // COM API経由でWinGet操作実行
        var result = await _wingetClient.ExecuteAsync(command);
        
        if (result.Success)
        {
            // Gist同期（非同期・失敗時は警告のみ）
            _ = Task.Run(async () =>
            {
                try
                {
                    await _gistSync.SyncAfterOperationAsync(command);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Gist sync failed, but WinGet operation succeeded");
                }
            });
        }
        
        return result.ExitCode;
    }
}
```

#### ValidationService（引数バリデーション強化）

```csharp
public interface IValidationService
{
    ValidationResult ValidateCommand(ParsedCommand command);
}

public class ValidationService : IValidationService
{
    public ValidationResult ValidateCommand(ParsedCommand command)
    {
        var rules = GetValidationRules(command.Name);
        
        foreach (var rule in rules)
        {
            var result = rule.Validate(command);
            if (!result.IsValid)
                return result;
        }
        
        return ValidationResult.Success();
    }
    
    private IEnumerable<IValidationRule> GetValidationRules(string commandName)
    {
        return commandName switch
        {
            "install" => new IValidationRule[] 
            {
                new PackageIdRequiredRule(),
                new MutuallyExclusiveOptionsRule("id", "query"),
                new ElevationRequiredRule()
            },
            _ => Array.Empty<IValidationRule>()
        };
    }
}
```

### 4.3 Domain Services Layer

#### PackageService（WinGet操作の抽象化）

```csharp
public interface IPackageService
{
    Task<OperationResult<IReadOnlyList<Package>>> GetInstalledPackagesAsync();
    Task<OperationResult> InstallPackageAsync(InstallRequest request);
    Task<OperationResult> UninstallPackageAsync(UninstallRequest request);
    Task<OperationResult> UpgradePackageAsync(UpgradeRequest request);
}

public class PackageService : IPackageService
{
    private readonly IWinGetClient _client;
    private readonly ILogger<PackageService> _logger;

    public async Task<OperationResult> InstallPackageAsync(InstallRequest request)
    {
        try
        {
            await _client.InitializeAsync();
            
            var installOptions = MapToInstallOptions(request);
            var result = await _client.InstallPackageAsync(request.PackageId, installOptions);
            
            return MapToOperationResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install package {PackageId}", request.PackageId);
            return OperationResult.Failure($"Installation failed: {ex.Message}");
        }
    }
}
```

#### GistSyncService（同期ロジックの中核）

```csharp
public interface IGistSyncService
{
    Task<SyncResult> SyncFromGistAsync();
    Task<SyncResult> SyncToGistAsync();
    Task SyncAfterOperationAsync(ParsedCommand command);
}

public class GistSyncService : IGistSyncService
{
    private readonly IGistClient _gistClient;
    private readonly IPackageService _packageService;
    private readonly IYamlProcessor _yamlProcessor;
    private readonly ILogger<GistSyncService> _logger;

    public async Task<SyncResult> SyncFromGistAsync()
    {
        // 1. Gistから設定ファイルを取得
        var gistContent = await _gistClient.GetFileContentAsync("packages.yaml");
        
        // 2. YAMLをパッケージリストに変換
        var gistPackages = _yamlProcessor.DeserializePackages(gistContent);
        
        // 3. 現在のインストール済みパッケージを取得
        var installedResult = await _packageService.GetInstalledPackagesAsync();
        if (!installedResult.Success)
            return SyncResult.Failure("Failed to get installed packages");
            
        var installed = installedResult.Value.ToDictionary(p => p.Id);
        
        // 4. 差分を計算
        var toInstall = gistPackages.Where(p => !installed.ContainsKey(p.Id) && !p.Uninstall);
        var toUninstall = gistPackages.Where(p => installed.ContainsKey(p.Id) && p.Uninstall);
        
        // 5. 操作を実行
        var results = new List<OperationResult>();
        
        foreach (var package in toInstall)
        {
            var installRequest = MapToInstallRequest(package);
            var result = await _packageService.InstallPackageAsync(installRequest);
            results.Add(result);
        }
        
        foreach (var package in toUninstall)
        {
            var uninstallRequest = new UninstallRequest { PackageId = package.Id };
            var result = await _packageService.UninstallPackageAsync(uninstallRequest);
            results.Add(result);
        }
        
        return SyncResult.FromOperationResults(results);
    }
    
    public async Task SyncAfterOperationAsync(ParsedCommand command)
    {
        if (!ShouldSyncAfterOperation(command))
            return;
            
        // 現在の状態をGistに反映
        await SyncToGistAsync();
    }
    
    private bool ShouldSyncAfterOperation(ParsedCommand command) =>
        command.Name is "install" or "uninstall" or "upgrade";
}
```

### 4.4 Infrastructure Layer

#### WinGetClient（MVPから大幅強化）

```csharp
public interface IWinGetClient
{
    Task<bool> InitializeAsync();
    Task<WinGetResult<T>> ExecuteAsync<T>(WinGetOperation<T> operation);
    Task<int> PassthroughAsync(string[] args); // MVP機能継承
}

public class WinGetClient : IWinGetClient, IDisposable
{
    private readonly IComInteropWrapper _comWrapper;
    private readonly IProcessWrapper _processWrapper;
    private readonly ILogger<WinGetClient> _logger;
    private PackageManager? _packageManager;

    public async Task<WinGetResult<T>> ExecuteAsync<T>(WinGetOperation<T> operation)
    {
        if (!await EnsureInitializedAsync())
            return WinGetResult<T>.Failure("COM API initialization failed");

        try
        {
            return await operation.ExecuteAsync(_packageManager!, _logger);
        }
        catch (COMException ex)
        {
            _logger.LogError(ex, "COM operation failed: {Operation}", operation.GetType().Name);
            return WinGetResult<T>.Failure($"WinGet operation failed: {ex.Message}");
        }
    }
    
    // MVP実装を継承・改良
    public async Task<int> PassthroughAsync(string[] args)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "winget.exe",
            UseShellExecute = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false
        };

        foreach (var arg in args)
            startInfo.ArgumentList.Add(arg);

        using var process = _processWrapper.Start(startInfo);
        if (process == null)
        {
            _logger.LogError("Failed to start winget.exe");
            return 1;
        }

        await process.WaitForExitAsync();
        return process.ExitCode;
    }
}
```

#### YamlProcessor（PowerShell版互換）

```csharp
public interface IYamlProcessor
{
    List<GistPackage> DeserializePackages(string yaml);
    string SerializePackages(IEnumerable<GistPackage> packages);
}

public class YamlProcessor : IYamlProcessor
{
    private readonly IYamlDeserializer _deserializer;
    private readonly IYamlSerializer _serializer;

    public List<GistPackage> DeserializePackages(string yaml)
    {
        var dict = _deserializer.Deserialize<Dictionary<string, Dictionary<string, object>>>(yaml);
        var packages = new List<GistPackage>();

        foreach (var (packageId, properties) in dict)
        {
            var package = new GistPackage { Id = packageId };
            
            if (properties != null)
            {
                MapPropertiesToPackage(package, properties);
            }
            
            packages.Add(package);
        }

        return packages.OrderBy(p => p.Id).ToList();
    }
    
    private void MapPropertiesToPackage(GistPackage package, Dictionary<string, object> properties)
    {
        // PowerShell版のパラメータ完全対応
        foreach (var (key, value) in properties)
        {
            switch (key.ToLower())
            {
                case "allowhashmismatch":
                    package.AllowHashMismatch = Convert.ToBoolean(value);
                    break;
                case "architecture":
                    package.Architecture = value.ToString();
                    break;
                case "custom":
                    package.Custom = value.ToString();
                    break;
                // ... 他のパラメータも同様に対応
                case "uninstall":
                    package.Uninstall = Convert.ToBoolean(value);
                    break;
            }
        }
    }
}
```

## 5. データモデル

### GistPackage（PowerShell GistGetPackage互換）

```csharp
public class GistPackage
{
    public string Id { get; set; } = string.Empty;
    public bool AllowHashMismatch { get; set; }
    public string? Architecture { get; set; }
    public string? Custom { get; set; }
    public bool Force { get; set; }
    public string? Header { get; set; }
    public string? InstallerType { get; set; }
    public string? Locale { get; set; }
    public string? Location { get; set; }
    public string? Log { get; set; }
    public string? Mode { get; set; }
    public string? Override { get; set; }
    public string? Scope { get; set; }
    public bool SkipDependencies { get; set; }
    public string? Version { get; set; }
    public bool Confirm { get; set; }
    public bool WhatIf { get; set; }
    public bool Uninstall { get; set; }
    
    // PowerShell版と同じパラメータリスト
    public static readonly string[] SupportedParameters = {
        "allowHashMismatch", "architecture", "custom", "force", "header",
        "installerType", "locale", "location", "log", "mode", "override",
        "scope", "skipDependencies", "version", "confirm", "whatIf", "uninstall"
    };
}
```

## 6. テスト戦略

### 6.1 レイヤー別テスト方針

| レイヤー | テスト種別 | カバレッジ目標 | モック対象 |
|---------|-----------|---------------|-----------|
| CLI Interface | E2E | 90% | なし |
| Application Services | 統合 | 95% | Infrastructure Layer |
| Domain Services | 単体 | 98% | Infrastructure Layer |
| Infrastructure | 統合+結合 | 85% | 外部依存のみ |

### 6.2 テストプロジェクト構成

```
test/
├── GistGet.UnitTests/           # 単体テスト
│   ├── Services/
│   │   ├── CommandServiceTests.cs
│   │   ├── ValidationServiceTests.cs
│   │   └── GistSyncServiceTests.cs
│   └── Models/
│       └── GistPackageTests.cs
├── GistGet.IntegrationTests/    # 統合テスト
│   ├── WinGetClientTests.cs     # COM API統合
│   ├── GistClientTests.cs       # GitHub API統合  
│   └── YamlProcessorTests.cs    # YAML互換性
├── GistGet.E2ETests/           # E2Eテスト
│   ├── InstallScenarioTests.cs
│   ├── SyncScenarioTests.cs
│   └── PassthroughScenarioTests.cs
└── TestAssets/
    ├── sample-packages.yaml    # テスト用YAML
    └── mock-responses/         # モック用レスポンス
```

### 6.3 重要テストシナリオ

#### 1. WinGet互換性テスト
```csharp
[Fact]
public async Task PassthroughAsync_WithListCommand_ShouldProduceSameOutputAsWinget()
{
    // Direct winget execution
    var wingetOutput = await ExecuteWingetDirectly("list", "--count", "5");
    
    // GistGet passthrough execution  
    var gistgetOutput = await _wingetClient.PassthroughAsync(new[] {"list", "--count", "5"});
    
    Assert.Equal(wingetOutput, gistgetOutput);
}
```

#### 2. PowerShell互換性テスト
```csharp
[Fact]
public void YamlProcessor_WithPowerShellGeneratedYaml_ShouldDeserializeCorrectly()
{
    var powershellYaml = LoadTestAsset("powershell-generated.yaml");
    
    var packages = _yamlProcessor.DeserializePackages(powershellYaml);
    
    Assert.NotEmpty(packages);
    Assert.All(packages, package => Assert.NotNull(package.Id));
}
```

#### 3. Gist同期テスト
```csharp
[Fact]
public async Task SyncFromGistAsync_WithNewPackagesInGist_ShouldInstallMissingPackages()
{
    // Arrange
    var gistContent = CreateGistWithPackages("Git.Git", "7zip.7zip");
    var installedPackages = new[] { CreateMockPackage("Git.Git") };
    
    _gistClient.Setup(x => x.GetFileContentAsync("packages.yaml"))
              .ReturnsAsync(gistContent);
    _packageService.Setup(x => x.GetInstalledPackagesAsync())
                  .ReturnsAsync(OperationResult<IReadOnlyList<Package>>.Success(installedPackages));
    
    // Act
    var result = await _gistSyncService.SyncFromGistAsync();
    
    // Assert
    Assert.True(result.Success);
    _packageService.Verify(x => x.InstallPackageAsync(It.Is<InstallRequest>(r => r.PackageId == "7zip.7zip")), Times.Once);
}
```

## 7. 実装段階とマイルストーン

### Phase 1: 基盤アーキテクチャ構築（1週間）
- ✅ **依存性注入とサービス基盤**
- ✅ **引数解析とバリデーション強化**  
- ✅ **ログとエラーハンドリング統一**
- ✅ **テストプロジェクト基盤**

### Phase 2: WinGetクライアント完成（1週間）
- ✅ **COM APIクライアント安定化**
- ✅ **パススルー機能のテスト強化**
- ✅ **エラー処理と診断機能**
- ✅ **パフォーマンス最適化**

### Phase 3: Gist同期機能完成（2週間）
- ✅ **GitHub OAuth Device Flow実装**
- ✅ **Gist API クライアント実装** 
- ✅ **YAML処理（PowerShell互換）**
- ✅ **同期ロジックとスケジューラー**

### Phase 4: 統合・テスト・パッケージング（1週間）
- ✅ **E2Eテスト完成**
- ✅ **CI/CDパイプライン構築**
- ✅ **自己完結型バイナリ作成**
- ✅ **ドキュメント整備**

## 8. 成功メトリクス

### 機能品質メトリクス
- **WinGet互換性**: 全パススルーコマンドで100%同一出力
- **PowerShell互換性**: YAML形式完全互換（双方向変換テスト）
- **同期精度**: Gist↔Local同期で99.9%の整合性維持
- **パフォーマンス**: 95%のケースでwinget.exe直接実行と同等速度

### コード品質メトリクス  
- **テストカバレッジ**: 85%以上
- **循環複雑度**: 平均10以下
- **重複コード**: 5%以下
- **技術的負債**: SonarQube A評価

### 運用品質メトリクス
- **起動時間**: 1秒以内（初回COM初期化除く）
- **メモリ使用量**: 50MB以下（通常操作時）
- **クラッシュ率**: 0.1%以下
- **エラー回復率**: 90%以上（診断メッセージ提供）

## 9. 将来の拡張計画

### 短期拡張（3ヶ月以内）
- **複数Gistサポート**: 環境別設定ファイル管理
- **競合解決UI**: コンソール対話式マージ機能
- **スケジュール同期**: タスクスケジューラー連携

### 中期拡張（6ヶ月以内）
- **GUI版**: WPF/Avalonia ベースのグラフィカル版
- **プラグインシステム**: 独自インストーラー対応
- **エンタープライズ機能**: グループポリシー統合

### 長期拡張（1年以内）
- **クロスプラットフォーム**: Linux/macOSでの類似機能
- **クラウド同期**: Azure/AWS連携
- **AIアシスタント**: パッケージ推奨とメンテナンス自動化

---

## まとめ

このアーキテクチャは、MVPで実証された**ハイブリッド戦略**（COM + パススルー）を基盤とし、エンタープライズ品質に必要な**テスト可能性**、**拡張性**、**保守性**を追加した設計です。

PowerShell版との完全互換性を維持しながら、.NET 8の利点を活かした高性能・高信頼性のアプリケーションを構築することで、両プラットフォームのユーザーに最適な体験を提供します。