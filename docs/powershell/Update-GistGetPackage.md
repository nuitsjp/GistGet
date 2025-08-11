# Update-GistGetPackage

## 概要
インストール済みパッケージのアップデートを管理し、Gistの設定に基づいてバージョン制御を行う関数です。

## 関連ソースファイル
- **関数実装**: [Update-GistGetPackage.ps1](../../powershell/src/Public/Update-GistGetPackage.ps1)
- **テストファイル**: [Update-GistGetPackage.Tests.ps1](../../powershell/test/Public/Update-GistGetPackage.Tests.ps1)
- **ドキュメント**: [Update-GistGetPackage.md](../../powershell/docs/en-us/Update-GistGetPackage.md)

## 基本動作フロー

1. **Gist情報の取得**
   - `Get-GistGetPackage()`を呼び出してパッケージリストを取得

2. **アップデート可能パッケージの取得**
   - `Get-WinGetPackage | Where-Object { $_.IsUpdateAvailable }`でアップデート可能なパッケージを取得

3. **各パッケージのアップデート判定**
   各アップデート可能パッケージに対して以下のロジックを実行：
   
   - **Gistに該当パッケージがある場合**:
     - **バージョン指定あり**: インストール済みバージョンとGistバージョンを比較
       - 同一の場合: アップデートしない
       - 異なる場合: `Confirm-ReplacePackage`で確認後、アンインストール→インストール
     - **バージョン指定なし**: 無条件でアップデート
   
   - **Gistに該当パッケージがない場合**: 無条件でアップデート

4. **アップデート実行**
   - 条件を満たすパッケージに対して`Update-WinGetPackage`を実行
   - 再起動が必要な場合は配列に記録

5. **再起動処理**
   - 再起動が必要なパッケージがある場合は`Confirm-Reboot`でユーザーに確認
   - ユーザーが承認した場合は`Restart-Computer -Force`を実行

## パラメーター仕様
- パラメーターなし（全自動処理）

## エラーハンドリング
- **Gist取得エラー**: パッケージリストの取得に失敗した場合は例外をスロー
- **パッケージ取得エラー**: インストール済みパッケージの取得に失敗した場合は例外をスロー
- **アップデートエラー**: 個別パッケージのアップデート失敗時はエラー表示後に次のパッケージへ継続
- **再インストールエラー**: バージョン不一致による再インストール失敗時はエラー表示

## 依存関数
- **Get-GistGetPackage()**: Gistからパッケージリストを取得
- **Get-WinGetPackage**: インストール済みパッケージとアップデート可能性を取得
- **Confirm-ReplacePackage()**: バージョン不一致時のユーザー確認
- **Uninstall-WinGetPackage**: パッケージアンインストール（Microsoft.WinGet.Client）
- **Install-WinGetPackage**: パッケージインストール（Microsoft.WinGet.Client）
- **Update-WinGetPackage**: パッケージアップデート（Microsoft.WinGet.Client）
- **Confirm-Reboot()**: 再起動確認UI
- **Restart-Computer**: システム再起動

## 特記事項
- **バージョン制御**: Gistの設定に基づいて特定バージョンに固定可能
- **ユーザー確認**: バージョン不一致時はユーザーに置き換え確認を要求
- **再起動管理**: 複数パッケージの再起動要求を統合して処理
