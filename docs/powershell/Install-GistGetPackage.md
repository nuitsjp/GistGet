# Install-GistGetPackage仕様調査

## 概要
WinGetパッケージを検索・インストールし、そのパッケージ情報をGistに記録する統合的なパッケージ管理関数です。

## 関連ソースファイル
- **関数実装**: [Install-GistGetPackage.ps1](../../powershell/src/Public/Install-GistGetPackage.ps1)
- **テストファイル**: [Install-GistGetPackage.Tests.ps1](../../powershell/test/Public/Install-GistGetPackage.Tests.ps1)
- **ドキュメント**: [Install-GistGetPackage.md](../../powershell/docs/en-us/Install-GistGetPackage.md)

## 基本動作フロー

1. **前提条件チェック**
   - Microsoft.WinGet.Clientモジュールの存在確認
   - モジュールが存在しない場合は例外をスロー

2. **パラメーター分離**
   関数は受け取ったパラメーターを以下の2つのカテゴリに分離します：
   - **Find-WinGetPackage用**: Query, Command, Count, Id, MatchOption, Moniker, Name, Source, Tag
   - **Install-WinGetPackage用**: AllowHashMismatch, Architecture, Custom, Force, Header, InstallerType, Locale, Location, Log, Mode, Override, Scope, SkipDependencies, Version

3. **Gist情報の取得**
   - `Get-GistFile()`を呼び出してGist情報を取得
   - `Get-GistGetPackage()`を呼び出して既存のパッケージリストを取得

4. **パッケージ検索**
   - `Find-WinGetPackage`を実行してパッケージを検索
   - `Id`パラメーターが指定されている場合、完全一致するパッケージのみを抽出
   - 該当パッケージが見つからない場合は警告を表示して終了

5. **インストール確認とパッケージ情報表示**
   - 複数パッケージが見つかった場合は警告として一覧を表示
   - `ShouldProcess`を使用してインストール確認を実行

6. **パッケージインストール処理**
   各パッケージに対して以下を実行：
   - **パッケージインストール**: `Install-WinGetPackage`を実行し、インストール結果から再起動要求フラグをチェック
   - **Gistパッケージリストの更新**: 既存の同一IDのパッケージを削除後、新しい設定でパッケージを追加

7. **Gist更新**
   - パッケージが追加された場合は`Set-GistGetPackages`を呼び出してGistを更新

8. **再起動処理**
   - 再起動が必要なパッケージがある場合は`Confirm-Reboot`でユーザーに確認
   - ユーザーが承認した場合は`Restart-Computer -Force`を実行

## パラメーター仕様

### 検索パラメーター（Query ParameterSet）
- **Query**: パッケージ検索文字列配列
- **Id**: パッケージID（完全一致検索）
- **MatchOption**: 検索マッチオプション（Equals, EqualsCaseInsensitive等）
- **Moniker**: パッケージモニカー
- **Name**: パッケージ名
- **Source**: WinGetソース

### インストールパラメーター
- **AllowHashMismatch**: ハッシュ不一致を許可
- **Architecture**: プロセッサアーキテクチャ（Default, X86, Arm, X64, Arm64）
- **Custom**: インストーラーへの追加引数
- **Force**: 通常チェックをスキップして強制実行
- **Header**: WinGet RESTソースへのカスタムHTTPヘッダー
- **InstallerType**: インストーラータイプ（Default, Inno, Wix等）
- **Locale**: インストーラーロケール（BCP47形式）
- **Location**: インストールパス
- **Log**: インストーラーログファイルパス
- **Mode**: インストーラー実行モード（Default, Silent, Interactive）
- **Override**: インストーラーの既存引数を上書き
- **Scope**: インストールスコープ（Any, User, System等）
- **SkipDependencies**: 依存関係のインストールをスキップ
- **Version**: インストールするパッケージバージョン

## エラーハンドリング
- **前提条件エラー**: Microsoft.WinGet.Clientモジュール未インストール時に例外スロー
- **パッケージ検索エラー**: 該当パッケージなしの場合は警告表示後に正常終了
- **インストールエラー**: 個別パッケージのインストール失敗時はエラー表示後に次のパッケージへ継続

## ShouldProcess サポート
- `-WhatIf`: 実際のインストールを行わずに動作をシミュレート
- `-Confirm`: インストール前に確認プロンプトを表示

## 依存関数
- **Get-GistFile()**: Gist情報取得
- **Get-GistGetPackage()**: 既存パッケージリスト取得
- **Find-WinGetPackage**: パッケージ検索（Microsoft.WinGet.Client）
- **Install-WinGetPackage**: パッケージインストール（Microsoft.WinGet.Client）
- **Set-GistGetPackages()**: Gistパッケージリスト更新
- **Confirm-Reboot()**: 再起動確認UI
- **Restart-Computer**: システム再起動

## 特記事項
- **パッケージ重複処理**: 既存の同一IDパッケージは削除後に新しい設定で追加されるため、パッケージの設定更新が可能
- **再起動要求処理**: WinGetからの再起動要求を適切にハンドリングし、ユーザーの明示的な承認後に実行
- **パラメーター変換**: switch型パラメーターは適切にboolean値に変換され、null値は除外されてGistに保存
