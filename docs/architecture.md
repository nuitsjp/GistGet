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
│           COM API Layer                 │ WinGetComClient
├─────────────────────────────────────────┤
│        Microsoft WinGet API             │ COM Interop
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

**責務**: WinGet操作の統一インターフェースとCOM API実装

- **IWinGetClient**: 公開インターフェース
- **WinGetComClient**: COM API専用実装

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
    // COM API専用実装
    public async Task<OperationResult> InstallPackageAsync(InstallOptions options)
    
    // COM API初期化
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
        -_isInitialized: bool
        +InitializeAsync() Task
        +InstallPackageAsync(InstallOptions options) Task~OperationResult~
        +Dispose() void
    }

    Program --> IWinGetArgumentParser : uses
    WinGetArgumentParser ..|> IWinGetArgumentParser : implements
    Program --> BaseCommandHandler : creates
    BaseCommandHandler <|-- InstallCommandHandler : extends
    BaseCommandHandler <|-- ListCommandHandler : extends
    BaseCommandHandler --> IWinGetClient : uses
    WinGetComClient ..|> IWinGetClient : implements
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
    
    OperationResult --> ErrorDetails : contains
    IWinGetClient --> WinGetPackage : returns
    IWinGetClient --> InstallOptions : accepts
    IWinGetClient --> ListOptions : accepts
    IWinGetClient --> OperationResult : returns
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

    User->>Program: install --id Git.Git
    Program->>Parser: BuildRootCommand()
    Parser-->>Program: RootCommand
    Program->>Parser: Parse(args)
    Parser->>Handler: CreateInstallHandler()
    Handler->>Client: GetService<IWinGetClient>()
    
    Note over Handler,Client: 依存性注入によるクライアント取得
    
    Handler->>Client: InstallPackageAsync(options)
    Client->>Client: EnsureInitializedAsync()
    Client->>COM: InstallPackageAsync(options)
    
    alt COM API成功
        COM-->>Client: OperationResult (Success)
        Client-->>Handler: OperationResult (Success)
        Handler-->>Program: ExitCode = 0
        Program-->>User: インストール完了
    else COM API失敗
        COM-->>Client: Exception
        Client->>Client: ErrorDetailsを作成
        Client-->>Handler: OperationResult (Failed)
        Handler-->>Program: ExitCode = 1
        Program-->>User: 詳細エラーメッセージ
    end
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

### COM API 初期化と実行フロー

```mermaid
sequenceDiagram
    participant Handler as CommandHandler
    participant Client as WinGetComClient
    participant COM as COM API

    Handler->>Client: 操作要求 (例: InstallPackageAsync)
    Client->>Client: EnsureInitializedAsync()
    
    alt COM API初期化成功
        Note over Client: COM API利用可能
        Client->>COM: COM API呼び出し
        alt COM API操作成功
            COM-->>Client: 結果
            Client-->>Handler: OperationResult (Success)
        else COM API操作失敗
            COM-->>Client: Exception
            Client->>Client: 詳細なErrorDetailsを作成
            Client-->>Handler: OperationResult (Failed with Details)
        end
    else COM API初期化失敗
        Note over Client: 環境設定不備
        Client-->>Handler: WinGetInitializationException
        Note over Handler: "WinGet COM APIが利用できません。\n環境設定を確認してください。"
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
    else COM API初期化失敗
        Handler->>Client: 操作実行
        Client->>Client: COM API初期化試行
        Note over Client: WinGet環境設定不備
        Client-->>Handler: WinGetInitializationException
        Handler-->>Program: ExitCode = 1
        Program-->>User: "WinGet COM APIが利用できません。環境設定を確認してください。"
    else COM API操作失敗
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

### 2. 例外処理パターン
- COM API専用の明確なエラーハンドリング
- 環境設定問題の明確な診断メッセージ
- 段階的エラー回復とユーザーガイダンス

### 3. コマンドパターン
- 各WinGetコマンドを独立したコマンドハンドラーとして実装
- 共通ベースクラスによる統一されたインターフェース

### 4. アダプターパターン
- Microsoft WindowsPackageManager COM APIをIWinGetClientに適合
- COM API専用の最適化されたインターフェース実装

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

### COM API専用最適化
- ネイティブCOM API直接利用によるオーバーヘッド削減
- プロセス間通信の削除による高速化

## エラーハンドリング戦略

### 階層的エラーハンドリング
1. **Program.cs**: 最上位例外キャッチとログ出力
2. **CommandHandler**: コマンド実行時の例外処理
3. **WinGetComClient**: COM API/CLI実行の例外処理

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

## パフォーマンス最適化

### COM API専用戦略
- ネイティブCOM API直接利用による最適化
- 外部プロセス不要による高速化とリソース効率

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
- COM API専用の最適化されたテスト支援

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