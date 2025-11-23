# GistGet

**GistGet** は、GitHub Gist を使用して複数のデバイス間で Windows Package Manager (`winget`) パッケージを同期するために設計された CLI ツールです。プライベートまたはパブリック Gist に保存されたシンプルな YAML 設定ファイルを利用して、インストールされているアプリケーションやツールの一貫性を保つことができます。

## 機能

-   **クラウド同期**: GitHub Gist 経由でインストール済みパッケージを同期します。
-   **Winget パススルー**: `winget` コマンドのラッパーとして `gistget` を使用できます (例: `gistget search`, `gistget install`)。
-   **クロスデバイス**: 職場や自宅のコンピュータを同期状態に保ちます。
-   **Configuration as Code**: 読みやすい `packages.yaml` 形式でソフトウェアリストを管理します。

## インストール

### GitHub Releases から

1.  [Releases ページ](https://github.com/nuitsjp/GistGet/releases) から最新リリースをダウンロードします。
2.  zip ファイルを解凍します。
3.  解凍したフォルダをシステムの `PATH` に追加します。

### Winget から (近日公開予定)

```powershell
winget install nuitsjp.GistGet
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

### エクスポート / インポート

現在の状態を YAML ファイルにエクスポートするには:

```powershell
gistget export --output my-packages.yaml
```

YAML ファイルを Gist にインポートするには:

```powershell
gistget import my-packages.yaml
```

### Winget コマンド

`gistget` は `winget` と同様に使用できます。コマンドは基盤となる `winget` 実行可能ファイルにパススルーされます。

```powershell
gistget search vscode
gistget show Microsoft.PowerToys
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
