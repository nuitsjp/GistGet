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
│        WinGet Client Abstraction        │ IWinGetClient
├─────────────────────────────────────────┤
│    COM API + CLI Fallback Layer        │ WinGetComClient
├─────────────────────────────────────────┤
│        Microsoft WinGet API             │ COM Interop + winget.exe
└─────────────────────────────────────────┘
```

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

### 4. WinGetクライアント抽象化レイヤー

**責務**: WinGet操作の統一インターフェースとフォールバック機構

- **IWinGetClient**: 公開インターフェース
- **WinGetComClient**: COM API + CLI フォールバック実装

### 5. データモデル

**責務**: WinGet操作で使用するデータ構造とオプション

- **パッケージモデル**: WinGetPackage, SearchResult等
- **オプションモデル**: InstallOptions, ListOptions等
- **結果モデル**: OperationResult, ErrorDetails等

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
public class WinGetComClient : IWinGetClient, IDisposable
{
    // COM API優先、CLI フォールバック
    public async Task<OperationResult> InstallPackageAsync(InstallOptions options)
    
    // 初期化とCOM API可用性チェック
    public async Task InitializeAsync()
}
```

## クラス構造図

### コアアーキテクチャ概念図

```mermaid
classDiagram
    class Program {
        +Main(string[] args) Task~int~
        -CreateHost() Host
    }
    
    class IWinGetArgumentParser {
        <<interface>>
        +BuildRootCommand() Command
        +ValidateArguments(ParseResult result) ValidationResult
    }
    
    class WinGetArgumentParser {
        -_validationEngine: ValidationEngine
        +BuildRootCommand() Command
        +BuildInstallCommand() Command
        +BuildListCommand() Command
        +BuildUpgradeCommand() Command
    }
    
    class BaseCommandHandler {
        <<abstract>>
        #ServiceProvider: IServiceProvider
        +SetServiceProvider(IServiceProvider provider) void
        +ExecuteAsync()* Task~int~
    }
    
    class InstallCommandHandler {
        +ExecuteAsync(InstallOptions options) Task~int~
    }
    
    class ListCommandHandler {
        +ExecuteAsync(ListOptions options) Task~int~
    }
    
    class IWinGetClient {
        <<interface>>
        +InitializeAsync() Task
        +InstallPackageAsync(InstallOptions options) Task~OperationResult~
        +ListInstalledPackagesAsync(ListOptions options) Task~IEnumerable~WinGetPackage~~
        +UpgradePackageAsync(UpgradeOptions options) Task~OperationResult~
        +Dispose() void
    }
    
    class WinGetComClient {
        -_packageManager: PackageManager
        -_logger: ILogger
        -_processRunner: IProcessRunner
        -_isInitialized: bool
        -_comApiAvailable: bool
        +InitializeAsync() Task
        +InstallPackageAsync(InstallOptions options) Task~OperationResult~
        +InstallPackageComAsync(InstallOptions options) Task~OperationResult~
        +InstallPackageCliAsync(InstallOptions options) Task~OperationResult~
        +Dispose() void
    }
    
    class IProcessRunner {
        <<interface>>
        +RunAsync(string fileName, string arguments) Task~ProcessResult~
    }
    
    class DefaultProcessRunner {
        +RunAsync(string fileName, string arguments) Task~ProcessResult~
    }

    Program --> IWinGetArgumentParser : uses
    WinGetArgumentParser ..|> IWinGetArgumentParser : implements
    Program --> BaseCommandHandler : creates
    BaseCommandHandler <|-- InstallCommandHandler : extends
    BaseCommandHandler <|-- ListCommandHandler : extends
    BaseCommandHandler --> IWinGetClient : uses
    WinGetComClient ..|> IWinGetClient : implements
    WinGetComClient --> IProcessRunner : uses
    DefaultProcessRunner ..|> IProcessRunner : implements
```

### データモデル構造図

```mermaid
classDiagram
    class WinGetPackage {
        +Id: string
        +Name: string
        +Version: string
        +Source: string
        +Publisher: string
        +Description: string
    }
    
    class InstallOptions {
        +Id: string
        +Query: string
        +Version: string
        +Source: string
        +Scope: InstallationScope
        +Architecture: Architecture
        +Silent: bool
        +Interactive: bool
        +Location: string
    }
    
    class ListOptions {
        +Id: string
        +Query: string
        +Source: string
        +Scope: InstallationScope
        +UpgradeAvailable: bool
        +Count: int
        +IncludeUnknown: bool
        +IncludePinned: bool
    }
    
    class OperationResult {
        +IsSuccess: bool
        +ErrorCode: int
        +ErrorMessage: string
        +ErrorDetails: ErrorDetails
        +Data: object
    }
    
    class ErrorDetails {
        +Source: string
        +Category: string
        +Details: string
        +Exception: Exception
    }
    
    class ProcessResult {
        +ExitCode: int
        +StandardOutput: string
        +StandardError: string
        +ExecutionTime: TimeSpan
    }

    OperationResult --> ErrorDetails : contains
    IWinGetClient --> WinGetPackage : returns
    IWinGetClient --> InstallOptions : accepts
    IWinGetClient --> ListOptions : accepts
    IWinGetClient --> OperationResult : returns
    IProcessRunner --> ProcessResult : returns
```

## 処理シーケンス図

### パッケージインストール処理フロー

```mermaid
sequenceDiagram
    participant User
    participant Program as Program.cs
    participant Parser as WinGetArgumentParser
    participant Handler as InstallCommandHandler
    participant Client as WinGetComClient
    participant COM as COM API
    participant CLI as winget.exe

    User->>Program: install --id Git.Git
    Program->>Parser: BuildRootCommand()
    Parser-->>Program: RootCommand
    Program->>Parser: Parse(args)
    Parser->>Handler: CreateInstallHandler()
    Handler->>Client: GetService<IWinGetClient>()
    
    Note over Handler,Client: 依存性注入によるクライアント取得
    
    Handler->>Client: InstallPackageAsync(options)
    Client->>Client: EnsureInitializedAsync()
    
    alt COM API利用可能
        Client->>COM: InstallPackageAsync(options)
        COM-->>Client: OperationResult (Success)
        Client-->>Handler: OperationResult
    else COM API失敗
        Note over Client: COM API失敗、CLIフォールバックに切り替え
        Client->>CLI: ExecuteWingetAsync("install", args)
        CLI-->>Client: ProcessResult
        Client->>Client: ParseCliOutput(result)
        Client-->>Handler: OperationResult
    end
    
    Handler-->>Program: ExitCode (0=Success)
    Program-->>User: インストール完了
```

### アプリケーション初期化フロー

```mermaid
sequenceDiagram
    participant User
    participant Program as Program.cs
    participant Host as .NET Generic Host
    participant DI as DI Container
    participant Parser as WinGetArgumentParser
    participant Client as WinGetComClient

    User->>Program: アプリケーション起動
    Program->>Host: CreateDefaultBuilder()
    Program->>DI: ConfigureServices()
    
    Note over Program,DI: 依存性注入の構成
    
    DI->>DI: AddSingleton<IWinGetArgumentParser, WinGetArgumentParser>()
    DI->>DI: AddSingleton<IWinGetClient, WinGetComClient>()
    DI->>DI: AddLogging()
    
    Program->>Host: Build()
    Host-->>Program: Built Host
    
    Program->>DI: GetService<IWinGetArgumentParser>()
    DI->>Parser: Create Instance
    DI-->>Program: Parser Instance
    
    Program->>Parser: BuildRootCommand()
    Parser-->>Program: RootCommand
    
    Program->>Program: rootCommand.Parse(args)
    Program->>Program: parseResult.InvokeAsync()
    
    Note over Program: CommandHandler実行
```

### COM API + CLI フォールバック機構

```mermaid
sequenceDiagram
    participant Handler as CommandHandler
    participant Client as WinGetComClient
    participant COM as COM API
    participant Process as IProcessRunner
    participant CLI as winget.exe

    Handler->>Client: 操作要求 (例: InstallPackageAsync)
    Client->>Client: EnsureInitializedAsync()
    
    alt 初期化成功 (_comApiAvailable = true)
        Client->>COM: COM API呼び出し
        alt COM API成功
            COM-->>Client: 結果
            Client-->>Handler: OperationResult (Success)
        else COM API失敗 (COMException)
            Note over Client: COM失敗を検出、CLIフォールバックに切り替え
            Client->>Client: _comApiAvailable = false
            Client->>Process: RunAsync("winget.exe", arguments)
            Process->>CLI: プロセス実行
            CLI-->>Process: ProcessResult
            Process-->>Client: ProcessResult
            Client->>Client: ParseCliOutput(result)
            Client-->>Handler: OperationResult (Fallback)
        end
    else 初期化失敗 (_comApiAvailable = false)
        Note over Client: 最初からCLI実行
        Client->>Process: RunAsync("winget.exe", arguments)
        Process->>CLI: プロセス実行
        CLI-->>Process: ProcessResult
        Process-->>Client: ProcessResult
        Client->>Client: ParseCliOutput(result)
        Client-->>Handler: OperationResult (CLI Only)
    end
```

### エラーハンドリング階層

```mermaid
sequenceDiagram
    participant User
    participant Program as Program.cs
    participant Handler as CommandHandler
    participant Client as WinGetComClient
    participant COM as COM API

    User->>Program: コマンド実行
    Program->>Handler: ExecuteAsync()
    
    alt 正常処理
        Handler->>Client: 操作実行
        Client->>COM: COM API呼び出し
        COM-->>Client: 成功結果
        Client-->>Handler: OperationResult (Success)
        Handler-->>Program: ExitCode = 0
        Program-->>User: 成功メッセージ
    else ハンドラーレベルエラー
        Handler->>Handler: 引数バリデーションエラー
        Handler-->>Program: ExitCode = 1
        Program-->>User: エラーメッセージ
    else クライアントレベルエラー
        Handler->>Client: 操作実行
        Client->>COM: COM API呼び出し
        COM-->>Client: Exception
        Client->>Client: ErrorDetailsを作成
        Client-->>Handler: OperationResult (Failed)
        Handler-->>Program: ExitCode = 1
        Program-->>User: 詳細エラーメッセージ
    else 未処理例外
        Handler->>Client: 操作実行
        Client-->>Handler: Unhandled Exception
        Handler-->>Program: Unhandled Exception
        Program->>Program: Global Exception Handler
        Note over Program: ILogger でエラーログ記録
        Program-->>User: "Error: <exception.Message>"
        Program-->>User: ExitCode = 1
    end
```

## 設計パターンと原則

### 1. 依存性注入パターン
- .NET Generic Hostによる統一されたDIコンテナ
- インターフェース分離による疎結合設計

### 2. ストラテジーパターン (COM + CLI フォールバック)
- COM API利用可能時は高速なCOM API使用
- COM API失敗時は自動的にCLI実行にフォールバック
- 透明なフォールバック機構により可用性向上

### 3. コマンドパターン
- 各WinGetコマンドを独立したコマンドハンドラーとして実装
- 共通ベースクラスによる統一されたインターフェース

### 4. アダプターパターン
- Microsoft WindowsPackageManager COM APIをIWinGetClientに適合
- CLI実行もCOM APIと同一インターフェースで利用可能

## システム統合

### Microsoft.WindowsPackageManager.ComInterop統合
- バージョン: 1.11.430
- COM初期化: PackageManagerFactory.CreatePackageManager()
- 非同期操作: Task-based Async Pattern
- プログレス通知: IProgress&lt;OperationProgress&gt;

### System.CommandLine統合
- 引数解析フレームワーク
- コマンド階層とサブコマンドサポート
- 相互排他性とバリデーションルール

### プロセス実行抽象化
- IProcessRunner による外部プロセス実行の抽象化
- テスト可能性向上とモック可能な設計

## エラーハンドリング戦略

### 階層的エラーハンドリング
1. **Program.cs**: 最上位例外キャッチとログ出力
2. **CommandHandler**: コマンド実行時の例外処理
3. **WinGetComClient**: COM API/CLI実行の例外処理

### フォールバック機構
- COM API実行失敗時の自動CLI切り替え
- 詳細なエラー情報（ErrorDetails）の提供
- 段階的エラー回復（権限昇格要求等）

## ログとモニタリング

### 統合ログ基盤
- Microsoft.Extensions.Logging使用
- 構造化ログでの詳細な処理情報記録
- COM API/CLI実行の透明性確保

### 診断情報
- ClientInfo による実行環境情報取得
- COM API/CLI可用性状況の把握
- バージョン互換性チェック

## セキュリティ考慮事項

### プロセス実行セキュリティ
- 外部プロセス（winget.exe）実行時の引数エスケープ
- プロセス実行権限の最小化

### 入力検証
- コマンドライン引数の厳密なバリデーション
- ValidationEngineによるルールベース検証

## パフォーマンス最適化

### COM API優先戦略
- ネイティブCOM API使用による高速化
- プロセス起動オーバーヘッドの削減

### 非同期処理
- 全操作のTask-based Async Pattern対応
- CancellationToken による処理中断サポート

### リソース管理
- IDisposable による適切なリソース解放
- COM オブジェクトの確実な解放

## 拡張性とメンテナンス性

### インターフェース分離
- IWinGetClient による実装詳細の隠蔽
- 新たなWinGet実装方式の容易な追加

### モジュラー設計
- 各レイヤーの独立性確保
- テスト容易性の向上

### 設定可能性
- 依存性注入による柔軟な構成変更
- プロセス実行の抽象化によるテスト対応

## 今後の拡張予定

### フェーズ4: Gist同期機能統合
- IGistClient インターフェース追加
- OAuth Device Flow認証実装
- GitHub Gist API統合
- PowerShell版との互換性確保

### テスト基盤強化
- 単体テストカバレッジ向上（目標90%以上）
- 統合テスト・E2Eテストの実装
- パフォーマンステストの追加

---

*本書は実装状況に基づいた現時点（2025年8月）でのアーキテクチャ設計書です。*