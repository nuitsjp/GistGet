## 【.NET 8 + WinGet COM ラッパー & Gist 認証付き同期モジュール】設計

### 1. 背景と目的

* **課題**: WinGet を利用し、異なる環境間でパッケージと設定（ソース、構成含む）を同期したいが、WinGet は標準でサポートしていない。
* **目的**:

  * .NET 8（自己完結型）で WinGet の COM API をラップし、信頼性の高い同期ツールを構築。
  * 同期設定（パッケージ一覧など）を GitHub Gist に保存し、OAuth Device Flow による認証で安全に自動読み書き。

---

### 2. WinGet CLI 完全準拠仕様

#### A. 対応コマンド一覧
| コマンド | エイリアス | 主要機能 | 状態 |
|----------|------------|----------|------|
| install | add | パッケージインストール | 予定 |
| list | ls | インストール済みパッケージ表示 | 予定 |
| upgrade | update | パッケージアップグレード | 予定 |
| uninstall | - | パッケージアンインストール | 予定 |
| search | - | パッケージ検索 | 予定 |
| show | - | パッケージ情報表示 | 予定 |
| source | - | パッケージソース管理 | 予定 |
| settings | config | 設定管理 | 予定 |
| export | - | パッケージリストエクスポート | 予定 |
| import | - | パッケージリストインポート | 予定 |
| pin | - | パッケージピン管理 | 予定 |
| configure | - | システム構成 | 予定 |
| download | - | インストーラダウンロード | 予定 |
| repair | - | パッケージ修復 | 予定 |
| hash | - | ハッシュ計算ヘルパー | 予定 |
| validate | - | マニフェスト検証 | 予定 |
| features | - | 実験的機能管理 | 予定 |
| dscv3 | - | DSC v3リソース | 予定 |

#### B. 共通オプション
```
グローバルオプション (全コマンド共通):
  -?, --help                  ヘルプ表示
  -v, --version              バージョン表示
  --info                     一般情報表示
  --wait                     キー入力待機
  --logs, --open-logs        ログ場所を開く
  --verbose, --verbose-logs  詳細ログ有効化
  --nowarn, --ignore-warnings 警告非表示
  --disable-interactivity    対話プロンプト無効化
  --proxy                    プロキシ設定
  --no-proxy                 プロキシ使用無効化
```

#### C. 主要コマンド詳細

##### install/add コマンド
```
使用法: winget install [[-q] <query>...] [options]
必須: <query> または --id/--name/--moniker のいずれか

検索オプション:
  -q, --query <text>         検索クエリ
  --id <packageid>          パッケージID指定
  --name <name>             パッケージ名指定
  --moniker <moniker>       モニカー指定
  -e, --exact               完全一致検索
  -s, --source <source>     検索ソース指定

インストールオプション:
  -v, --version <version>   バージョン指定
  --scope <user|machine>    インストール範囲
  -a, --architecture <arch> アーキテクチャ指定
  --installer-type <type>   インストーラータイプ
  -i, --interactive         対話式インストール
  -h, --silent              サイレントインストール
  -l, --location <path>     インストール場所
  --locale <locale>         ロケール (BCP47)
  -o, --log <logfile>       ログファイル
  --custom <args>           追加引数
  --override <args>         引数上書き
  
セキュリティ・動作オプション:
  --ignore-security-hash    ハッシュチェック無視
  --allow-reboot            再起動許可
  --skip-dependencies       依存関係スキップ
  --ignore-local-archive-malware-scan マルウェアスキャン無視
  --dependency-source <src> 依存関係検索ソース
  --accept-package-agreements パッケージ使用許諾同意
  --accept-source-agreements ソース使用許諾同意
  --no-upgrade              アップグレードスキップ
  -r, --rename <name>       実行ファイル名変更 (ポータブル)
  --uninstall-previous      旧バージョン削除
  --force                   強制実行

認証オプション:
  --header <header>         HTTPヘッダー
  --authentication-mode <mode> 認証モード (silent/silentPreferred/interactive)
  --authentication-account <account> 認証アカウント
```

##### list/ls コマンド
```
使用法: winget list [[-q] <query>] [options]

フィルタリング:
  -q, --query <text>        検索クエリ
  --id <packageid>         パッケージID
  --name <name>            パッケージ名
  --moniker <moniker>      モニカー
  --tag <tag>              タグ
  --cmd, --command <cmd>   コマンド
  -s, --source <source>    ソース指定
  -e, --exact              完全一致
  --scope <user|machine>   スコープ指定
  -n, --count <number>     結果数制限 (1-1000)

アップグレード関連:
  --upgrade-available      アップグレード可能のみ表示
  -u, --unknown, --include-unknown 不明バージョン含む
  --pinned, --include-pinned ピン付き含む

認証:
  --header <header>        HTTPヘッダー
  --authentication-mode <mode> 認証モード
  --authentication-account <account> 認証アカウント
  --accept-source-agreements ソース使用許諾同意
```

##### upgrade/update コマンド
```
使用法: winget upgrade [[-q] <query>...] [options]

検索・フィルタリング:
  -q, --query <text>       検索クエリ
  --id <packageid>        パッケージID
  --name <name>           パッケージ名
  --moniker <moniker>     モニカー
  -s, --source <source>   検索ソース
  -e, --exact             完全一致

アップグレード制御:
  -v, --version <version> バージョン指定
  -r, --recurse, --all    全パッケージアップグレード
  -u, --unknown, --include-unknown 不明バージョン含む
  --pinned, --include-pinned ピン付き含む
  --uninstall-previous    旧バージョン削除
  --force                 強制実行

インストールオプション (installと同様):
  --scope, --architecture, --installer-type, --locale
  -i, --interactive, -h, --silent, -l, --location
  -o, --log, --custom, --override
  --ignore-security-hash, --allow-reboot, --skip-dependencies
  --accept-package-agreements, --accept-source-agreements
  --purge (ポータブル用)
```

##### source コマンド
```
使用法: winget source [サブコマンド] [options]

サブコマンド:
  add <name> <arg>        新ソース追加
  list                    ソース一覧
  update [name]           ソース更新
  remove <name>           ソース削除  
  reset                   ソースリセット
  export                  ソースエクスポート

add オプション:
  --name <name>          ソース名
  --arg <url>            ソースURL/引数
  --type <type>          ソースタイプ
  --trust-level <level>  信頼レベル
  --explicit             明示的ソース指定
  --header <header>      HTTPヘッダー
```

##### export/import コマンド
```
# export
使用法: winget export [-o] <output> [options]
  -o, --output <file>         出力ファイル
  -s, --source <source>       対象ソース
  --include-versions          バージョン含む
  --accept-source-agreements  ソース使用許諾同意

# import  
使用法: winget import [-i] <import-file> [options]
  -i, --import-file <file>    インポートファイル
  --ignore-unavailable        利用不可パッケージ無視
  --ignore-versions           バージョン無視
  --no-upgrade                アップグレードスキップ
  --accept-package-agreements パッケージ使用許諾同意
  --accept-source-agreements  ソース使用許諾同意
```

##### settings/config コマンド
```
使用法: winget settings [サブコマンド] [options]

サブコマンド:
  (なし)                  設定ファイルを開く
  export                  設定エクスポート
  set <setting> <value>   管理者設定
  reset <setting>         設定リセット

管理者設定:
  --enable <setting>      設定有効化
  --disable <setting>     設定無効化

対象設定例:
  LocalManifestFiles      ローカルマニフェストファイル許可
  BypassCertificatePinningForMicrosoftStore MS Store証明書固定回避
  InstallerHashOverride   インストーラーハッシュ上書き許可
```

#### D. 引数処理アーキテクチャ設計

##### 技術的制約と解決方針
- **ConsoleAppFramework制限**: 複雑なサブコマンド階層、条件付きオプション、相互排他性への対応困難
- **WinGet引数の特徴**: 階層サブコマンド (`source add`)、エイリアス、条件付きバリデーション
- **解決方針**: ConsoleAppFramework + カスタム引数パーサーのハイブリッド実装

##### カスタム引数パーサー設計
```csharp
// 引数解析フロー
public class WinGetArgumentParser
{
    // 1. 基本コマンド識別 (ConsoleAppFramework)
    public ParsedCommand ParseCommand(string[] args);
    
    // 2. WinGet固有引数解析 (カスタム実装)
    public class InstallCommandOptions
    {
        // 相互排他性チェック
        public string Query { get; set; }
        public string Id { get; set; }      // Query と排他
        public string Name { get; set; }    // Query と排他
        
        // 条件付きオプション
        public bool IncludeUnknown { get; set; }    // --upgrade-available 必須
        public bool IncludePinned { get; set; }     // --upgrade-available 必須
        
        // 複雑なバリデーション
        public void Validate()
        {
            if (string.IsNullOrEmpty(Query) && string.IsNullOrEmpty(Id) && 
                string.IsNullOrEmpty(Name))
                throw new ArgumentException("Query, Id, Name のいずれか必須");
                
            if (IncludeUnknown && !UpgradeAvailable)
                throw new ArgumentException("--include-unknown は --upgrade-available 必須");
        }
    }
}

// 3. COM API呼び出し (Microsoft.WindowsPackageManager.ComInterop)
public interface IWinGetClient  
{
    Task<InstallResult> InstallAsync(InstallCommandOptions options);
    Task<List<InstalledPackage>> ListAsync(ListCommandOptions options);  
    Task<UpgradeResult> UpgradeAsync(UpgradeCommandOptions options);
}
```

---

### 3. アーキテクチャ概要

#### A. WinGet COM ラッパー層（.NET 8 前提）

##### NuGet パッケージとCOM統合
* **主要依存**: `Microsoft.WindowsPackageManager.ComInterop` 1.11.430 ([GitHub][1], [nuget.org][2])
* **ランタイム**: .NET 8 (自己完結型展開対応)
* **COM初期化**: `PackageManagerFactory.CreatePackageManager()` を使用

##### インターフェース階層設計
```csharp
// 最上位インターフェース - WinGet CLI完全準拠
public interface IWinGetClient
{
    // 基本パッケージ操作
    Task<InstallResult> InstallAsync(InstallOptions options);
    Task<List<InstalledPackage>> ListAsync(ListOptions options);
    Task<UpgradeResult> UpgradeAsync(UpgradeOptions options);
    Task<UninstallResult> UninstallAsync(UninstallOptions options);
    Task<List<SearchResult>> SearchAsync(SearchOptions options);
    Task<PackageDetails> ShowAsync(ShowOptions options);
    
    // ソース管理
    Task<SourceResult> SourceAddAsync(SourceAddOptions options);
    Task<List<PackageSource>> SourceListAsync(SourceListOptions options);
    Task<SourceResult> SourceUpdateAsync(SourceUpdateOptions options);
    Task<SourceResult> SourceRemoveAsync(SourceRemoveOptions options);
    Task<SourceResult> SourceResetAsync();
    Task<List<PackageSource>> SourceExportAsync();
    
    // インポート・エクスポート
    Task<ExportResult> ExportAsync(ExportOptions options);
    Task<ImportResult> ImportAsync(ImportOptions options);
    
    // 設定管理
    Task<SettingsResult> SettingsGetAsync();
    Task<SettingsResult> SettingsSetAsync(SettingsSetOptions options);
    Task<SettingsResult> SettingsResetAsync(SettingsResetOptions options);
    Task<SettingsResult> SettingsExportAsync();
    
    // その他のコマンド
    Task<List<PinnedPackage>> PinListAsync();
    Task<PinResult> PinAddAsync(PinAddOptions options);
    Task<PinResult> PinRemoveAsync(PinRemoveOptions options);
    Task<DownloadResult> DownloadAsync(DownloadOptions options);
    Task<RepairResult> RepairAsync(RepairOptions options);
    Task<string> HashAsync(HashOptions options);
    Task<ValidateResult> ValidateAsync(ValidateOptions options);
    Task<List<Feature>> FeaturesAsync();
    Task<ConfigureResult> ConfigureAsync(ConfigureOptions options);
}

// COM APIラッパー - 低レベル操作
public interface IWinGetComClient
{
    Task<IInstallResult> InstallPackageAsync(PackageManager packageManager, InstallOptions options);
    Task<IEnumerable<MatchResult>> SearchPackagesAsync(PackageManager packageManager, SearchOptions options);
    Task<IEnumerable<CatalogPackage>> GetInstalledPackagesAsync(PackageManager packageManager);
    // COM API直接操作...
}

// CLI フォールバック - COM API利用不可時
public interface IWinGetCliClient  
{
    Task<ProcessResult> ExecuteWinGetAsync(string command, string[] arguments);
    Task<T> ParseWinGetOutput<T>(ProcessResult result);
}
```

##### エラーハンドリングと復帰機構
```csharp
public class WinGetClient : IWinGetClient
{
    private readonly IWinGetComClient _comClient;
    private readonly IWinGetCliClient _cliClient;
    private readonly ILogger<WinGetClient> _logger;
    
    public async Task<InstallResult> InstallAsync(InstallOptions options)
    {
        try
        {
            // 1. COM API優先実行
            return await _comClient.InstallPackageAsync(options);
        }
        catch (COMException comEx) when (IsComApiUnavailable(comEx))
        {
            // 2. CLI フォールバック
            _logger.LogWarning("COM API失敗、CLIフォールバックに切り替え: {Error}", comEx.Message);
            return await _cliClient.ExecuteInstallAsync(options);
        }
        catch (UnauthorizedAccessException authEx)
        {
            // 3. 権限エラーの場合は管理者権限要求
            throw new WinGetAuthorizationException("管理者権限が必要です", authEx);
        }
    }
}
```

##### パフォーマンス最適化
- **COM接続プール**: PackageManagerインスタンス再利用
- **非同期処理**: 全操作をTask-based Async Pattern対応
- **キャンセレーション**: CancellationToken全面対応
- **進捗レポート**: IProgress<T>による詳細進捗通知

#### B. 同期ドメイン

* Gist に保存されたファイルをもとに設定を同期する
* [PowerShell版](./powershell/)の実装に準拠する

#### C. 認証（GitHub Gist への安全なアクセス）

* **認証方式**: OAuth App + Device Flow によるトークン取得（PAT 自動作成は不可）([GitHub Docs][3])。
* フロー:

  1. `device_code` と `verification_uri` を取得し、ブラウザを起動。
  2. ユーザーが認証後、アプリは `access_token` をポーリングで取得。
  3. 以降は `Authorization: Bearer <token>` で Gist API 呼び出し。
* スコープ: `gist`（公開・Secret Gist 読み書き可能）([GitHub Docs][3])。
* 実装参考: GitHub の Device Flow 手順および GitHub App 向けサンプル CLI ([GitHub Docs][4])。

#### D. トークン保存と管理

* アクセストークンは **Windows DPAPI** などでユーザー領域に暗号化保存。
* トークンが未設定／期限切れの場合、自動トークン再取得（Device Flow 再実行）。

---

### 4. 実装計画

**詳細な実装ロードマップは [TODO.md](./TODO.md) を参照してください。**

#### 開発フェーズ概要
1. **フェーズ1**: WinGetコマンド完全仕様書作成 【最優先】
2. **フェーズ2**: カスタム引数パーサー実装  
3. **フェーズ3**: COM APIラッパー実装
4. **フェーズ4**: Gist同期機能統合
5. **フェーズ5**: テストと品質保証

#### 最優先事項
- **ドキュメント作成**: WinGetコマンド仕様書が全ての基盤
- **引数パーサー**: WinGet準拠が品質の核心  
- **COM API安定性**: フォールバック機構で可用性確保


[1]: https://github.com/microsoft/winget-cli/issues/4320?utm_source=chatgpt.com "Issues with COM API and retrieving installed packages"
[2]: https://www.nuget.org/packages/Microsoft.WindowsPackageManager.ComInterop?utm_source=chatgpt.com "Microsoft.WindowsPackageManager.ComInterop 1.11.430"
[3]: https://docs.github.com/en/apps/oauth-apps/building-oauth-apps/authorizing-oauth-apps?utm_source=chatgpt.com "Authorizing OAuth apps - GitHub Docs"
[4]: https://docs.github.com/enterprise-cloud%40latest/apps/creating-github-apps/writing-code-for-a-github-app/building-a-cli-with-a-github-app?utm_source=chatgpt.com "Building a CLI with a GitHub App"
[5]: https://learn.microsoft.com/en-us/windows/package-manager/winget/?utm_source=chatgpt.com "Use WinGet to install and manage applications"
[6]: https://www.reddit.com/r/golang/comments/17m22mq/github_oauth2_device_flow_does_anyone_have_an/?utm_source=chatgpt.com "github oauth2 device flow. does anyone have an example?"
[7]: https://github.com/microsoft/winget-cli/issues/4377?utm_source=chatgpt.com "WinRTAct.dll from Microsoft.WindowsPackageManager. ..."
