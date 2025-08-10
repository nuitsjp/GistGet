# GistGet アーキテクチャ設計

## 1. 現在の実装状態

### 実装済み機能
- ✅ WinGet CLIへのパススルー実装
- ✅ COM API基盤の構築
- ✅ 基本的なコマンドハンドラー構造
- ⏳ Gist同期機能（未実装）

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