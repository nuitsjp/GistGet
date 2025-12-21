# GistGet

[![GitHub release (latest by date)](https://img.shields.io/github/v/release/nuitsjp/GistGet)](https://github.com/nuitsjp/GistGet/releases)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![GitHub issues](https://img.shields.io/github/issues/nuitsjp/GistGet)](https://github.com/nuitsjp/GistGet/issues)
[![GitHub pull requests](https://img.shields.io/github/issues-pr/nuitsjp/GistGet)](https://github.com/nuitsjp/GistGet/pulls)

**GistGet**は、GitHub Gistを使用して複数のデバイス間でWindows Package Manager(`winget`)パッケージを同期するために設計されたCLIツールです。
プライベートまたはパブリックGistに保存されたシンプルなYAML設定ファイルを利用して、インストールされているアプリケーションやツールの一貫性を保つことができます。

## 機能

-   **☁️ クラウド同期**: GitHub Gist経由でインストール済みパッケージを同期します。
-   **🚀 Winget 完全互換**: 標準の `winget` コマンドをそのまま利用でき、さらにクラウド同期機能が統合されています (例: `gistget search`, `gistget install`)。
-   **💻 クロスデバイス**: 職場や自宅のコンピューターを同期状態に保ちます。
-   **📄 Configuration as Code**: 読みやすい `GistGet.yaml` 形式でソフトウェアリストを管理します。

## 要件

-   Windows 10/11
-   Windows Package Manager (`winget`)

## インストール

### GitHub Releases から

1.  [Releases ページ](https://github.com/nuitsjp/GistGet/releases) から最新リリースをダウンロードします。
2.  zipファイルを解凍します。
3.  解凍したフォルダーをシステムの`PATH`に追加します。

### Winget から (近日公開予定)

```powershell
winget install nuitsjp.GistGet
```

インストール後は以下で起動できます:

```powershell
gistget --help
```

## 使用方法

### 認証

まず、Gistアクセスを有効にするためにGitHubアカウントにログインします。

```powershell
gistget auth login
```

画面の指示にしたがって、Device Flowを使用して認証を行います。

### 初期設定 (init)

新しいPCのセットアップや、既存環境のクラウド同期を始める場合は、まず `init` コマンドで同期対象のパッケージを選択します:

```powershell
gistget init
```

これにより、ローカルにインストールされているパッケージの一覧が表示され、対話的にクラウド同期対象を選択できます。選択が完了すると、Gistに `GistGet.yaml` が作成（または上書き）されます。

### 同期

ローカルパッケージをGistと同期するには:

```powershell
gistget sync
```

これにより、以下の処理が行われます:
1.  Gistから`GistGet.yaml`を取得します。
2.  ローカルにインストールされているパッケージと比較します。
3.  不足しているパッケージをインストールし、削除対象としてマークされたパッケージをアンインストールします。

外部のYAMLファイルから同期するには:

```powershell
gistget sync --url https://gist.githubusercontent.com/user/id/raw/GistGet.yaml
```

### Winget 互換コマンド

GistGetは`winget`のコマンド体系を完全にサポートしています。いつものコマンドでパッケージ管理を行いながら、クラウド同期の恩恵を受けることができます。

```powershell
gistget search vscode
gistget show Microsoft.PowerToys
```

## 設定

GistGetはGist内の `GistGet.yaml` ファイルを使用します。パッケージIDをキーとし、インストールオプションと同期フラグを値とするマップです。

```yaml
<PackageId>:
  name: <string>                   # winget の表示名（自動設定）
  pin: <string>                   # ピン留めバージョン（省略でピン留めなし）
  pinType: <pinning | blocking | gating>  # ピンの種類（省略時はpinning）
  uninstall: <boolean>            # trueでアンインストール対象
  # インストールオプション（winget パススルー）
  scope: <user | machine>
  architecture: <x86 | x64 | arm | arm64>
  installerType: <string>
  interactive: <boolean>
  silent: <boolean>
  locale: <string>
  location: <string>
  log: <string>
  custom: <string>
  override: <string>
  force: <boolean>
  acceptPackageAgreements: <boolean>
  acceptSourceAgreements: <boolean>
  allowHashMismatch: <boolean>
  skipDependencies: <boolean>
  header: <string>
```

### コアパラメーター

| パラメーター | 型 | 説明 |
|-----------|-----|------|
| `name` | string | winget が表示するパッケージ名。`install` / `upgrade` / `uninstall` / `pin add` / `init` で自動設定される。 |
| `pin` | string | ピン留めするバージョン。省略でピン留めなし（常に最新版）。ワイルドカード `*` 使用可（例: `1.7.*`）。 |
| `pinType` | enum | ピンの種類。`pin` が指定されている場合のみ有効。省略時は `pinning`。 |
| `uninstall` | boolean | `true` の場合、sync 時にアンインストールされる。 |

### pinType の種類

| 値 | 説明 | `upgrade --all` | `upgrade <pkg>` |
|----|------|-----------------|-----------------|
| なし | pin なし。すべての upgrade 対象。 | ✅ 可能 | ✅ 可能 |
| `pinning` | デフォルト。`upgrade --all` から除外されるが、明示的 upgrade は可能。 | ❌ スキップ | ✅ 可能 |
| `blocking` | `upgrade --all` から除外。明示的 upgrade も可能。 | ❌ スキップ | ✅ 可能 |
| `gating` | 指定バージョン範囲内のみ upgrade 可能（例: `1.7.*`）。 | 範囲内のみ | 範囲内のみ |

### インストールオプション (winget パススルー)

| パラメーター | wingetオプション | 説明 |
|-----------|------------------|------|
| `scope` | `--scope` | `user` または `machine` |
| `architecture` | `--architecture` | `x86`, `x64`, `arm`, `arm64` |
| `installerType` | `--installer-type` | インストーラータイプ |
| `interactive` | `--interactive` | 対話型インストール |
| `silent` | `--silent` | サイレントインストール |
| `locale` | `--locale` | ロケール（BCP47形式） |
| `location` | `--location` | インストール先パス |
| `log` | `--log` | ログファイルパス |
| `custom` | `--custom` | 追加のインストーラー引数 |
| `override` | `--override` | インストーラー引数の上書き |
| `force` | `--force` | 強制実行 |
| `acceptPackageAgreements` | `--accept-package-agreements` | パッケージ契約に同意 |
| `acceptSourceAgreements` | `--accept-source-agreements` | ソース契約に同意 |
| `allowHashMismatch` | `--ignore-security-hash` | ハッシュ不一致を無視 |
| `skipDependencies` | `--skip-dependencies` | 依存関係をスキップ |
| `header` | `--header` | カスタム HTTP ヘッダー |

### 設定例

```yaml
# 最新版をインストール、アップグレード可能（ピン留めなし）
Microsoft.VisualStudioCode:
  name: Visual Studio Code
  scope: user
  silent: true
  override: /VERYSILENT /MERGETASKS=!runcode

# バージョン 23.01 に固定（upgrade --all から除外）
7zip.7zip:
  name: 7-Zip
  pin: "23.01"
  architecture: x64

# バージョン 1.7.x の範囲に制限（gating）
jqlang.jq:
  name: jq
  pin: "1.7.*"
  pinType: gating

# アンインストール対象
DeepL.DeepL:
  name: DeepL
  uninstall: true
```


## 開発者向け

本セクションは、GistGetプロジェクトに貢献する開発者向けの情報を提供します。

### 開発環境

- **OS**: Windows 10/11（Windows 10.0.26100.0以降）
- **.NET SDK**: .NET 10.0以降
- **Windows SDK**: 10.0.26100.0以降（UAP Platformを含む）
- **IDE**: Visual Studio 2022またはVisual Studio Code（推奨）
- **Windows Package Manager**: winget（Windows App Installer経由）
- **PowerShell**: 5.1以降（スクリプト実行用）

### 参考資料

- 実装サンプル: `external/winget-cli/samples/WinGetClientSample/`
- GitHub: [microsoft/winget-cli](https://github.com/microsoft/winget-cli)

### 開発用スクリプト

> [!IMPORTANT]
> 統合テストは実際のGitHub APIを使用します。テスト実行前に必ず `Run-AuthLogin.ps1` で認証を完了してください。

#### 1. Run-AuthLogin.ps1（初回・認証切れ時）

GitHub認証を実行し、認証情報をWindows Credential Managerに保存するスクリプト:

```powershell
.\scripts\Run-AuthLogin.ps1
```

認証情報は永続化されるため、初回実行後や認証期限切れ時のみ実行が必要です。

#### 2. Run-CodeQuality.ps1（日常の開発作業）

コード品質パイプラインを実行する統合スクリプト:

```powershell
# 全ステップ実行 (デフォルト)
# FormatCheck → Build → Tests → ReSharper
.\scripts\Run-CodeQuality.ps1

# 特定のステップのみ実行
.\scripts\Run-CodeQuality.ps1 -Build           # Buildのみ
.\scripts\Run-CodeQuality.ps1 -Build -Tests    # BuildとTestsのみ
.\scripts\Run-CodeQuality.ps1 -Tests           # Testsのみ

# Releaseビルドで実行
.\scripts\Run-CodeQuality.ps1 -Configuration Release

# カバレッジ閾値を変更
.\scripts\Run-CodeQuality.ps1 -CoverageThreshold 95
```

### リリース

リリースはGitHub Actionsで自動化されています。タグをプッシュすると、ビルド、GitHub Releasesへのアップロード、WinGetへのPR作成が自動実行されます。

#### 正式リリース

```powershell
# 1. csproj のバージョンを更新してコミット
git add .
git commit -m "chore: bump version to 0.2.0"

# 2. タグを作成してプッシュ
git tag v0.2.0
git push origin main --tags
```

#### プレリリース

プレリリースタグ（`-alpha`、`-beta`、`-rc`など）を使用すると、WinGetへのPR作成がスキップされます:

```powershell
git tag v0.2.0-beta.1
git push origin --tags
```

## ライセンス

MIT License
