# GistGet YAML仕様書

このドキュメントでは、GistGet が Windows Package Manager (winget) パッケージを同期するために使用する `packages.yaml` のスキーマを定義します。

## スキーマ概要

YAML ファイルは、キーがパッケージ ID であり、値がインストールパラメータと同期フラグを含むオブジェクトであるマップで構成されます。

```yaml
<PackageId>:
  version: <string>
  uninstall: <boolean>
  scope: <user|machine>
  interactive: <boolean>
  silent: <boolean>
  locale: <string>
  location: <string>
  log: <string>
  custom: <string>
  override: <string>
  force: <boolean>
  allowHashMismatch: <boolean>
  skipDependencies: <boolean>
  acceptPackageAgreements: <boolean>
  acceptSourceAgreements: <boolean>
  header: <string>
  architecture: <x86|x64|arm|arm64>
  installerType: <exe|msi|msix|...>
```

## パッケージ ID と CLI の制約

GistGet は winget が提供するオプションを極力パススルーし、受け取った値をそのまま `winget.exe` に渡します。  
その前提として `packages.yaml` における各エントリのキーは winget の `--id` と 1 対 1 で対応しなければならないため、`winget install` および `winget uninstall` の実行時には必ず `--id` を指定し、`--query` や `--name` での曖昧な指定はエラーとして拒否します。  
これにより、YAML に記載された ID と実際に実行される winget コマンドのターゲットが完全に一致し、Gist 定義ファイルを信頼できる単一情報源として扱えます。

## パラメータ

### コアパラメータ

*   **`version`**: インストールするパッケージの特定のバージョン。
    *   **指定あり**: 指定されたバージョンをインストールし、**winget でピン留め**して自動アップグレードを防ぎます。
    *   **省略**: 最新バージョンをインストールします（ピン留めは行われません）。
    *   *対応コマンド*: `winget install --version <value>` / `winget pin add`
*   **`uninstall`**: `true` の場合、同期中にパッケージがアンインストールされます。
    *   *対応コマンド*: `winget uninstall`

### インストールオプション (Winget パススルー)

これらのパラメータは `winget install` オプションに直接マップされます。

*   **`scope`**: パッケージを現在のユーザーまたはマシン全体にインストールします。
    *   *値*: `user`, `machine`
    *   *対応コマンド*: `--scope <value>`
*   **`architecture`**: インストールするアーキテクチャを指定します。
    *   *値*: `x86`, `x64`, `arm`, `arm64`
    *   *対応コマンド*: `--architecture <value>`
*   **`installerType`**: インストーラタイプを指定します。
    *   *値*: `exe`, `msi`, `msix` など
    *   *対応コマンド*: `--installer-type <value>`
*   **`interactive`**: 対話型インストールを要求します。
    *   *対応コマンド*: `--interactive`
*   **`silent`**: サイレントインストールを要求します。
    *   *対応コマンド*: `--silent`
*   **`locale`**: ロケール (BCP47 形式) を指定します。
    *   *対応コマンド*: `--locale <value>`
*   **`location`**: インストール場所を指定します。
    *   *対応コマンド*: `--location <value>`
*   **`log`**: ログファイルへのパス。
    *   *対応コマンド*: `--log <value>`
*   **`custom`**: インストーラに渡す追加の引数。
    *   *対応コマンド*: `--custom "<value>"`
*   **`override`**: インストーラに渡される引数を上書きします。
    *   *対応コマンド*: `--override "<value>"`
*   **`force`**: コマンドの実行を強制します。
    *   *対応コマンド*: `--force`
*   **`allowHashMismatch`**: ハッシュの不一致エラーを無視します。
    *   *対応コマンド*: `--ignore-security-hash`
*   **`skipDependencies`**: 依存関係の処理をスキップします。
    *   *対応コマンド*: `--skip-dependencies`
*   **`acceptPackageAgreements`**: すべてのパッケージ契約に同意します。
    *   *対応コマンド*: `--accept-package-agreements`
*   **`acceptSourceAgreements`**: すべてのソース契約に同意します。
    *   *対応コマンド*: `--accept-source-agreements`
*   **`header`**: REST ソース用のカスタム HTTP ヘッダー。
    *   *対応コマンド*: `--header "<value>"`

### 高度な / 新しいオプション (サポート予定)

*   **`allowReboot`**: 該当する場合、再起動を許可します。 (`--allow-reboot`)
*   **`noUpgrade`**: すでにインストールされている場合はアップグレードをスキップします。 (`--no-upgrade`)
*   **`uninstallPrevious`**: アップグレード中に以前のバージョンをアンインストールします。 (`--uninstall-previous`)
*   **`rename`**: 実行可能ファイルの名前を変更します (ポータブル)。 (`--rename`)

## 例

```yaml
Microsoft.VisualStudioCode:
  scope: user
  silent: true
  override: /VERYSILENT /MERGETASKS=!runcode

7zip.7zip:
  version: 23.01
  architecture: x64

DeepL.DeepL:
  uninstall: true
```
