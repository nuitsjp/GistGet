# GistGet コマンド仕様

## 概要
GistGet は、GitHub Gist を介して設定管理を提供する `winget` のラッパー CLI ツールです。すべての `winget` コマンドをパススルーでサポートし、同期と認証のための特定のコマンドを追加します。

## コマンド構造
```
gistget <command> [options]
```

## コアコマンド (GistGet 固有)

### `sync`
ローカルのパッケージ状態を Gist に保存されている設定と同期します。

**構文:**
```bash
gistget sync [--url <gist-url>] [--dry-run]
```

**オプション:**
- `--url <gist-url>`: この同期操作に特定の Gist URL (raw または web) を使用します。Gist が公開されている場合、認証は不要です。
- `--dry-run`: (将来予定) 変更を行わずに何が起こるかを表示します。

**動作:**
1.  Gist (または URL) からパッケージリストを取得します。
2.  ローカルにインストールされているパッケージと比較します。
3.  不足しているパッケージをインストールします。
4.  `uninstall: true` とマークされているパッケージが存在する場合、アンインストールします。
5.  バージョンの不一致がある場合、パッケージを更新します (設定可能)。

### `export`
現在のローカルパッケージ状態を GistGet 互換の YAML ファイルにエクスポートします。

**構文:**
```bash
gistget export [--output <file>]
```

**オプション:**
- `--output <file>`: 出力ファイルパスを指定します。デフォルトは標準出力またはデフォルトファイルです。

### `import`
YAML ファイルをインポートし、Gist を更新します。

**構文:**
```bash
gistget import <file> [--create-gist]
```

**オプション:**
- `--create-gist`: 既存の Gist を更新する代わりに、新しい Gist を作成します。

### `auth`
GitHub との認証を管理します。

**構文:**
```bash
gistget auth <subcommand>
```

**サブコマンド:**
- `login`: Device Flow 認証を開始します。
- `logout`: 保存された資格情報をクリアします。
- `status`: 現在の認証状態を確認します。

## パススルーコマンド (Winget 互換)
その他のすべてのコマンドは `winget` にパススルーされます。

- `install`
- `uninstall`
- `upgrade`
- `list`
- `search`
- `show`
- `source`
- `settings`
- `features`

**例:**
```bash
gistget search vscode
# 以下と同等: winget search vscode
```

## データ形式 (packages.yaml)
設定ファイルは、キーがパッケージ ID である YAML 辞書です。

```yaml
<PackageId>:
  version: <string>
  custom: <string> # カスタムインストール引数
  uninstall: <boolean> # true の場合、パッケージが削除されていることを確認します
  # その他の winget インストールパラメータ
  scope: <user|machine>
  interactive: <boolean>
  silent: <boolean>
  locale: <string>
  location: <string>
  architecture: <x86|x64|arm|arm64>
```
