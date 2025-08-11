# Uninstall-GistGetPackage

## 概要
指定された条件に基づいてパッケージをアンインストールし、GistGetパッケージリストを更新する関数です。

## 関連ソースファイル
- **関数実装**: [Uninstall-GistGetPackage.ps1](../../powershell/src/Public/Uninstall-GistGetPackage.ps1)
- **テストファイル**: [Uninstall-GistGetPackage.Tests.ps1](../../powershell/test/Public/Uninstall-GistGetPackage.Tests.ps1)
- **ドキュメント**: [Uninstall-GistGetPackage.md](../../powershell/docs/en-us/Uninstall-GistGetPackage.md)

## 基本動作フロー

1. **Gist情報の取得**
   - `Get-GistFile()`を呼び出してGist情報を取得
   - `Get-GistGetPackage()`を呼び出して既存のパッケージリストを取得

2. **パラメーター分離**
   関数は受け取ったパラメーターを以下の2つのカテゴリに分離します：
   - **Get-WinGetPackage用**: Query, Command, Count, Id, MatchOption, Moniker, Name, Source, Tag
   - **Uninstall-WinGetPackage用**: Force, Mode

3. **パッケージ検索**
   - `Get-WinGetPackage`を実行してインストール済みパッケージを検索

4. **パッケージアンインストール処理**
   各対象パッケージに対して以下を実行：
   - **パッケージアンインストール**: `Uninstall-WinGetPackage`を実行
   - **Gistパッケージリストの更新**: 該当パッケージの`Uninstall`フラグを`true`に設定

5. **パッケージが見つからない場合の処理**
   - 警告メッセージを表示
   - `Id`パラメーターが指定されている場合、Gist内の該当パッケージの`Uninstall`フラグを`true`に設定

6. **Gist更新**
   - パッケージが更新された場合は`Set-GistGetPackages`を呼び出してGistを更新

## パラメーター仕様

### 検索パラメーター
- **Query**: パッケージ検索文字列配列（パイプライン対応）
- **Command**: パッケージに関連するコマンド（パイプライン対応）
- **Count**: 返すパッケージ数（パイプライン対応）
- **Id**: パッケージID（パイプライン対応）
- **MatchOption**: 検索マッチオプション（パイプライン対応）
- **Moniker**: パッケージモニカー（パイプライン対応）
- **Name**: パッケージ名（パイプライン対応）
- **Source**: WinGetソース（パイプライン対応）
- **Tag**: パッケージタグ（パイプライン対応）

### アンインストールパラメーター
- **Force**: 強制アンインストール
- **Mode**: アンインストールモード（Default, Silent, Interactive）

## エラーハンドリング
- **Gist取得エラー**: Gist情報の取得に失敗した場合は例外をスロー
- **パッケージ検索エラー**: 該当パッケージが見つからない場合は警告を表示
- **アンインストールエラー**: 個別パッケージのアンインストール失敗時もGist更新は継続

## 依存関数
- **Get-GistFile()**: Gist情報取得
- **Get-GistGetPackage()**: 既存パッケージリスト取得
- **Get-WinGetPackage**: インストール済みパッケージの検索（Microsoft.WinGet.Client）
- **Uninstall-WinGetPackage**: パッケージアンインストール（Microsoft.WinGet.Client）
- **Set-GistGetPackages()**: Gistパッケージリスト更新

## 特記事項
- **論理削除**: パッケージは物理的に削除されず、`Uninstall`フラグによる論理削除
- **パイプライン対応**: 全ての検索パラメーターがパイプライン入力に対応
- **見つからない場合の処理**: インストールされていないパッケージでもGist上の設定を更新
