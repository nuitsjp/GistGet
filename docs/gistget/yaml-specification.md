# GistGet YAML仕様書

## 概要
GistGetが使用するYAML定義ファイルの仕様です。GitHub Gistに保存されるパッケージ定義ファイルの構造と各パラメータについて説明します。

## YAML構造

### 基本構造
```yaml
PackageId1:
PackageId2:
  parameter1: value1
  parameter2: value2
PackageId3:
  uninstall: true
```

### 構造の説明
- **トップレベルキー**: WinGetパッケージID（例: `Microsoft.VisualStudioCode`, `Git.Git`）
- **値**: パッケージのインストール設定（省略可能）
- **空の値**: パッケージIDのみでデフォルト設定を使用

## YAML定義例

### 実際のYAML例
```yaml
7zip.7zip:
Microsoft.VisualStudioCode:
  allowHashMismatch: true
  architecture: x64
  custom: /VERYSILENT /NORESTART /MERGETASKS=!runcode,addcontextmenufiles,addcontextmenufolders
  force: true
  header: 'Authorization: Bearer xxx'
  installerType: exe
  locale: en-US
  location: C:\Program Files\Microsoft VS Code
  log: C:\Temp\vscode_install.log
  mode: silent
  override: /SILENT
  scope: machine
  skipDependencies: true
  version: 1.85.0
  confirm: true
  whatIf: true
  uninstall: false
Git.Git:
  version: 2.43.0
PowerShell.PowerShell:
  scope: user
Zoom.Zoom:
  uninstall: true
```

## パラメータ一覧

### 必須パラメータ
なし（パッケージIDのみで基本インストールが可能）

### オプションパラメータ

#### インストール制御
| パラメータ | 型 | 説明 | 例 |
|-----------|---|------|-----|
| `version` | string | 特定バージョンの指定 | `1.85.0` |
| `scope` | string | インストールスコープ<br>`user`, `machine`, `any` | `machine` |
| `architecture` | string | アーキテクチャ指定<br>`x86`, `x64`, `arm`, `arm64` | `x64` |
| `locale` | string | ロケール指定（BCP 47形式） | `en-US`, `ja-JP` |
| `location` | string | インストール場所の指定 | `C:\Program Files\MyApp` |

#### インストーラー設定
| パラメータ | 型 | 説明 | 例 |
|-----------|---|------|-----|
| `custom` | string | カスタムインストール引数 | `/SILENT /NORESTART` |
| `override` | string | インストール引数の上書き | `/VERYSILENT` |
| `mode` | string | インストールモード<br>`default`, `silent`, `interactive` | `silent` |
| `installerType` | string | インストーラータイプ<br>`exe`, `msi`, `msix`, `zip` など | `exe` |

#### 高度な設定
| パラメータ | 型 | 説明 | 例 |
|-----------|---|------|-----|
| `force` | boolean | 強制インストール | `true` |
| `allowHashMismatch` | boolean | ハッシュ不一致を許可 | `true` |
| `skipDependencies` | boolean | 依存関係のスキップ | `true` |
| `header` | string | カスタムHTTPヘッダー | `Authorization: Bearer xxx` |
| `log` | string | ログファイルパス | `C:\Temp\install.log` |

#### 操作制御
| パラメータ | 型 | 説明 | 例 |
|-----------|---|------|-----|
| `uninstall` | boolean | アンインストール指定 | `true` |
| `confirm` | boolean | 確認プロンプト表示 | `true` |
| `whatIf` | boolean | 実行前の確認（ドライラン） | `true` |

## パラメータ詳細仕様

### version
```yaml
Microsoft.VisualStudioCode:
  version: 1.85.0
```
- 特定バージョンのインストールを指定
- 省略時は最新バージョンをインストール
- セマンティックバージョニング形式

### scope
```yaml
PowerShell.PowerShell:
  scope: user
```
- `user`: ユーザースコープでインストール
- `machine` (デフォルト): システム全体にインストール
- `any`: インストーラーに依存

### architecture
```yaml
Microsoft.VisualStudioCode:
  architecture: x64
```
- `x86`: 32ビット版
- `x64`: 64ビット版
- `arm`: ARM 32ビット版
- `arm64`: ARM 64ビット版

### custom
```yaml
Microsoft.VisualStudioCode:
  custom: /VERYSILENT /NORESTART /MERGETASKS=!runcode,addcontextmenufiles
```
- インストーラーに渡すカスタム引数
- 複数の引数はスペース区切りで指定
- インストーラー固有の引数をサポート

### uninstall
```yaml
Zoom.Zoom:
  uninstall: true
```
- `true`: Sync-GistGetPackage実行時にアンインストール
- `false` (デフォルト): 通常のインストール対象
- アンインストール指定のパッケージ管理に使用

## 使用例とパターン

### 基本的なパッケージ
```yaml
Git.Git:
7zip.7zip:
Microsoft.PowerToys:
```

### バージョン固定
```yaml
Microsoft.VisualStudioCode:
  version: 1.85.0
PowerShell.PowerShell:
  version: 7.4.0
```

### カスタム設定
```yaml
Microsoft.VisualStudioCode:
  scope: user
  custom: /SILENT /MERGETASKS=addcontextmenufiles,addcontextmenufolders
Docker.DockerDesktop:
  location: D:\Applications\Docker
  log: C:\Temp\docker_install.log
```

### アンインストール指定
```yaml
# 以前使用していたが現在は不要
OldSoftware.Package:
  uninstall: true
  
# 一時的に削除
DeepL.DeepL:
  uninstall: true
```

### 高度な設定
```yaml
Microsoft.VisualStudioCode.Insiders:
  version: 1.86.0-insider
  architecture: x64
  scope: machine
  force: true
  allowHashMismatch: true
  custom: /VERYSILENT /NORESTART /MERGETASKS=!runcode
  log: C:\Temp\vscode_insider_install.log
```

## 実装における注意点

### YAML解析
- パッケージIDは大文字小文字を区別
- パラメータ名はcamelCase形式
- Boolean値は `true`/`false` （小文字）
- 文字列値はクォート省略可（特殊文字含む場合は必要）

### パラメータ優先順位
1. YAML定義のパラメータ
2. コマンドライン引数
3. WinGetデフォルト設定

### 互換性
- PowerShellモジュール版と完全互換
- .NET版でも同一のYAML構造を使用
- 新規パラメータ追加時は下位互換性を維持

## データモデル（.NET実装用）

### C#クラス構造例
```csharp
public class PackageDefinition
{
    public string Id { get; set; }
    public bool? AllowHashMismatch { get; set; }
    public string Architecture { get; set; }
    public string Custom { get; set; }
    public bool? Force { get; set; }
    public string Header { get; set; }
    public string InstallerType { get; set; }
    public string Locale { get; set; }
    public string Location { get; set; }
    public string Log { get; set; }
    public string Mode { get; set; }
    public string Override { get; set; }
    public string Scope { get; set; }
    public bool? SkipDependencies { get; set; }
    public string Version { get; set; }
    public bool? Confirm { get; set; }
    public bool? WhatIf { get; set; }
    public bool? Uninstall { get; set; }
}

public class PackageCollection : Dictionary<string, PackageDefinition>
{
    // パッケージIDをキーとする辞書
    // YAML構造との直接的なマッピング
}
```

## 検証ルール

### 必須検証
- パッケージIDの形式確認（Vendor.Product形式）
- Boolean値の妥当性
- Architecture値の妥当性
- Scope値の妥当性

### 推奨検証
- バージョン形式の確認
- ファイルパスの妥当性
- カスタム引数の安全性

## 移行とアップグレード

### 旧形式からの移行
PowerShellモジュール版と.NET版では同一のYAML形式を使用するため、移行は不要です。

### 新機能追加時
- 新しいパラメータは常にオプション
- デフォルト値による下位互換性確保
- 段階的な機能展開

この仕様に基づき、GistGetの全コマンドで一貫したYAML処理を実装します。