# GistGet

[![GitHub release (latest by date)](https://img.shields.io/github/v/release/nuitsjp/GistGet)](https://github.com/nuitsjp/GistGet/releases)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![GitHub issues](https://img.shields.io/github/issues/nuitsjp/GistGet)](https://github.com/nuitsjp/GistGet/issues)
[![GitHub pull requests](https://img.shields.io/github/issues-pr/nuitsjp/GistGet)](https://github.com/nuitsjp/GistGet/pulls)

**GistGet** は、GitHub Gist を使用して複数のデバイス間で Windows Package Manager (`winget`) パッケージを同期するために設計された CLI ツールです。プライベート/パブリック Gist に保存された YAML 設定ファイルを単一情報源として扱います。

## 機能

- **クラウド同期**: Gist 上の `gistget.yaml` を基準にインストール/アンインストール/Pin を同期。
- **Winget パススルー**: `winget` サブコマンド/オプションをほぼそのまま利用可能 (`gistget search`, `gistget upgrade` など)。
- **クロスデバイス**: 複数 PC で同じパッケージセットを再現。
- **Configuration as Code**: YAML でインストールオプションや pin を記述し、履歴管理も容易。

## インストール

### GitHub Releases から

1. [Releases ページ](https://github.com/nuitsjp/GistGet/releases) から最新リリースをダウンロード。
2. zip を展開。
3. 展開フォルダを `PATH` に追加。

### Winget から (近日公開予定)

```powershell
winget install nuitsjp.GistGet
```

## 使用方法

### 認証

```powershell
gistget auth login
```

Device Flow に従ってブラウザで認証します。

### 同期

```powershell
# 認証済みの Gist (gistget.yaml) と同期
gistget sync

# 外部 URL から同期
gistget sync --url https://gist.githubusercontent.com/user/id/raw/gistget.yaml

# ローカル YAML から同期
gistget sync --file .\gistget.yaml
```

処理概要:
1. Gist から `gistget.yaml` を取得 (または `--url`/`--file` を優先)
2. ローカルのインストール済みパッケージと比較
3. 不足分をインストール、`uninstall: true` をアンインストール、pin を同期

### エクスポート / インポート

```powershell
# 現在の状態を YAML に出力
gistget export --output my-packages.yaml

# YAML を Gist にインポート (既存を上書き)
gistget import my-packages.yaml
```

### Winget 互換コマンド

```powershell
gistget search vscode
gistget show Microsoft.PowerToys
gistget list
```

### ピン留め (Pin)

```powershell
# バージョンを固定して Gist に保存
gistget pin add <package-id> --version <version>

# ピン留めを解除
gistget pin remove <package-id>
```

## 設定ファイル

GistGet は Gist 内の `gistget.yaml` を使用します。例:

```yaml
Microsoft.PowerToys:
  silent: true
  pin: "0.80.*"
  pinType: pinning
DeepL.DeepL:
  uninstall: true
```

## 要件

- Windows 10/11
- Windows Package Manager (`winget`)

## ライセンス

MIT License
