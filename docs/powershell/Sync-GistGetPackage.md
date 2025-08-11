# Sync-GistGetPackage

## 概要
Gistの定義に基づいてパッケージを同期（インストール/アンインストール）する関数です。

## 関連ソースファイル
- **関数実装**: [Sync-GistGetPackage.ps1](../../powershell/src/Public/Sync-GistGetPackage.ps1)
- **テストファイル**: [Sync-GistGetPackage.Tests.ps1](../../powershell/test/Public/Sync-GistGetPackage.Tests.ps1)
- **ドキュメント**: [Sync-GistGetPackage.md](../../powershell/docs/en-us/Sync-GistGetPackage.md)

## 基本動作フロー

1. **パッケージ設定の取得**
   - デフォルト: `Get-GistFile()`で取得したGistから設定を読込
   - オプション: `Uri`または`Path`パラメーターで指定された場所から設定を読込

2. **インストール済みパッケージの取得**
   - `Get-WinGetPackage`でインストール済みパッケージのIDを辞書形式で取得

3. **同期処理**
   各Gist定義パッケージに対して以下を実行：
   
   - **Uninstallフラグがtrueの場合**:
     - インストール済み: `Uninstall-WinGetPackage`を実行
     - 未インストール: メッセージ表示のみ
   
   - **Uninstallフラグがfalseまたは未設定の場合**:
     - インストール済み: メッセージ表示のみ（何もしない）
     - 未インストール: `Install-WinGetPackage`を実行

4. **結果の表示**
   - インストールしたパッケージのリストを表示
   - アンインストールしたパッケージのリストを表示

5. **再起動処理**
   - 再起動が必要なパッケージがある場合はユーザーに確認
   - 確認後に`Restart-Computer -Force`を実行

## パラメーター仕様
- **Uri**: カスタムGist URLの指定（オプション）
- **Path**: ローカルファイルパスの指定（オプション）

## エラーハンドリング
- **設定取得エラー**: Gist、URI、またはファイルからの設定取得に失敗した場合は例外をスロー
- **パッケージ取得エラー**: インストール済みパッケージの取得に失敗した場合は例外をスロー
- **インストールエラー**: 個別パッケージのインストール失敗時はエラー表示後に次のパッケージへ継続
- **アンインストールエラー**: 個別パッケージのアンインストール失敗時はエラー表示後に次のパッケージへ継続

## 依存関数
- **Get-GistFile()**: デフォルトGist情報取得
- **Get-GistGetPackage()**: パッケージ設定の取得（Gist、URI、ファイル対応）
- **Get-WinGetPackage**: インストール済みパッケージの取得（Microsoft.WinGet.Client）
- **Install-WinGetPackage**: パッケージインストール（Microsoft.WinGet.Client）
- **Uninstall-WinGetPackage**: パッケージアンインストール（Microsoft.WinGet.Client）
- **Restart-Computer**: システム再起動

## 特記事項
- **追加のみ同期**: 既存パッケージは削除せず、定義に基づく追加のみ実行
- **設定ソースの柔軟性**: Gist、URL、ローカルファイルの3つのソースに対応
- **冪等性**: 既にインストール済みのパッケージは再インストールしない
