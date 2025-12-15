# GistGet

[![GitHub release (latest by date)](https://img.shields.io/github/v/release/nuitsjp/GistGet)](https://github.com/nuitsjp/GistGet/releases)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![GitHub issues](https://img.shields.io/github/issues/nuitsjp/GistGet)](https://github.com/nuitsjp/GistGet/issues)
[![GitHub pull requests](https://img.shields.io/github/issues-pr/nuitsjp/GistGet)](https://github.com/nuitsjp/GistGet/pulls)

**GistGet** は、GitHub Gist を使用して複数のデバイス間で Windows Package Manager (`winget`) パッケージを同期するために設計された CLI ツールです。
プライベートまたはパブリック Gist に保存されたシンプルな YAML 設定ファイルを利用して、インストールされているアプリケーションやツールの一貫性を保つことができます。

## 機能

-   **☁️ クラウド同期**: GitHub Gist 経由でインストール済みパッケージを同期します。
-   **🚀 Winget 完全互換**: 標準の `winget` コマンドをそのまま利用でき、さらにクラウド同期機能が統合されています (例: `gistget search`, `gistget install`)。
-   **💻 クロスデバイス**: 職場や自宅のコンピュータを同期状態に保ちます。
-   **📄 Configuration as Code**: 読みやすい `packages.yaml` 形式でソフトウェアリストを管理します。

## インストール

### GitHub Releases から

1.  [Releases ページ](https://github.com/nuitsjp/GistGet/releases) から最新リリースをダウンロードします。
2.  zip ファイルを解凍します。
3.  解凍したフォルダをシステムの `PATH` に追加します。

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

まず、Gist アクセスを有効にするために GitHub アカウントにログインします。

```powershell
gistget auth login
```

画面の指示に従って、Device Flow を使用して認証を行います。

### 同期

ローカルパッケージを Gist と同期するには:

```powershell
gistget sync
```

これにより、以下の処理が行われます:
1.  Gist から `packages.yaml` を取得します。
2.  ローカルにインストールされているパッケージと比較します。
3.  不足しているパッケージをインストールし、削除対象としてマークされたパッケージをアンインストールします。

外部の YAML ファイルから同期するには:

```powershell
gistget sync --url https://gist.githubusercontent.com/user/id/raw/packages.yaml
```

### エクスポート / インポート

現在の状態を YAML ファイルにエクスポートするには:

```powershell
gistget export --output my-packages.yaml
```

YAML ファイルを Gist にインポートするには:

```powershell
gistget import my-packages.yaml
```

### Winget 互換コマンド

GistGet は `winget` のコマンド体系を完全にサポートしています。いつものコマンドでパッケージ管理を行いながら、クラウド同期の恩恵を受けることができます。

```powershell
gistget search vscode
gistget show Microsoft.PowerToys
```

### ピン留め (Pin)

パッケージのバージョンを固定し、自動アップグレードを防ぐことができます。GistGet の `pin` コマンドは `winget pin` を実行すると同時に、`packages.yaml` にもバージョン情報を保存して同期します。

```powershell
# バージョンを固定して Gist に保存
gistget pin add <package-id> --version <version>

# ピン留めを解除して Gist を更新
gistget pin remove <package-id>

# ピン留めされているパッケージを表示 (winget pin list と同じ)
gistget pin list
```

## 設定

GistGet は Gist 内の `packages.yaml` ファイルを使用します。

```yaml
Microsoft.PowerToys:
  version: 0.75.0
Microsoft.VisualStudioCode:
  custom: /VERYSILENT
DeepL.DeepL:
  uninstall: true
```

## 要件

-   Windows 10/11
-   Windows Package Manager (`winget`)

## ライセンス

MIT License
