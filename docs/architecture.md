# GistGet アーキテクチャ設計

## アーキテクチャ設計概要

-- ここから編集禁止 --

1. セキュリティ設計: 認証方式と認証情報の保存、設定保管先のGist情報の設定と保管
2. 論理構造設計: レイヤーモデルと、レイヤーの基本的な相互作用の設計
3. テスト設計: 単体・結合テストの方式設計

-- ここまで編集禁止 --

## 1. セキュリティ設計

### 1.1 認証方式

**GitHub OAuth Device Flow**を採用し、ユーザーの代理でGist APIにアクセスする権限を取得しています。

```mermaid
sequenceDiagram
    participant User as ユーザー
    participant App as GistGet
    participant GitHub as GitHub API
    participant Browser as ブラウザ

    User->>App: gistget auth
    App->>GitHub: Device Code要求
    GitHub-->>App: Device Code + User Code
    App->>Browser: 認証URL自動起動
    App->>User: User Codeを表示
    User->>Browser: User Code入力・認証
    App->>GitHub: Access Token取得（ポーリング）
    GitHub-->>App: Access Token
    App->>User: 認証完了通知
```

**認証の特徴:**
- OAuth Client ID: `Ov23lihQJhLB6hCnEIvS` (GistGet専用)
- スコープ: `gist`（Gist読み書きのみ）
- 管理者権限不要
- トークン有効期限なし（ユーザーが明示的に取り消すまで有効）

### 1.2 認証情報の保存

- **保存場所**: `%APPDATA%\GistGet\token.json`
- **暗号化**: 現在は平文保存（将来的にDPAPI対応予定）
- **権限**: CurrentUserスコープ（同じユーザーのみアクセス可能）

### 1.3 Gist設定の保管

- **保存場所**: `%APPDATA%\GistGet\gist.dat`
- **暗号化**: Windows DPAPI（CurrentUserスコープ）
- **内容**: Gist ID、ファイル名、タイムスタンプ

### 1.4 セキュリティ強化ポイント

**現在の実装:**
- トークンは平文保存（改善余地あり）
- Gist設定はDPAPI暗号化済み
- ファイルアクセス権限によるユーザー隔離

**将来の改善予定:**
- トークンのDPAPI暗号化
- トークンの定期的な更新機能
- 不正アクセス検知機能

## 2. 論理構造設計

### 2.1 レイヤーモデル

```mermaid
graph TB
    subgraph "プレゼンテーション層"
        CLI[コマンドライン<br/>インターフェース]
        Program[Program.cs<br/>エントリポイント]
    end
    
    subgraph "アプリケーション層"
        RunnerApp[RunnerApplication.cs<br/>実行制御]
        CommandService[CommandService.cs<br/>コマンドルーター]
        Commands[Commands/<br/>個別コマンド実装]
    end
    
    subgraph "ドメイン層"
        Services[Services/<br/>ビジネスロジック]
        Models[Models/<br/>データモデル]
    end
    
    subgraph "インフラストラクチャ層"
        WinGetCOM[WinGetComClient.cs<br/>COM API]
        WinGetExe[WinGetPassthrough.cs<br/>exe実行]
        GitHubAPI[GitHubAuthService.cs<br/>GitHub API]
        Storage[Storage/<br/>設定保存]
    end
    
    subgraph "外部システム"
        WinGet[WinGet COM API]
        WinGetProcess[winget.exe]
        GitHub[GitHub API]
        FileSystem[ファイルシステム]
    end

    CLI --> Program
    Program --> RunnerApp
    RunnerApp --> CommandService
    CommandService --> Commands
    Commands --> Services
    Services --> Models
    Services --> WinGetCOM
    Services --> WinGetExe
    Services --> GitHubAPI
    Services --> Storage
    WinGetCOM --> WinGet
    WinGetExe --> WinGetProcess
    GitHubAPI --> GitHub
    Storage --> FileSystem
```

### 2.2 レイヤー間の基本的な相互作用

- 依存性注入による疎結合
- コマンドルーティング戦略

### 2.3 抽象化レイヤー

- インターフェース定義
  - コマンド実行の抽象化
  - WinGet操作の抽象化
  - GitHub認証の抽象化
  - Gist同期の抽象化
  - エラー処理の抽象化
- 実装戦略の切り替え
  - COM API優先、失敗時にwinget.exe実行にフォールバック
  - テスト時はモック実装に差し替え可能
  - 将来的な機能拡張に対応

## 3. テスト設計

### 3.1 テスト方式

**テスト実装の現状**:
- テストプロジェクト: `tests/NuitsJp.GistGet.Tests/`
- テストフレームワーク: xUnit + Moq + Shouldly
- .NET 8 Windows 10環境（win-x64）での実行
- 13個のテストファイル（各主要クラスをカバー）

**テストファイル一覧**:
- `CommandServiceTests.cs` - コマンドルーターのテスト
- `GistConfigurationStorageTests.cs` - 設定保存のテスト
- `GistConfigurationTests.cs` - 設定モデルのテスト
- `WinGetComClientTests.cs` - WinGet COM APIのテスト
- `RunnerApplicationTests.cs` - アプリケーションエントリポイントのテスト
- `MockServices.cs` - テスト用モック実装

### 3.2 単体テスト設計

**テストアーキテクチャ**:

| レイヤー | テスト対象 | 実装済みテスト | テスト戦略 |
|---------|-----------|---------------|-----------|
| **Services** | ビジネスロジック | `CommandServiceTests.cs`<br/>`GistManagerTests.cs` | Moqを使用した依存性隔離テスト |
| **Models** | データモデル | `GistConfigurationTests.cs`<br/>`PackageDefinitionTests.cs`<br/>`PackageCollectionTests.cs` | プロパティ検証・バリデーション |
| **Storage** | データ永続化 | `GistConfigurationStorageTests.cs` | 一時ファイルでのDPAPI暗号化テスト |
| **Infrastructure** | 外部システム連携 | `WinGetComClientTests.cs`<br/>`GitHubGistClientTests.cs` | モック・スタブによる隔離テスト |
| **Application** | アプリケーション制御 | `RunnerApplicationTests.cs` | エントリポイントの動作検証 |

**モック戦略**:
- `MockWinGetClient`: COM API呼び出しをモック化
- `MockWinGetPassthroughClient`: winget.exe実行をモック化  
- `MockGistSyncService`: Gist同期処理をモック化
- Moqライブラリによる動的モック生成

### 3.3 結合テスト設計

**結合テストの段階**:

1. **認証前提テスト**: 
   - `gistget auth`で事前認証が必要
   - 実際のGitHub APIとの通信をテスト

2. **Gist設定前提テスト**:
   - 認証 + Gist設定の両方が必要
   - 実際のGist読み書きをテスト

3. **E2Eテスト**:
   - コマンドライン引数から最終結果まで
   - 実際のwinget.exe実行を含む

- テスト実行戦略

### 3.4 テスト実行環境

**技術的制約**:
- ターゲット環境: `net8.0-windows10.0.26100` （Windows 10限定）
- ランタイム: `win-x64` （64bit Windows専用）
- COM API依存のため、CI/CD環境ではWindows Runnerが必須

**テスト実行戦略**:

| テストタイプ | 実行環境 | 自動化レベル | 制約事項 |
|-------------|----------|-------------|----------|
| **単体テスト** | ローカル/CI | 完全自動化 | モック使用で外部依存なし |
| **統合テスト** | ローカル環境のみ | 半自動化 | COM API・winget.exe依存 |
| **E2Eテスト** | ローカル環境のみ | 手動実行 | 実際のGitHub API接続必要 |

**ローカル開発環境での実行**:
```bash
# テスト実行コマンド
dotnet test tests/NuitsJp.GistGet.Tests/

# カバレッジ取得
dotnet test --collect:"XPlat Code Coverage"
```

**前提条件**:
- Windows 10/11 (Build 26100以降)
- .NET 8 SDK
- Visual Studio 2022またはVS Code
- WinGet COM APIの利用可能な環境

## 4. 実装の特徴とアーキテクチャ原則

### 4.1 設計原則

1. **段階的実装**: パススルー → COM API → Gist同期の順で機能追加
2. **後方互換性**: 既存のwingetワークフローを破壊しない
3. **フォールバック戦略**: COM API失敗時の自動フォールバック
4. **セキュアデフォルト**: 認証情報の適切な保護

### 4.2 技術スタック

- **フレームワーク**: .NET 8
- **COM API**: Microsoft.Management.Deployment
- **HTTP通信**: System.Net.Http（GitHub API用）
- **暗号化**: Windows DPAPI
- **ログ**: Microsoft.Extensions.Logging
- **DI**: Microsoft.Extensions.DependencyInjection

### 4.3 エラーハンドリング戦略

- 階層化されたエラー処理
- 回復戦略
  - COM API失敗 → winget.exe実行
  - ネットワーク失敗 → オフライン動作継続
  - 認証失敗 → 再認証プロンプト