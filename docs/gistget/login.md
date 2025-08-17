# loginコマンド仕様書

## 概要
GitHub認証を管理するコマンドです。OAuth Device Flowを使用してGitHubアカウントとの認証を行い、Gist操作に必要なアクセストークンを安全に取得・保存します。

## 基本動作方針

### コアコンセプト
- **OAuth Device Flow認証**: ブラウザベースの安全な認証フロー
- **トークン管理**: Windows DPAPI暗号化でのトークン安全保存
- **認証状態確認**: 現在の認証状態と有効性の確認
- **ログアウト機能**: トークンの安全な削除
- **自動認証**: 他コマンドからの自動認証フロー対応

### 動作フロー

1. **認証方式の決定**
   - ローカル開発: OAuth Device Flow
   - CI/CD環境: 環境変数 `GIST_TOKEN`
   - 手動設定: Personal Access Token（上級者向け）

2. **OAuth Device Flow実行**
   - GitHubからDevice CodeとUser Codeを取得
   - ユーザーにブラウザでの認証URLを表示
   - ポーリングでトークン取得を確認

3. **トークン検証**
   - 取得したトークンの有効性確認
   - 必要な権限（gistスコープ）の確認

4. **トークン保存**
   - Windows DPAPI暗号化でローカル保存
   - 認証情報の設定ファイル更新

5. **認証完了確認**
   - 認証成功の確認
   - ユーザー情報の表示

## 詳細仕様

### コマンドライン引数
```bash
gistget login [options]
```

**オプション**:
- `--status`: 現在の認証状態のみ表示
- `--logout`: ログアウト（認証情報削除）
- `--token <token>`: Personal Access Tokenを直接指定（上級者向け）
- `--force`: 既存認証を上書き
- `--timeout <seconds>`: 認証タイムアウト時間（デフォルト: 300秒）

### OAuth Device Flow認証プロセス

#### 1. 認証開始
```
GitHub認証を開始します
===================

以下のURLをブラウザで開いてください:
https://github.com/login/device

ユーザーコード: ABCD-1234

認証が完了するまで待機しています...
```

#### 2. 認証進行中
```
認証待機中... (残り時間: 4分30秒)

ブラウザでユーザーコード「ABCD-1234」を入力してください。
認証をキャンセルするには Ctrl+C を押してください。
```

#### 3. 認証完了
```
GitHub認証が完了しました！
========================

ユーザー: username
メール: user@example.com
権限: gist, read:user

認証情報が安全に保存されました。
次回以降の実行では認証は不要です。
```

### 認証状態確認（--statusオプション）

#### 認証済み状態
```
GitHub認証状態
=============

✓ 認証状態: 有効
  ユーザー: username
  メール: user@example.com
  権限: gist, read:user
  認証日時: 2024-01-10 15:00:00
  有効期限: なし（Personal Access Token）

✓ Gist権限: 有効
  読み取り: ✓
  書き込み: ✓
```

#### 未認証状態
```
GitHub認証状態
=============

✗ 認証状態: 未認証
  認証が必要です

推奨アクション: gistget login を実行
```

#### 認証エラー状態
```
GitHub認証状態
=============

✗ 認証状態: エラー
  トークン: 無効（期限切れまたは取り消し済み）
  最終認証: 2024-01-10 15:00:00

推奨アクション: gistget login --force を実行
```

### ログアウト（--logoutオプション）

#### ログアウト確認
```
ログアウトの確認
===============

現在の認証情報:
  ユーザー: username
  認証日時: 2024-01-10 15:00:00

警告: ログアウト後はGist操作ができなくなります。

ログアウトしますか？ (y/N): 
```

#### ログアウト完了
```
ログアウトが完了しました
=====================

削除された認証情報:
  ✓ アクセストークン
  ✓ ユーザー情報
  ✓ 認証キャッシュ

再度Gist操作を行う場合は「gistget login」を実行してください。
```

### エラーハンドリング

#### OAuth認証エラー
- **認証タイムアウト**: 指定時間内に認証が完了しなかった場合
- **ユーザーキャンセル**: ユーザーがCtrl+Cで中断した場合
- **認証拒否**: GitHubでユーザーが認証を拒否した場合
- **ネットワークエラー**: GitHub APIへの接続に失敗した場合

#### トークンエラー
- **無効なトークン**: 指定されたPersonal Access Tokenが無効
- **権限不足**: 必要なgistスコープがない場合
- **期限切れ**: トークンの有効期限が切れている場合

#### システムエラー
- **暗号化エラー**: Windows DPAPI暗号化に失敗した場合
- **ファイル操作エラー**: 設定ファイルの書き込みに失敗した場合
- **CI環境**: OAuth Device Flowが使用できない環境での実行

#### 終了コード
- `0`: 正常終了（認証成功または状態表示成功）
- `1`: 認証エラー
- `2`: ネットワークエラー
- `3`: システムエラー
- `4`: ユーザーキャンセル

## シーケンス図

```mermaid
sequenceDiagram
    participant User as ユーザー
    participant Router as CommandRouter
    participant LoginCmd as LoginCommand
    participant AuthSvc as AuthService
    participant GitHubAPI as GitHub API
    participant TokenStore as TokenStore

    User->>Router: gistget login
    Router->>LoginCmd: ExecuteAsync()
    
    note over LoginCmd: OAuth Device Flow開始
    LoginCmd->>AuthSvc: StartDeviceFlowAsync()
    AuthSvc->>GitHubAPI: POST /login/device/code
    GitHubAPI-->>AuthSvc: device_code, user_code, verification_uri
    AuthSvc-->>LoginCmd: DeviceFlowInfo
    
    LoginCmd->>User: "ブラウザで認証してください: https://github.com/login/device"
    LoginCmd->>User: "ユーザーコード: ABCD-1234"
    
    note over LoginCmd: ポーリングでトークン取得
    loop 認証完了まで
        LoginCmd->>AuthSvc: PollForTokenAsync(device_code)
        AuthSvc->>GitHubAPI: POST /login/oauth/access_token
        GitHubAPI-->>AuthSvc: access_token or pending
        AuthSvc-->>LoginCmd: TokenResult
        
        alt トークン取得成功
            break
        else まだ認証中
            LoginCmd->>LoginCmd: Wait 5 seconds
        end
    end
    
    note over LoginCmd: トークン検証
    LoginCmd->>AuthSvc: ValidateTokenAsync(access_token)
    AuthSvc->>GitHubAPI: GET /user
    GitHubAPI-->>AuthSvc: UserInfo
    AuthSvc-->>LoginCmd: ValidationResult
    
    note over LoginCmd: トークン保存
    LoginCmd->>TokenStore: SaveTokenAsync(access_token, user_info)
    TokenStore->>TokenStore: Encrypt with DPAPI
    TokenStore-->>LoginCmd: success
    
    LoginCmd->>LoginCmd: DisplaySuccessMessage()
    LoginCmd-->>User: "認証完了" (exit 0)
```

## 実装クラス

### LoginCommand (Presentation層)
```csharp
public class LoginCommand
{
    public async Task<int> ExecuteAsync(LoginOptions options)
    {
        // UI制御：認証フロー表示、進捗表示、結果表示
        // Business層への委譲：AuthService.AuthenticateAsync()
    }
    
    private async Task<bool> ShowDeviceFlowInstructions(DeviceFlowInfo flowInfo)
    {
        // OAuth Device Flowの案内表示
    }
    
    private async Task<bool> WaitForAuthentication(string deviceCode, int timeout)
    {
        // 認証完了まで待機とポーリング
    }
}
```

### AuthService (Business層)
```csharp
public class AuthService : IAuthService
{
    public async Task<bool> AuthenticateAsync(LoginOptions options)
    {
        // 認証処理のメイン制御
        // 1. 認証方式の決定
        // 2. OAuth Device Flow実行
        // 3. トークン検証
        // 4. トークン保存
    }
    
    public async Task<DeviceFlowInfo> StartDeviceFlowAsync()
    {
        // OAuth Device Flowの開始
    }
    
    public async Task<TokenResult> PollForTokenAsync(string deviceCode)
    {
        // トークン取得のポーリング
    }
    
    public async Task<bool> ValidateTokenAsync(string token)
    {
        // トークンの有効性確認
    }
    
    public async Task<bool> IsAuthenticatedAsync()
    {
        // 現在の認証状態確認
    }
    
    public async Task<bool> LogoutAsync()
    {
        // ログアウト処理
    }
}
```

### TokenStore (Infrastructure層)
```csharp
public class TokenStore : ITokenStore
{
    public async Task SaveTokenAsync(string token, UserInfo userInfo)
    {
        // Windows DPAPI暗号化でトークンを保存
    }
    
    public async Task<string> GetTokenAsync()
    {
        // 暗号化されたトークンを復号化して取得
    }
    
    public async Task DeleteTokenAsync()
    {
        // トークンファイルの安全な削除
    }
}
```

### LoginOptions (Business層モデル)
```csharp
public class LoginOptions
{
    public bool Status { get; set; }
    public bool Logout { get; set; }
    public string Token { get; set; }
    public bool Force { get; set; }
    public int Timeout { get; set; } = 300;
}
```

### DeviceFlowInfo (Business層モデル)
```csharp
public class DeviceFlowInfo
{
    public string DeviceCode { get; set; }
    public string UserCode { get; set; }
    public string VerificationUri { get; set; }
    public int ExpiresIn { get; set; }
    public int Interval { get; set; }
}
```

## 依存関係

### 必要なサービス
- `IGitHubApiClient`: GitHub API通信
- `ITokenStore`: トークン保存管理
- `ILogger<T>`: ログ出力

### 設定要件
- インターネット接続（GitHub API用）
- ローカルファイルシステム書き込み権限

## テスト戦略

### 単体テスト (Business層)
- OAuth Device Flow処理のテスト
- トークン検証ロジックのテスト
- 認証状態管理のテスト
- エラーハンドリングのテスト

### 統合テスト (Infrastructure層)
- GitHub API呼び出しのテスト
- Windows DPAPI暗号化のテスト
- トークンファイル操作のテスト

### E2Eテスト
- 実際のGitHub認証フローのテスト
- CI環境での環境変数認証テスト
- エラーケースでの適切な動作確認

## 実装注意点

### セキュリティ
- Windows DPAPI暗号化での安全なトークン保存
- トークンのメモリ上での適切な管理
- ログ出力でのトークン情報の非表示化
- CI環境での環境変数認証の安全な処理

### ユーザビリティ
- 分かりやすい認証フロー案内
- 認証進行状況の視覚的表示
- エラー時の具体的な解決策提示
- キャンセル操作の適切な処理

### 堅牢性
- ネットワーク接続エラーの適切な処理
- 認証タイムアウトの処理
- トークン期限切れの自動検出
- CI/CD環境での適切な動作

### パフォーマンス
- 適切なポーリング間隔の設定
- タイムアウト時間の最適化
- キャッシュ機能の活用

### PowerShell版との互換性
- PowerShell版と同じ認証フロー
- 同じトークン保存形式
- 一貫したエラーメッセージ

## 重要：CI/CD環境での対応

### 環境変数認証
```bash
# CI/CD環境での認証
export GIST_TOKEN="ghp_xxxxxxxxxxxxxxxxxxxx"
gistget sync  # 自動的に環境変数のトークンを使用
```

### 認証方式の自動判定
```csharp
public async Task<AuthMethod> DetermineAuthMethodAsync()
{
    // 1. 環境変数 GIST_TOKEN の確認
    // 2. CI環境の検出
    // 3. 適切な認証方式の選択
}
```

### セットアップフローとの連携
```bash
# 1. GitHub認証
gistget login

# 2. 認証状態確認
gistget login --status

# 3. Gist設定
gistget gist set

# 4. 認証が必要なコマンドの自動認証
gistget sync  # 未認証時は自動的にloginフローを実行
```

**loginコマンドはGistGetの認証基盤として重要な役割を果たし、安全で使いやすい認証体験を提供することで、すべてのGist連携機能の前提条件を満たします。**

この仕様に基づき、PowerShellモジュール版と同等の機能を持つ.NET版loginコマンドを実装します。