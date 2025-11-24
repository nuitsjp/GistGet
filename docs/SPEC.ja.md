# GistGet コマンド仕様

## 概要
GistGet は、GitHub Gist を介して設定管理を提供する `winget` のラッパー CLI ツールです。すべての `winget` コマンドをパススルーでサポートし、同期と認証のための特定のコマンドを追加します。

## コマンド構造
```
gistget <command> [options]
```

## コアコマンド (GistGet 固有)

GistGet 独自の機能を提供するコマンド群です。これらは `winget` には存在しないか、`winget` の機能を拡張してクラウド同期を実現しています。

### `sync`
Gist 上の定義 (`packages.yaml`) とローカル環境の同期を行います。

**構文:**
```bash
gistget sync [--url <gist-url>] [--dry-run]
```

**オプション:**
- `--url <gist-url>`: 
    - 指定された Gist URL (Raw または Web URL) からパッケージ定義を取得して同期します。
    - このオプションを使用する場合、認証は不要です (パブリック Gist の場合)。
    - デフォルトでは、認証済みユーザーの Gist から `gistget-packages.yaml` を検索して使用します。
- `--dry-run`: 
    - (将来予定) 実際の変更を行わず、実行されるであろうインストール/アンインストール操作を表示します。

**詳細な動作:**
1.  **定義の取得**: Gist (または指定 URL) から YAML 形式のパッケージリストを取得します。
2.  **差分検出**: ローカルにインストールされている `winget` パッケージと比較します。
3.  **インストール**: 定義に存在し、ローカルにないパッケージを `winget install` でインストールします。
4.  **アンインストール**: 定義で `uninstall: true` とマークされているパッケージがローカルに存在する場合、`winget uninstall` で削除します。
5.  **バージョンの固定 (Pinning)**:
    - YAML で `version` が指定されている場合、そのバージョンに固定 (`winget pin add`) します。
    - `version` が指定されていない場合、固定を解除 (`winget pin remove`) し、常に最新版への追従を許可します。
6.  **エラーハンドリング**: 個別のパッケージ操作が失敗しても処理を継続し、最後に失敗したパッケージの一覧とエラーを表示します。

### `export`
現在のローカル環境のパッケージ状態を GistGet 互換の YAML ファイルに出力します。

**構文:**
```bash
gistget export [--output <file>]
```

**オプション:**
- `--output <file>`: 
    - 指定されたファイルパスに YAML を保存します。
    - 省略時は標準出力に表示されるか、デフォルトのファイル名が使用される場合があります (実装依存)。

**動作:**
- `winget list` 相当の情報を取得し、GistGet の `packages.yaml` 形式に変換して出力します。
- 固定されているバージョン情報も反映されます。

### `import`
ローカルの YAML ファイルを読み込み、Gist 上の定義を更新します。

**構文:**
```bash
gistget import <file> [--create-gist]
```

**オプション:**
- `--create-gist`: 
    - 既存の Gist を更新するのではなく、強制的に新しい Gist を作成します。

**動作:**
- 指定された YAML ファイルを読み込みます。
- GitHub API を使用して Gist を更新します。
    - 既存の `gistget-packages.yaml` を含む Gist が見つかればそれを更新します。
    - 見つからない場合は、新規に Gist (Secret) を作成します。
- **認証必須**: このコマンドを実行するには `gistget auth login` での認証が必要です。

### `auth`
GitHub Gist へのアクセス権限を管理します。

**構文:**
```bash
gistget auth <subcommand>
```

**サブコマンド:**
- `login`: 
    - GitHub Device Flow を使用して認証を開始します。
    - ブラウザで表示されたコードを入力することで、安全にトークンを取得します。
    - 取得したアクセストークンは Windows 資格情報マネージャー (`GistGet:GitHub:AccessToken`) に安全に保存されます。
- `logout`: 
    - Windows 資格情報マネージャーからアクセストークンを削除します。
- `status`: 
    - 現在の認証状態 (ログイン済みか否か) を確認します。

## パススルーコマンド (Winget 互換)
その他のすべてのコマンドは `winget` にそのまま渡されます (パススルー)。
`gistget` を `winget` のエイリアスとして使用することで、ツールを切り替えることなくシームレスに操作可能です。

- `upgrade`
- `list`
- `search`
- `show`
- `source`
- `settings`
- `features`
- その他 `winget` がサポートするすべてのコマンド

**例:**
```bash
gistget search vscode
# 内部実行: winget search vscode
```

## データ形式 (packages.yaml)
設定ファイルは、キーがパッケージ ID である YAML 辞書です。

```yaml
<PackageId>:
  version: <string>    # (任意) 特定バージョンに固定する場合に指定
  custom: <string>     # (任意) インストール時に渡すカスタム引数
  uninstall: <boolean> # (任意) true の場合、このパッケージを削除対象とします
  
  # 将来的な拡張用フィールド (現時点では未使用または計画中)
  scope: <user|machine>
  interactive: <boolean>
  silent: <boolean>
  locale: <string>
  location: <string>
  architecture: <x86|x64|arm|arm64>
```
