# PowerShell Module Specification

## 概要

GistGetプロジェクトのPowerShellモジュール仕様書です。このモジュールは、WinGetパッケージリストをGitHub Gist、Web、またはファイルで管理するためのPowerShellモジュールです。.NETコマンドラインツールとの機能対応も含まれています。

## 機能統合表（PowerShell vs .NET CLI）

| 機能分類 | PowerShell関数 | .NET CLIコマンド | 分類 | Gist同期 | 権限 | 優先度 | 説明 |
|----------|----------------|------------------|------|----------|------|--------|------|
| **認証管理** |
| GitHub認証 | Set-GitHubToken | auth | 独立実装 | - | 不要 | 最高 | GitHub認証・トークン管理 |
| **パッケージ管理（同期機能付き）** |
| インストール | Install-GistGetPackage | install / add | COM利用 | 更新 | 要管理者 | 最高 | パッケージインストール + Gist定義更新 |
| アンインストール | Uninstall-GistGetPackage | uninstall / remove / rm | COM利用 | 更新 | 要管理者 | 最高 | パッケージアンインストール + Gist定義更新 |
| アップデート | Update-GistGetPackage | upgrade / update | COM利用 | 更新 | 要管理者 | 最高 | パッケージアップグレード + Gist定義更新 |
| 同期 | Sync-GistGetPackage | sync | COM利用 | 読込 | 要管理者 | 最高 | Gist定義パッケージをインストール（追加のみ） |
| **Gist同期専用** |
| **設定管理** |
| Gist設定取得 | Get-GistFile | *なし* | 独立実装 | - | 不要 | 最高 | GistFileオブジェクトを取得 |
| Gist設定保存 | Set-GistFile | *なし* | 独立実装 | - | 不要 | 最高 | GistFileの設定を保存 |
| システム設定 | *対応なし* | configure | パススルー | - | 要管理者 | 低 | システム構成 |
| **情報表示（パススルー）** |
| 一覧表示 | *パススルー* | list / ls | パススルー | - | 不要 | 中 | インストール済み表示 |
| 検索 | *パススルー* | search / find | パススルー | - | 不要 | 低 | パッケージ検索 |
| 詳細表示 | *パススルー* | show / view | パススルー | - | 不要 | 低 | パッケージ詳細表示 |
| **システム管理（パススルー）** |
| ソース管理 | *パススルー* | source | パススルー | - | 要管理者 | 低 | ソース管理 |
| 設定管理 | *パススルー* | settings / config | パススルー | - | 不要 | 低 | 設定管理 |
| バージョン固定 | *パススルー* | pin | パススルー | - | 不要 | 低 | ローカルバージョン固定 |
| **その他ユーティリティ** |
| インストーラDL | *パススルー* | download | パススルー | - | 不要 | 低 | インストーラダウンロード |
| 修復 | *パススルー* | repair | パススルー | - | 要管理者 | 低 | パッケージ修復 |
| ハッシュ計算 | *パススルー* | hash | パススルー | - | 不要 | 低 | ハッシュ計算 |
| 検証 | *パススルー* | validate | パススルー | - | 不要 | 低 | マニフェスト検証 |
| エクスポート | *対応なし* | export | パススルー | 読込 | 不要 | 高 | Gistから定義ファイルをダウンロード |
| インポート | *対応なし* | import | パススルー | 作成 | 不要 | 高 | 現在の環境をGistへアップロード |
| 実験的機能 | *パススルー* | features | パススルー | - | 不要 | 低 | 実験的機能 |

## パッケージ管理機能 詳細仕様

パッケージ管理機能の詳細仕様については、以下の個別ドキュメントを参照してください：

- **[Install-GistGetPackage](powershell/Install-GistGetPackage.md)** - パッケージインストール + Gist定義更新
- **[Uninstall-GistGetPackage](powershell/Uninstall-GistGetPackage.md)** - パッケージアンインストール + Gist定義更新  
- **[Update-GistGetPackage](powershell/Update-GistGetPackage.md)** - パッケージアップグレード + Gist定義更新
- **[Sync-GistGetPackage](powershell/Sync-GistGetPackage.md)** - Gist定義パッケージをインストール（追加のみ）

