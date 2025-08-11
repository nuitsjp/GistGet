# GistGet アーキテクチャ設計

## 1. 現在の実装状態

### 実装済み機能
- ✅ WinGet CLIへのパススルー実装
- ✅ COM API基盤の構築
- ✅ 基本的なコマンドハンドラー構造
- ⏳ Gist同期機能（未実装）
- ⏳ CI/CDパイプライン（未実装）

### アーキテクチャ概要

```
┌─────────────────────────────────────────┐
│            Program.cs                    │ エントリポイント
├─────────────────────────────────────────┤
│         CommandRouter                    │ コマンド分類・ルーティング  
├──────────────┬──────────────────────────┤
│  COM API     │    パススルー             │
│  (将来拡張)   │    (現在のメイン)         │
├──────────────┼──────────────────────────┤
│ WinGetClient │   ProcessRunner          │
│              │                          │
├──────────────┼──────────────────────────┤
│ COM API      │   winget.exe             │
└──────────────┴──────────────────────────┘
```

## 2. 引数処理戦略

### 二段階の引数処理アプローチ

GistGetは引数処理において、**最小限の解釈**と**完全なパススルー**を使い分けます：

#### 第1段階: 最小限の解釈（ルーティング判定のみ）
```csharp
// 第1引数のコマンドのみを確認してルーティング決定
var command = args.FirstOrDefault()?.ToLower();

switch (command)
{
    case "sync":
    case "export":
    case "import":
        // GistGet独自コマンド → System.CommandLineで完全解析
        return await HandleGistCommand(args);
        
    case "install" when HasGistSyncEnabled():
    case "uninstall" when HasGistSyncEnabled():
        // Gist同期が有効な場合のみCOM API経由
        return await HandleWithComApi(args);
        
    default:
        // その他すべて → 引数を一切解釈せずパススルー
        return await PassthroughToWinGet(args);
}
```

#### 第2段階: コマンド別の処理

| パターン | 対象コマンド | 引数処理 | 理由 |
|---------|------------|---------|------|
| **完全パススルー** | search, list, show, source, settings等 | 引数を**一切解釈せず**そのまま渡す | WinGetの複雑な引数体系と完全互換を保証 |
| **Gist独自コマンド** | sync, export, import | System.CommandLineで**完全解析** | GistGet独自機能のため独自の引数体系 |
| **ハイブリッド** | install, uninstall（Gist同期時） | 最小限の解析後、残りをCOM APIへ | Gist同期のための情報抽出が必要 |

### System.CommandLine使用範囲の限定

```csharp
public class ArgumentStrategy
{
    // GistGet独自コマンドのみSystem.CommandLineを使用
    private static readonly HashSet<string> GistOnlyCommands = new()
    {
        "sync",   // gistget sync [--force]
        "export", // gistget export [--output file]
        "import"  // gistget import [--file file]
    };
    
    public bool ShouldParseArguments(string command)
    {
        // Gist独自コマンドのみ引数解析が必要
        return GistOnlyCommands.Contains(command);
    }
    
    public async Task<int> RouteCommand(string[] args)
    {
        var firstArg = args.FirstOrDefault()?.ToLower();
        
        if (ShouldParseArguments(firstArg))
        {
            // System.CommandLineで引数解析
            var parser = new GistCommandParser();
            return await parser.ParseAndExecute(args);
        }
        else
        {
            // 引数を一切触らずにパススルー
            return await ProcessRunner.RunWinGetAsync(args);
        }
    }
}
```

### パススルー時の注意点

**重要:** WinGetへのパススルー時は引数を**一切加工しない**
- 引数の順序を維持
- 大文字小文字を維持  
- 特殊文字やエスケープを維持
- 未知のオプションもそのまま渡す

```csharp
// ❌ 悪い例: 引数を解釈・加工してしまう
public async Task<int> BadPassthrough(string[] args)
{
    var parsed = ParseArguments(args);  // 不要な解析
    var reformatted = BuildWinGetArgs(parsed);  // 再構築で情報が失われる可能性
    return await RunWinGet(reformatted);
}

// ✅ 良い例: 引数をそのまま渡す
public async Task<int> GoodPassthrough(string[] args)
{
    // 引数配列をそのままwinget.exeに渡す
    return await ProcessRunner.RunWinGetAsync(args);
}
```

## 3. 実行フローのシーケンス図

### A. パススルーパターン（現在のメイン実装）

```mermaid
sequenceDiagram
    participant User
    participant Program
    participant CommandRouter
    participant ProcessRunner
    participant WinGetExe as winget.exe

    User->>Program: gistget search git
    Program->>CommandRouter: RouteAsync(["search", "git"])
    CommandRouter->>CommandRouter: 判定: search = パススルー対象
    CommandRouter->>ProcessRunner: RunWinGetAsync(["search", "git"])
    ProcessRunner->>WinGetExe: Process.Start("winget", "search git")
    WinGetExe-->>ProcessRunner: 標準出力/エラー出力
    ProcessRunner-->>CommandRouter: 終了コード
    CommandRouter-->>Program: 終了コード
    Program-->>User: WinGetの出力をそのまま表示
```

### B. COM APIパターン（将来の拡張用）

```mermaid
sequenceDiagram
    participant User
    participant Program
    participant CommandRouter
    participant InstallHandler as InstallCommandHandler
    participant WinGetClient
    participant ComAPI as COM API

    User->>Program: gistget install Git.Git
    Program->>CommandRouter: RouteAsync(["install", "Git.Git"])
    CommandRouter->>CommandRouter: 判定: install = COM API対象
    CommandRouter->>InstallHandler: ExecuteAsync(InstallOptions)
    InstallHandler->>WinGetClient: InstallPackageAsync(packageId)
    WinGetClient->>ComAPI: PackageManager.InstallPackageAsync()
    ComAPI-->>WinGetClient: InstallResult
    WinGetClient-->>InstallHandler: 結果
    InstallHandler->>InstallHandler: Gist同期（将来実装）
    InstallHandler-->>CommandRouter: 終了コード
    CommandRouter-->>Program: 終了コード
    Program-->>User: 処理結果表示
```

## 4. 現在の実装詳細

### コマンドルーティング戦略

```csharp
public class CommandRouter
{
    // 現在はすべてパススルー
    // 将来的にGist同期が必要なコマンドのみCOM API経由に切り替え
    
    public async Task<int> RouteAsync(string[] args)
    {
        // 現在の実装: すべてwinget.exeへパススルー
        return await ProcessRunner.RunWinGetAsync(args);
        
        // 将来の実装:
        // var command = args.FirstOrDefault();
        // if (IsGistSyncCommand(command))
        // {
        //     return await HandleWithComApi(args);
        // }
        // return await ProcessRunner.RunWinGetAsync(args);
    }
}
```

### 主要コンポーネント

#### Program.cs
- アプリケーションのエントリポイント
- コマンドライン引数をCommandRouterに渡す
- 終了コードを返す

#### CommandRouter
- コマンドの分類とルーティング
- 将来的にCOM APIとパススルーの振り分けを行う
- 現在はすべてパススルー

#### ProcessRunner
- winget.exeの実行を管理
- 標準出力/エラー出力の処理
- プロセスの終了コード取得

#### WinGetClient（部分実装）
- COM APIのラッパー
- PackageManagerの初期化と管理
- 将来のGist同期用基盤

## 5. 技術スタック

### 現在使用中
- **フレームワーク**: .NET 8
- **COM API**: Microsoft.Management.Deployment
- **プロセス管理**: System.Diagnostics.Process
- **非同期処理**: Task-based Async Pattern

### 将来追加予定
- **引数パーサー**: System.CommandLine
- **HTTP通信**: HttpClient（GitHub API用）
- **YAML処理**: YamlDotNet（Gist同期用）
- **暗号化**: Windows DPAPI（トークン保存用）

## 6. 実装の特徴

### シンプルな設計
- 最小限の抽象化
- 直接的なコード実装
- 段階的な機能追加

### 互換性重視
- WinGetの出力を完全に保持
- 既存のワークフローを破壊しない
- エラーメッセージもそのまま伝達

### 拡張性の確保
- COM API基盤は構築済み
- Gist同期機能の追加が容易
- テスト可能な構造

## 7. セキュリティ考慮事項

### 現在の実装
- プロセス実行時の引数エスケープ
- COM APIの安全な初期化
- リソースの適切な解放

### 将来の実装（Gist同期時）
- OAuth Device Flowによる認証
- トークンの暗号化保存
- 最小権限の原則

## 8. 既知の制限事項

### 現在の制限
- Windows専用（COM API依存）
- 管理者権限が必要な操作あり
- Gist同期機能未実装

### 対応予定
- エラーメッセージの改善
- 非管理者モードでの動作改善
- プログレス表示の実装

## 9. CI/CD環境での技術的課題

### A. Windows依存性の課題

```yaml
# COM APIのWindows依存性への対処
strategy:
  matrix:
    os: [windows-latest]  # Windows限定
    dotnet: ['8.0.x']
```

#### 課題と解決策

| コンポーネント | 課題 | 解決策 | 実装難易度 |
|---------------|------|--------|-----------|
| **COM API** | Windows限定 | 条件付きコンパイル | 低 |
| **DPAPI** | Windows限定 | 抽象化レイヤー | 中 |
| **winget.exe** | 実行ファイル依存 | モック実装 | 高 |
| **管理者権限** | CI環境で制限 | テスト分離 | 中 |

### B. ビルド専用CI/CD戦略

```csharp
// CI環境では実際のパッケージ操作を行わない
public static IServiceProvider ConfigureServices(bool isCI = false)
{
    var services = new ServiceCollection();
    
    if (isCI)
    {
        // CI環境：ビルド検証のみ、実際の操作は行わない
        services.AddSingleton<IPackageManager, NullPackageManager>();
        services.AddSingleton<IAuthProvider, NullAuthProvider>();
    }
    else
    {
        // ローカル環境：実際のCOM APIと認証を使用
        services.AddSingleton<IPackageManager, WinGetComManager>();
        services.AddSingleton<IAuthProvider, DeviceFlowAuthProvider>();
    }
    
    return services.BuildServiceProvider();
}

// CI専用の何もしないプロバイダー
public class NullPackageManager : IPackageManager
{
    public Task<int> InstallAsync(string packageId)
    {
        throw new NotSupportedException("CI環境ではパッケージ操作は実行されません");
    }
}

public class NullAuthProvider : IAuthProvider
{
    public Task<string?> GetTokenAsync()
    {
        throw new NotSupportedException("CI環境では認証は実行されません");
    }
}
```

### C. ビルドパイプライン（シンプル版）

```yaml
# ビルド検証のみ実施
name: Build
on: [push, pull_request]

jobs:
  build:
    strategy:
      matrix:
        os: [windows-latest, ubuntu-latest]
    runs-on: ${{ matrix.os }}
    
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      
      - name: Restore
        run: dotnet restore
      
      - name: Build
        run: dotnet build --configuration Release
      
      - name: Unit Tests Only
        run: dotnet test --filter "Category=Unit"
```

### D. ローカル開発重視のテスト戦略

```bash
# CI相当（ユニットテストのみ）
dotnet test --filter "Category=Unit"

# ローカル統合テスト（認証・COM API含む）
dotnet test --filter "Category=Local"

# 手動検証（実際のパッケージ操作）
dotnet test --filter "Category=Manual"

# 全テスト実行
dotnet test
```

### E. リリースパイプライン（シンプル版）

```yaml
# GitHub Releases への自動デプロイ（ビルドのみ）
name: Release
on:
  push:
    tags: ['v*']

jobs:
  release:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      
      - name: Build Release
        run: |
          dotnet publish -c Release -r win-x64 \
            --self-contained -p:PublishSingleFile=true
      
      - name: Create Release
        uses: softprops/action-gh-release@v1
        with:
          files: |
            src/NuitsJp.GistGet/bin/Release/net8.0/win-x64/publish/GistGet.exe
```

### F. 品質保証戦略

| 段階 | 実行環境 | 対象テスト | 自動化レベル |
|------|----------|-----------|-------------|
| **CI/CD** | GitHub Actions | ビルド + ユニットテスト | 完全自動 |
| **ローカル開発** | 開発PC | 統合テスト + COM API | 半自動 |
| **リリース前** | 手動 | 実際のパッケージ操作 | 手動 |

## 10. パフォーマンス最適化
  if: startsWith(github.ref, 'refs/tags/')
  uses: actions/create-release@v1
  with:
    tag_name: ${{ github.ref }}
    release_name: Release ${{ github.ref }}
    draft: false
    prerelease: false
    
- name: Upload Release Asset
  uses: actions/upload-release-asset@v1
  with:
    upload_url: ${{ steps.create_release.outputs.upload_url }}
    asset_path: ./bin/Release/net8.0/win-x64/publish/GistGet.exe
    asset_name: GistGet.exe
    asset_content_type: application/octet-stream
```

## 10. パフォーマンス最適化

### A. 起動時間の改善

```csharp
// ReadyToRun による起動高速化
// プロジェクトファイルに追加
<PropertyGroup>
  <PublishReadyToRun>true</PublishReadyToRun>
  <TieredCompilation>true</TieredCompilation>
</PropertyGroup>
```

### B. メモリ使用量の削減

```csharp
// 大量のパッケージリスト処理時のストリーミング
public async IAsyncEnumerable<Package> GetPackagesAsync()
{
    await foreach (var package in _comApi.GetPackagesAsync())
    {
        yield return package;
    }
}
```

## 11. 認証アーキテクチャ（シンプル戦略）

### A. 事前認証済み前提の戦略

```
┌─────────────────────────────────────────┐
│            AuthProvider                  │ 統一インターフェース
├─────────────────────────────────────────┤
│  既存のGitHubAuthService使用             │ すべての環境で共通
├─────────────────────────────────────────┤
│  • ローカル開発: gistget auth実行済み    │
│  • テスト実行: gistget auth実行済み      │
│  • CI/CD: 認証不要（ビルドのみ）         │
└─────────────────────────────────────────┘
```

### B. 実装アプローチ（シンプル版）

```csharp
// 既存のGitHubAuthServiceをそのまま使用
public class TestWithAuthentication
{
    private readonly IGitHubAuthService _authService;
    
    public TestWithAuthentication()
    {
        // DIコンテナから既存のサービスを取得
        _authService = serviceProvider.GetRequiredService<IGitHubAuthService>();
    }
    
    [Fact]
    [Trait("Category", "Local")]
    public async Task ExportCommand_ShouldWork()
    {
        // 事前認証チェック（失敗時はテストスキップ）
        if (!await _authService.IsAuthenticatedAsync())
        {
            throw new SkipException("認証が必要です。'gistget auth' を先に実行してください。");
        }
        
        // 実際のテスト実行
        var exportCommand = new ExportCommand(_authService);
        var result = await exportCommand.ExecuteAsync(new[] { "export" });
        
        result.Should().Be(0);
    }
}

// ユニットテスト（認証不要）
[Fact]
[Trait("Category", "Unit")]
public void ArgumentParsing_ShouldWork() 
{
    // 外部依存なし、認証不要
    var parser = new ArgumentParser();
    var result = parser.Parse(new[] { "export", "--output", "test.yaml" });
    
    result.Should().NotBeNull();
}
```

### C. テスト実行フロー（事前認証済み前提）

```bash
# 1. 事前認証（一度のみ実行）
gistget auth

# 2. 認証状態確認
gistget auth status
# 出力例: "認証済み: nuitsjp (Atsushi Nakamura)"

# 3. テスト実行
dotnet test --filter "Category=Unit"     # 認証不要
dotnet test --filter "Category=Local"    # 事前認証済み前提
dotnet test                              # 全テスト

# 認証が切れた場合のみ再実行
gistget auth
```

### D. 環境別の想定

| 環境 | 認証方式 | 実行前提 | テスト対象 |
|------|----------|----------|-----------|
| **ローカル開発** | Device Flow | `gistget auth`実行済み | Unit + Local |
| **CI/CD** | なし | 認証不要 | Unitのみ |
| **手動テスト** | Device Flow | `gistget auth`実行済み | 全テスト |

## 13. セキュリティ考慮事項（認証拡張）

### A. 認証方式別のセキュリティ

| 認証方式 | セキュリティレベル | リスク | 対策 |
|----------|------------------|-------|------|
| **Device Flow** | 高 | ブラウザ依存 | HTTPS必須、トークン暗号化 |
| **環境変数** | 中 | ログ漏洩 | CI専用、短期間使用 |
| **モック** | 低 | テスト専用 | 本番環境で無効化 |

### B. トークン管理

```csharp
// トークンの安全な取得
public class SecureTokenManager
{
    public async Task<string> GetSecureTokenAsync()
    {
        // 1. 環境変数チェック（CI/CD）
        var envToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
        if (!string.IsNullOrEmpty(envToken))
        {
            LogTokenSource("Environment Variable");
            return envToken;
        }
        
        // 2. 暗号化ファイルチェック（ローカル）
        var fileToken = await LoadEncryptedTokenAsync();
        if (!string.IsNullOrEmpty(fileToken))
        {
            LogTokenSource("Encrypted File");
            return fileToken;
        }
        
        throw new UnauthorizedAccessException("認証が必要です。'gistget auth' を実行してください。");
    }
    
    private void LogTokenSource(string source)
    {
        // セキュリティ: トークン自体はログに出力しない
        _logger.LogInformation("認証トークンを取得しました（ソース: {Source}）", source);
    }
}
```

## 14. 監視とログ（認証関連拡張）

### A. 構造化ログ

```csharp
// Serilog による構造化ログ
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File("logs/gistget-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

Log.Information("Installing package {PackageId} version {Version}", 
    packageId, version);
```

### B. テレメトリ（オプトイン）

```csharp
// Application Insights (オプトイン)
if (Settings.EnableTelemetry)
{
    services.AddApplicationInsightsTelemetry();
}
```

## 12. Gist管理アーキテクチャ（新規追加）

### A. Gist管理基盤の設計

```
┌─────────────────────────────────────────┐
│            GistManager                   │ Gist操作の統合管理
├─────────────────────────────────────────┤
│  - CRUD操作統合                          │
│  - パッケージ変換管理                    │
│  - エラーハンドリング                    │
├─────────────────────────────────────────┤
│         GistInfoStorage                  │ Gist設定の安全な保存
├─────────────────────────────────────────┤
│  - 暗号化保存（DPAPI）                   │
│  - 設定情報管理                          │
│  - 事前設定チェック                      │
├─────────────────────────────────────────┤
│       PackageYamlConverter               │ YAML ↔ C#オブジェクト変換
├─────────────────────────────────────────┤
│  - YamlDotNet活用                        │
│  - PackageCollection管理                 │
│  - バリデーション                        │
├─────────────────────────────────────────┤
│         GitHubGistClient                 │ GitHub Gist API
├─────────────────────────────────────────┤
│  - 既存のGitHubAuthService活用           │
│  - Gist取得・更新・作成                  │
│  - レート制限対応                        │
└─────────────────────────────────────────┘
```

### B. Gist設定データ形式

```csharp
// Gist設定情報（暗号化保存）
public class GistConfiguration
{
    public string GistId { get; set; } = string.Empty;      // Gist ID
    public string FileName { get; set; } = string.Empty;    // YAMLファイル名
    public DateTime CreatedAt { get; set; }                 // 設定日時
    public DateTime LastAccessAt { get; set; }              // 最終アクセス日時
}

// パッケージ定義（C#オブジェクト）
public class PackageCollection
{
    public List<PackageDefinition> Packages { get; set; } = new();
}

public class PackageDefinition
{
    public string Id { get; set; } = string.Empty;
    public string? Version { get; set; }            // バージョン固定（省略可）
    public bool Uninstall { get; set; } = false;    // アンインストールマーク
}

// 保存先: %APPDATA%\GistGet\gist.dat (DPAPI暗号化)
```

### C. プライベート関数の設計

```csharp
public class GistManager
{
    // Gist設定管理
    private async Task SaveGistConfigurationAsync(GistConfiguration gistConfig)
    {
        var json = JsonSerializer.Serialize(gistConfig);
        var encrypted = ProtectedData.Protect(
            Encoding.UTF8.GetBytes(json),
            null,
            DataProtectionScope.CurrentUser);
        
        var gistPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GistGet", "gist.dat");
        
        await File.WriteAllBytesAsync(gistPath, encrypted);
    }
    
    private async Task<GistConfiguration?> LoadGistConfigurationAsync()
    {
        var gistPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GistGet", "gist.dat");
        
        if (!File.Exists(gistPath)) return null;
        
        var encrypted = await File.ReadAllBytesAsync(gistPath);
        var decrypted = ProtectedData.Unprotect(
            encrypted,
            null,
            DataProtectionScope.CurrentUser);
        
        var json = Encoding.UTF8.GetString(decrypted);
        return JsonSerializer.Deserialize<GistConfiguration>(json);
    }
    
    // Gist操作（YAML ↔ C#オブジェクト）
    private async Task<PackageCollection?> GetGistPackagesAsync()
    {
        var gistConfig = await LoadGistConfigurationAsync();
        if (gistConfig == null)
        {
            throw new InvalidOperationException(
                "Gist設定が見つかりません。'gistget gist set'で設定してください。");
        }
        
        var yamlContent = await _gistClient.GetFileContentAsync(
            gistConfig.GistId, gistConfig.FileName);
        
        return _yamlConverter.FromYaml(yamlContent);
    }
    
    private async Task UpdateGistPackagesAsync(PackageCollection packages)
    {
        var gistConfig = await LoadGistConfigurationAsync();
        if (gistConfig == null)
        {
            throw new InvalidOperationException(
                "Gist設定が見つかりません。'gistget gist set'で設定してください。");
        }
        
        var yamlContent = _yamlConverter.ToYaml(packages);
        
        await _gistClient.UpdateFileContentAsync(
            gistConfig.GistId, gistConfig.FileName, yamlContent);
        
        // 最終アクセス時刻を更新
        gistConfig.LastAccessAt = DateTime.UtcNow;
        await SaveGistConfigurationAsync(gistConfig);
    }
}
```

### D. 事前設定前提のテスト戦略

```csharp
// Gist設定済み前提のテスト基盤
public class GistTestBase
{
    protected async Task SkipIfGistNotConfiguredAsync()
    {
        var gistManager = serviceProvider.GetRequiredService<IGistManager>();
        
        if (!await gistManager.IsConfiguredAsync())
        {
            throw new SkipException(
                "Gist設定が必要です。以下のコマンドで設定してください:\n" +
                "  gistget gist set --gist-id abc123 --file packages.yaml");
        }
    }
    
    protected async Task SkipIfNotFullyConfiguredAsync()
    {
        // 認証 + Gist設定の両方をチェック
        if (!await _authService.IsAuthenticatedAsync())
        {
            throw new SkipException("認証が必要です: gistget auth");
        }
        
        await SkipIfGistNotConfiguredAsync();
    }
}

// 実際のテスト例
[Fact]
[Trait("Category", "Local")]
public async Task SyncCommand_ShouldWork()
{
    await SkipIfNotFullyConfiguredAsync();
    
    var syncCommand = new SyncCommand(_gistManager);
    var result = await syncCommand.ExecuteAsync(new[] { "sync" });
    
    result.Should().Be(0);
}
```

### E. Gist管理コマンドの実装

```csharp
// Gist設定コマンドの実装（対話形式対応）
public class GistSetCommand
{
    public async Task<int> ExecuteAsync(string? gistId = null, string? fileName = null)
    {
        // 認証チェック
        if (!await _authService.IsAuthenticatedAsync())
        {
            Console.WriteLine("エラー: 認証が必要です。'gistget auth'を先に実行してください。");
            return 1;
        }
        
        // 引数が未指定の場合は対話形式
        if (string.IsNullOrEmpty(gistId))
        {
            Console.WriteLine("=== GistGet Gist設定 ===");
            Console.WriteLine();
            Console.WriteLine("まず、GitHubでGistを作成してください：");
            Console.WriteLine("1. https://gist.github.com/ にアクセス");
            Console.WriteLine("2. ファイル名に 'packages.yaml' を入力");
            Console.WriteLine("3. 以下の内容をコピー＆ペースト：");
            Console.WriteLine();
            Console.WriteLine("Packages:");
            Console.WriteLine("  - Id: Git.Git");
            Console.WriteLine("  - Id: Microsoft.VisualStudioCode");
            Console.WriteLine();
            Console.WriteLine("4. 'Create public gist' をクリック");
            Console.WriteLine("5. 作成後のURLからGist IDをコピー");
            Console.WriteLine("   例: https://gist.github.com/username/abc123456789");
            Console.WriteLine("       ↑この 'abc123456789' がGist ID");
            Console.WriteLine();
            
            // Gist ID入力
            Console.Write("Gist IDを入力してください: ");
            gistId = Console.ReadLine()?.Trim();
            
            if (string.IsNullOrEmpty(gistId))
            {
                Console.WriteLine("エラー: Gist IDは必須です。");
                return 1;
            }
        }
        
        // ファイル名のデフォルト設定
        if (string.IsNullOrEmpty(fileName))
        {
            Console.Write("ファイル名を入力してください [packages.yaml]: ");
            var input = Console.ReadLine()?.Trim();
            fileName = string.IsNullOrEmpty(input) ? "packages.yaml" : input;
        }
        
        // Gist ID形式チェック
        if (!IsValidGistId(gistId))
        {
            Console.WriteLine($"エラー: 無効なGist ID形式です: {gistId}");
            Console.WriteLine("Gist IDは32文字の英数字である必要があります。");
            return 1;
        }
        
        // Gist存在チェック
        Console.WriteLine($"Gist '{gistId}' の存在を確認中...");
        if (!await _gistClient.ExistsAsync(gistId))
        {
            Console.WriteLine($"エラー: Gist '{gistId}' が見つかりません。");
            Console.WriteLine("Gist IDが正しいか、Gistが公開設定になっているか確認してください。");
            return 1;
        }
        
        // 設定保存
        var gistConfig = new GistConfiguration
        {
            GistId = gistId,
            FileName = fileName,
            CreatedAt = DateTime.UtcNow,
            LastAccessAt = DateTime.UtcNow
        };
        
        await _gistManager.SaveGistConfigurationAsync(gistConfig);
        
        Console.WriteLine();
        Console.WriteLine("✅ Gist設定を保存しました:");
        Console.WriteLine($"  Gist ID: {gistId}");
        Console.WriteLine($"  ファイル名: {fileName}");
        Console.WriteLine();
        Console.WriteLine("次のコマンドで設定を確認できます:");
        Console.WriteLine("  gistget gist status");
        Console.WriteLine("  gistget gist show");
        
        return 0;
    }
    
    private static bool IsValidGistId(string gistId)
    {
        // Gist IDは通常32文字の16進数
        return gistId.Length == 32 && 
               gistId.All(c => char.IsDigit(c) || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'));
    }
}

// Gist状態表示コマンド
public class GistStatusCommand
{
    public async Task<int> ExecuteAsync()
    {
        var gistConfig = await _gistManager.LoadGistConfigurationAsync();
        
        if (gistConfig == null)
        {
            Console.WriteLine("❌ Gist設定が見つかりません。");
            Console.WriteLine();
            Console.WriteLine("以下のコマンドで設定してください:");
            Console.WriteLine("  gistget gist set");
            return 1;
        }
        
        Console.WriteLine("✅ Gist設定済み:");
        Console.WriteLine($"  Gist ID: {gistConfig.GistId}");
        Console.WriteLine($"  ファイル名: {gistConfig.FileName}");
        Console.WriteLine($"  設定日時: {gistConfig.CreatedAt:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"  最終アクセス: {gistConfig.LastAccessAt:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine();
        Console.WriteLine($"Gist URL: https://gist.github.com/{gistConfig.GistId}");
        
        return 0;
    }
}
```