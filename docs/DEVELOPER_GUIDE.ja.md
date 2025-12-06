# GistGet 開発者ガイド

本ドキュメントは、GistGetプロジェクトに貢献する開発者向けのガイドです。開発環境のセットアップから、コーディング規約、テスト方法まで、開発に必要な情報を提供します。

## 目次

- [開発環境のセットアップ](#開発環境のセットアップ)
- [プロジェクト構造](#プロジェクト構造)
- [ビルドとテスト](#ビルドとテスト)
- [開発ワークフロー](#開発ワークフロー)
- [コーディング規約](#コーディング規約)
- [テスト戦略](#テスト戦略)
- [デバッグ](#デバッグ)
- [トラブルシューティング](#トラブルシューティング)

## 開発環境のセットアップ

### 必要な環境

- **OS**: Windows 10/11 (Windows 10.0.26100.0 以降)
- **.NET SDK**: .NET 8.0 以降
- **Windows SDK**: 10.0.26100.0 以降（UAP Platform を含む）
- **IDE**: Visual Studio 2022 または Visual Studio Code (推奨)
- **Windows Package Manager**: winget (Windows App Installer 経由)
- **PowerShell**: 5.1 以降 (スクリプト実行用)

### Windows SDK のインストール

WinGet COM API を使用するため、Windows SDK の **UAP Platform** コンポーネントが必要です。

#### Visual Studio Installer でのインストール

1. Visual Studio Installer を起動
2. 「変更」をクリック
3. 「個別のコンポーネント」タブを選択
4. 以下のコンポーネントを選択してインストール：
   - **Windows 11 SDK (10.0.26100.0)**
   - **Windows ユニバーサル C ランタイム**

#### 確認方法

```powershell
# Platform.xml が存在することを確認
Test-Path "C:\Program Files (x86)\Windows Kits\10\Platforms\UAP\10.0.26100.0\Platform.xml"
```

### リポジトリのクローン

```powershell
git clone https://github.com/nuitsjp/GistGet.git
cd GistGet
```

### 依存関係の復元

```powershell
dotnet restore GistGet.sln
```

### ビルド

```powershell
# Debug ビルド
dotnet build GistGet.sln -c Debug

# Release ビルド
dotnet build GistGet.sln -c Release
```

## プロジェクト構造

GistGetは、レイヤー化アーキテクチャを採用しています。詳細は[DESIGN.ja.md](file:///d:/GistGet/docs/DESIGN.ja.md)を参照してください。

```
GistGet/
├── src/
│   ├── GistGet/                    # メインプロジェクト
│   │   ├── App/                    # アプリケーションエントリーポイント
│   │   │   └── Program.cs          # エントリーポイント
│   │   ├── Com/                    # WinGet COM API 連携
│   │   │   └── WinGetService.cs    # WinGet サービス実装
│   │   ├── IWinGetService.cs       # WinGet サービスインターフェース
│   │   ├── WinGetPackage.cs        # パッケージモデル
│   │   ├── PackageId.cs            # パッケージID値オブジェクト
│   │   └── Version.cs              # バージョン値オブジェクト
│   └── GistGet.Test/               # テストプロジェクト
│       └── Com/                    # COM API テスト
│           └── WinGetServiceTests.cs
├── scripts/                        # 開発用スクリプト
│   ├── Run-Tests.ps1               # テスト実行スクリプト
│   ├── Run-AuthLogin.ps1           # 認証テストスクリプト
│   └── Collect-Metrics.ps1         # メトリクス収集スクリプト
├── docs/                           # ドキュメント
│   ├── DESIGN.ja.md                # システム設計書
│   ├── DEVELOPER_GUIDE.ja.md       # 開発者ガイド（本ドキュメント）
│   ├── SPEC.ja.md                  # 仕様書
│   └── YAML_SPEC.ja.md             # YAML仕様書
└── external/                       # 外部参照
    └── winget-cli/                 # WinGet CLI リポジトリ（サンプル参照用）
```

### 主要なレイヤー

#### Presentation層 (`Presentation/`)
- CLIコマンドの定義とパース
- `System.CommandLine`を使用したコマンドライン処理
- ユーザー入力の検証とエラーハンドリング

#### Application層 (`Application/Services/`)
- ビジネスロジックの実装
- `AuthService`: GitHub認証 (Device Flow)
- `GistService`: Gist操作 (取得、更新)
- `PackageService`: パッケージ同期のオーケストレーション

#### COM 層 (`Com/`)
- WinGet COM API との統合
- `WinGetService`: WinGet COM API を使用したパッケージ検索・情報取得

### WinGet COM API について

GistGet は、WinGet COM API を使用してパッケージ情報を取得します。

#### 主要な依存パッケージ

```xml
<PackageReference Include="Microsoft.WindowsPackageManager.ComInterop" Version="1.10.340" />
<PackageReference Include="Microsoft.Windows.CsWinRT" Version="2.2.0" />
```

#### プロジェクト設定

WinGet COM API を使用するには、以下の設定が必要です：

```xml
<PropertyGroup>
  <RuntimeIdentifier>win-x64</RuntimeIdentifier>
  <MicrosoftManagementDeployment-FactoryLinkage>static</MicrosoftManagementDeployment-FactoryLinkage>
</PropertyGroup>
```

- **RuntimeIdentifier**: WinGet COM API は x64 プラットフォームでのみ動作
- **MicrosoftManagementDeployment-FactoryLinkage**: 
  - `static` (推奨): WinGet のプロセス内で COM オブジェクトを作成
  - `embedded`: テスト用途

#### 参考資料

- 実装サンプル: `external/winget-cli/samples/WinGetClientSample/`
- GitHub: [microsoft/winget-cli](https://github.com/microsoft/winget-cli)

#### 既知の問題と回避策

##### IsUpdateAvailable が正しく動作しない

WinGet COM API の `IsUpdateAvailable` プロパティは、更新が利用可能な場合でも `false` を返すことがあります。

**回避策**: `InstalledVersion` と `AvailableVersions[0]` を比較して更新の有無を判定：

```csharp
var installedVersion = package.InstalledVersion.Version;
var availableVersion = package.AvailableVersions.Count > 0 
    ? package.AvailableVersions[0].Version 
    : installedVersion;

bool isUpdateAvailable = installedVersion != availableVersion;

## ビルドとテスト

### ビルドコマンド

```powershell
# ソリューション全体をビルド
dotnet build GistGet.sln -c Debug

# 特定のプロジェクトをビルド
dotnet build src/GistGet/GistGet.csproj -c Debug
dotnet build src/GistGet.Test/GistGet.Test.csproj -c Debug
```

### テストの実行

#### 基本的なテスト実行

```powershell
# すべてのテストを実行
dotnet test src/GistGet.Test/GistGet.Test.csproj -c Debug

# 詳細な出力
dotnet test src/GistGet.Test/GistGet.Test.csproj -c Debug --logger "console;verbosity=detailed"
```

#### カバレッジ付きテスト実行

```powershell
# カバレッジを収集してテスト実行
dotnet test src/GistGet.Test/GistGet.Test.csproj -c Debug `
  --collect:"XPlat Code Coverage" `
  --results-directory TestResults

# 便利スクリプトを使用 (推奨)
.\scripts\Run-Tests.ps1 -Configuration Debug -CollectCoverage $true
```

カバレッジレポートは `TestResults/` ディレクトリに `coverage.cobertura.xml` として出力されます。

#### 特定のテストの実行

```powershell
# 特定のテストクラスを実行
dotnet test --filter "FullyQualifiedName~PackageServiceTests"

# 特定のテストメソッドを実行
dotnet test --filter "FullyQualifiedName~PackageServiceTests.SyncAsync_WithNewPackage_InstallsPackage"
```

### CLIの実行

開発中のCLIを実行するには:

```powershell
# dotnet run を使用
dotnet run --project src/GistGet/GistGet.csproj -- <command>

# 例: 認証
dotnet run --project src/GistGet/GistGet.csproj -- auth login

# 例: 同期
dotnet run --project src/GistGet/GistGet.csproj -- sync

# 例: ヘルプ
dotnet run --project src/GistGet/GistGet.csproj -- --help
```

または、ビルド後の実行ファイルを直接実行:

```powershell
.\src\GistGet\bin\Debug\net8.0-windows10.0.26100.0\GistGet.exe <command>
```

### 開発用スクリプト

#### Run-Tests.ps1

テストとカバレッジを一度に実行する便利スクリプト:

```powershell
# デフォルト (Debug, カバレッジあり)
.\scripts\Run-Tests.ps1

# Releaseビルドでテスト
.\scripts\Run-Tests.ps1 -Configuration Release

# カバレッジなしでテスト
.\scripts\Run-Tests.ps1 -CollectCoverage $false
```

#### Run-AuthLogin.ps1

認証フローをテストするスクリプト:

```powershell
.\scripts\Run-AuthLogin.ps1
```

#### Collect-Metrics.ps1

コードメトリクスを収集するスクリプト:

```powershell
.\scripts\Collect-Metrics.ps1
```

メトリクスレポートは `metrics-report.txt` に出力されます。

## 開発ワークフロー

### TDD (Test-Driven Development)

GistGetプロジェクトでは、**t-wadaスタイルのTDD**を厳格に遵守しています。

#### RED-GREEN-REFACTORサイクル

1. **RED**: 失敗するテストを書く
   ```csharp
   [Fact]
   public async Task SyncAsync_WithNewPackage_InstallsPackage()
   {
       // Arrange
       var mockExecutor = new Mock<IWinGetExecutor>();
       var service = new PackageService(mockExecutor.Object, ...);
       
       // Act
       var result = await service.SyncAsync(...);
       
       // Assert
       mockExecutor.Verify(x => x.InstallPackageAsync(It.IsAny<GistGetPackage>()), Times.Once);
   }
   ```

2. **GREEN**: テストを通す最小限の実装
   ```csharp
   public async Task<SyncResult> SyncAsync(...)
   {
       // 最小限の実装
       await _executor.InstallPackageAsync(package);
       return new SyncResult();
   }
   ```

3. **REFACTOR**: コードをリファクタリング
   ```csharp
   public async Task<SyncResult> SyncAsync(...)
   {
       // より良い実装にリファクタリング
       var packagesToInstall = CalculatePackagesToInstall(...);
       foreach (var package in packagesToInstall)
       {
           await _executor.InstallPackageAsync(package);
       }
       return new SyncResult { InstalledCount = packagesToInstall.Count };
   }
   ```

### ブランチ戦略

- `main`: 安定版ブランチ
- `feature/*`: 新機能開発用ブランチ
- `fix/*`: バグ修正用ブランチ
- `refactor/*`: リファクタリング用ブランチ

### コミットメッセージ

Conventional Commitsに従ってください:

```
<type>: <subject>

<body>

<footer>
```

#### Type

- `feat`: 新機能
- `fix`: バグ修正
- `docs`: ドキュメントのみの変更
- `style`: コードの意味に影響しない変更 (フォーマットなど)
- `refactor`: バグ修正や機能追加ではないコード変更
- `test`: テストの追加や修正
- `chore`: ビルドプロセスやツールの変更

#### 例

```
feat: add version pinning support for packages

- Add Version property to GistGetPackage model
- Implement version comparison in PackageService
- Add tests for version pinning logic

Closes #123
```

## コーディング規約

### C# スタイルガイド

- **言語バージョン**: C# 12
- **ターゲットフレームワーク**: `net8.0-windows10.0.26100.0`
- **インデント**: 4スペース
- **エンコーディング**: UTF-8
- **Nullable**: 有効 (`<Nullable>enable</Nullable>`)

### 命名規則

```csharp
// PascalCase: 型、メソッド、プロパティ
public class PackageService
{
    public async Task SyncAsync() { }
    public string PackageId { get; set; }
}

// camelCase: パラメータ、ローカル変数
public void ProcessPackage(string packageId)
{
    var installedPackages = GetInstalledPackages();
}

// _camelCase: プライベートreadonly フィールド
private readonly IWinGetExecutor _executor;
private readonly ILogger _logger;
```

### コーディングスタイル

#### var の使用

型が明らかな場合は `var` を使用:

```csharp
// Good
var packages = new Dictionary<string, GistGetPackage>();
var service = new PackageService(executor, repository);

// Avoid
Dictionary<string, GistGetPackage> packages = new Dictionary<string, GistGetPackage>();
```

#### readonly フィールド

依存関係は `readonly` フィールドとして宣言:

```csharp
public class PackageService
{
    private readonly IWinGetExecutor _executor;
    private readonly IWinGetRepository _repository;
    
    public PackageService(IWinGetExecutor executor, IWinGetRepository repository)
    {
        _executor = executor;
        _repository = repository;
    }
}
```

#### 非同期メソッド

非同期メソッドには `Async` サフィックスを付ける:

```csharp
public async Task<SyncResult> SyncAsync()
{
    var packages = await GetPackagesAsync();
    return await ProcessPackagesAsync(packages);
}
```

#### ガード句

引数検証にはガード句を使用:

```csharp
public void ProcessPackage(string packageId)
{
    if (string.IsNullOrWhiteSpace(packageId))
        throw new ArgumentException("Package ID cannot be null or empty.", nameof(packageId));
    
    // メインロジック
}
```

### 依存性注入

静的シングルトンを避け、コンストラクタインジェクションを使用:

```csharp
// Good
public class PackageService : IPackageService
{
    private readonly IWinGetExecutor _executor;
    
    public PackageService(IWinGetExecutor executor)
    {
        _executor = executor;
    }
}

// Avoid
public class PackageService
{
    private static WinGetExecutor _executor = new WinGetExecutor();
}
```

### インターフェース設計

サービスと実装を分離:

```csharp
// インターフェース
public interface IPackageService
{
    Task<SyncResult> SyncAsync(
        Dictionary<string, GistGetPackage> gistPackages,
        Dictionary<string, GistGetPackage> localPackages);
}

// 実装
public class PackageService : IPackageService
{
    // 実装
}
```

## テスト戦略

### テストフレームワーク

- **xUnit**: テストフレームワーク
- **Moq**: モッキングライブラリ

### テストの命名規則

```
Method_Scenario_ExpectedResult
```

例:
```csharp
[Fact]
public async Task SyncAsync_WithNewPackage_InstallsPackage()
{
    // テスト実装
}

[Fact]
public async Task SyncAsync_WithUninstallFlag_UninstallsPackage()
{
    // テスト実装
}
```

### テストの構造 (AAA パターン)

```csharp
[Fact]
public async Task SyncAsync_WithNewPackage_InstallsPackage()
{
    // Arrange (準備)
    var mockExecutor = new Mock<IWinGetExecutor>();
    var mockRepository = new Mock<IWinGetRepository>();
    var service = new PackageService(mockExecutor.Object, mockRepository.Object);
    
    var gistPackages = new Dictionary<string, GistGetPackage>
    {
        ["Microsoft.PowerToys"] = new GistGetPackage { Id = "Microsoft.PowerToys" }
    };
    var localPackages = new Dictionary<string, GistGetPackage>();
    
    // Act (実行)
    var result = await service.SyncAsync(gistPackages, localPackages);
    
    // Assert (検証)
    mockExecutor.Verify(
        x => x.InstallPackageAsync(It.Is<GistGetPackage>(p => p.Id == "Microsoft.PowerToys")),
        Times.Once);
}
```

### Theory テスト

複数のシナリオをテストする場合:

```csharp
[Theory]
[InlineData("1.0.0", "1.0.0", true)]
[InlineData("1.0.0", "1.0.1", false)]
[InlineData("2.0.0", "1.9.9", false)]
public void VersionEquals_VariousVersions_ReturnsExpectedResult(
    string version1, string version2, bool expected)
{
    // Arrange & Act
    var result = VersionHelper.Equals(version1, version2);
    
    // Assert
    Assert.Equal(expected, result);
}
```

### モッキング

依存関係をモック化してユニットテストを分離:

```csharp
var mockExecutor = new Mock<IWinGetExecutor>();
mockExecutor
    .Setup(x => x.InstallPackageAsync(It.IsAny<GistGetPackage>()))
    .ReturnsAsync(true);

var service = new PackageService(mockExecutor.Object, ...);
```

### カバレッジ目標

- **全体**: 95%以上
- **新規コード**: 100%

カバレッジレポートは `TestResults/coverage.cobertura.xml` で確認できます。

### 統合テスト

統合テストは、実際の外部システム（GitHub API、WinGet APIなど）と連携して動作を検証します。

#### 統合テストの種類

GistGetプロジェクトでは、以下の統合テストを提供しています：

- **Gist操作の統合テスト** (`Integration/GistServiceIntegrationTests.cs`): 実際のGitHub APIを使用してGist操作を検証

#### 前提条件

> [!IMPORTANT]
> 統合テストを実行する前に、必ず認証を完了してください。

```powershell
# GitHub認証を実行
.\scripts\Run-AuthLogin.ps1
```

認証情報はWindows Credential Managerに保存され、統合テスト実行時に使用されます。

#### 統合テストの実行

```powershell
# 統合テストのみを実行
dotnet test src/GistGet.Test/GistGet.Test.csproj -c Debug --filter "FullyQualifiedName~Integration"

# 特定の統合テストクラスを実行
dotnet test --filter "FullyQualifiedName~GistServiceIntegrationTests"
```

#### 統合テストの特徴

- **実際のAPI呼び出し**: モックではなく、実際のGitHub APIを使用
- **認証が必要**: 事前に `Run-AuthLogin.ps1` で認証が必要
- **テスト順序**: テストメソッド名の番号プレフィックス（Test1_, Test2_など）で実行順序を制御
- **状態の共有**: `IClassFixture` を使用してテスト間で状態を共有

#### 統合テストの例

```csharp
[Collection("GistIntegration")]
public class GistServiceIntegrationTests : IClassFixture<GistIntegrationTestFixture>
{
    private readonly IGistService _gistService;

    public GistServiceIntegrationTests(GistIntegrationTestFixture fixture)
    {
        var credentialService = new CredentialService();
        var authService = new AuthService(credentialService);
        _gistService = new GistService(authService);
    }

    [Fact]
    public async Task Test1_SavePackagesAsync_CreatesNewGist_WhenNoGistExists()
    {
        // Arrange
        var testPackages = new Dictionary<string, GistGetPackage>
        {
            { "Test.Package", new GistGetPackage { Id = "Test.Package" } }
        };

        // Act
        await _gistService.SavePackagesAsync(testPackages);

        // Assert
        var retrievedPackages = await _gistService.GetPackagesAsync();
        Assert.NotEmpty(retrievedPackages);
    }
}
```

#### ユニットテストとの使い分け

| 観点 | ユニットテスト | 統合テスト |
|------|--------------|-----------|
| **目的** | 個別のロジックを検証 | 外部システムとの連携を検証 |
| **実行速度** | 高速 | 低速 |
| **依存関係** | モックを使用 | 実際のシステムを使用 |
| **実行頻度** | 毎回のビルド | 重要な変更時のみ |
| **認証** | 不要 | 必要（GitHub認証など） |

**推奨事項**:
- 開発中は主にユニットテストを使用
- 統合テストは以下の場合に実行：
  - API連携ロジックの変更時
  - リリース前の最終確認
  - CI/CDパイプラインでの定期実行

#### トラブルシューティング

##### エラー: "Authentication required"

**原因**: GitHub認証が完了していない

**解決策**:
```powershell
.\scripts\Run-AuthLogin.ps1
```

##### エラー: "Rate limit exceeded"

**原因**: GitHub APIのレート制限に達した

**解決策**:
- しばらく待ってから再実行
- 統合テストの実行頻度を減らす
- 開発中はユニットテストを使用

##### エラー: ネットワーク接続エラー

**原因**: インターネット接続の問題

**解決策**:
- ネットワーク接続を確認
- プロキシ設定を確認
- ファイアウォール設定を確認

##### テスト後のクリーンアップ

統合テストは実行後に作成したGistを手動で削除する必要がある場合があります：

1. https://gist.github.com にアクセス
2. "GistGet Packages" という説明のGistを探す
3. 不要なテストGistを削除


## デバッグ

### Visual Studio でのデバッグ

1. `GistGet.csproj` をスタートアッププロジェクトに設定
2. プロジェクトのプロパティ → デバッグ → コマンドライン引数を設定
   ```
   auth login
   ```
3. F5 でデバッグ開始

### Visual Studio Code でのデバッグ

`.vscode/launch.json`:

```json
{
    "version": "0.2.0",
    "configurations": [
        {
            "name": "GistGet (auth login)",
            "type": "coreclr",
            "request": "launch",
            "preLaunchTask": "build",
            "program": "${workspaceFolder}/src/GistGet/bin/Debug/net8.0-windows10.0.26100.0/GistGet.exe",
            "args": ["auth", "login"],
            "cwd": "${workspaceFolder}",
            "console": "integratedTerminal",
            "stopAtEntry": false
        },
        {
            "name": "GistGet (sync)",
            "type": "coreclr",
            "request": "launch",
            "preLaunchTask": "build",
            "program": "${workspaceFolder}/src/GistGet/bin/Debug/net8.0-windows10.0.26100.0/GistGet.exe",
            "args": ["sync"],
            "cwd": "${workspaceFolder}",
            "console": "integratedTerminal",
            "stopAtEntry": false
        }
    ]
}
```

### ログ出力

開発中は `Console.WriteLine` または適切なロギングライブラリを使用:

```csharp
Console.WriteLine($"Installing package: {package.Id}");
```

## トラブルシューティング

### ビルドエラー

#### エラー: "Could not find the Windows SDK in the registry" または "Could not read the Windows SDK's Platform.xml"

**原因**: Windows SDK の UAP Platform コンポーネントがインストールされていない

**解決策**:
1. Visual Studio Installer を起動
2. 「個別のコンポーネント」で **Windows 11 SDK (10.0.26100.0)** をインストール
3. 以下のパスにファイルが存在することを確認：
   ```powershell
   Test-Path "C:\Program Files (x86)\Windows Kits\10\Platforms\UAP\10.0.26100.0\Platform.xml"
   ```

#### エラー: "The type or namespace name 'Windows' could not be found"

**原因**: Windows SDK が正しくインストールされていない

**解決策**:
```powershell
# Visual Studio Installer で Windows 10 SDK (10.0.26100.0) をインストール
```

#### エラー: "Could not load file or assembly 'Microsoft.WindowsPackageManager.ComInterop'"

**原因**: WinGet COM API が利用できない

**解決策**:
```powershell
# Windows App Installer を最新版に更新
winget --version
```

### テストエラー

#### エラー: テストが実行されない

**原因**: テストプロジェクトがビルドされていない

**解決策**:
```powershell
dotnet build src/GistGet.Test/GistGet.Test.csproj
dotnet test src/GistGet.Test/GistGet.Test.csproj
```

### 実行時エラー

#### エラー: "Access to the path is denied"

**原因**: Windows Credential Manager へのアクセス権限がない

**解決策**: 管理者権限で実行するか、ユーザーアカウントの権限を確認

#### エラー: "winget.exe not found"

**原因**: winget が PATH に含まれていない

**解決策**:
```powershell
# winget のパスを確認
where.exe winget

# PATH に追加 (通常は不要)
$env:PATH += ";C:\Program Files\WindowsApps\Microsoft.DesktopAppInstaller_*_x64__8wekyb3d8bbwe"
```

## 参考資料

- [DESIGN.ja.md](file:///d:/GistGet/docs/DESIGN.ja.md) - システム設計書
- [SPEC.ja.md](file:///d:/GistGet/docs/SPEC.ja.md) - 仕様書
- [YAML_SPEC.ja.md](file:///d:/GistGet/docs/YAML_SPEC.ja.md) - YAML仕様書
- [README.ja.md](file:///d:/GistGet/README.ja.md) - プロジェクト概要

## 質問・サポート

- **Issues**: [GitHub Issues](https://github.com/nuitsjp/GistGet/issues)
- **Discussions**: [GitHub Discussions](https://github.com/nuitsjp/GistGet/discussions)
