# Scripts Directory

このディレクトリには、開発環境セットアップとテスト関連のスクリプトが含まれています。

ビルド関連のスクリプトは `../build-scripts/` ディレクトリに移動しました。

## 開発ツール

- **`fix-code-issues.ps1`** - コード品質問題の自動修正
- **`setup-hooks.ps1`** - Git pre-commitフックの設定

## テスト・Sandbox環境

- **`setup-sandbox.ps1`** - Windows Sandbox環境の自動セットアップ（WinGet・.NET 8自動インストール）
- **`run-tests.ps1`** - Sandbox内での自動テスト実行（基本・対話・非対話モード対応）

### Sandbox環境の使用方法

1. **設定ファイル生成**：
   ```powershell
   # プロジェクトルートで実行
   .\sandbox-config.ps1
   ```

2. **Sandboxの起動**：
   ```powershell
   # 生成されたWSBファイルでSandboxを起動
   .\sandbox.wsb
   ```
   
   起動時に`setup-sandbox.ps1`が自動実行され、WinGetと.NET 8 Runtimeがインストールされます。

3. **テスト実行**：
   ```powershell
   # Sandbox内で管理者権限PowerShellを起動し実行
   cd C:\Scripts
   
   # 基本テストのみ（推奨）
   .\run-tests.ps1 -BasicOnly
   
   # 全テスト（対話形式）
   .\run-tests.ps1
   
   # 非対話モード
   .\run-tests.ps1 -SkipInteractive
   ```

## Pre-commitフックの設定

Git commitの前にコードフォーマットとコード品質をチェックするフックを設定できます：

```powershell
.\scripts\setup-hooks.ps1
```

一度実行すると、以降のcommit時に自動的にフォーマットチェックが実行されます。

**注意**: pre-commitフックは`src/`または`tests/`ディレクトリのファイルが変更された場合のみ実行されます。それ以外のファイル（ドキュメント、設定ファイルなど）のみの変更時はスキップされます。

## ビルドタスクについて

ビルド、テスト、フォーマットチェック、コード検査などは `../build-scripts/` ディレクトリに移動しました。
これらのタスクは Invoke-Build から実行するか、個別に実行できます：

```powershell
# ビルドタスクの実行
Invoke-Build FormatCheck  # フォーマットチェック
Invoke-Build FormatFix    # フォーマット修正
Invoke-Build CodeInspection  # コード検査

# 個別実行
.\build-scripts\Format.ps1 -CheckOnly  # フォーマットチェック
.\build-scripts\Format.ps1 -Fix        # フォーマット修正
.\build-scripts\CodeInspection.ps1     # コード検査
```

## 使用方法

1. **初回セットアップ**
   ```powershell
   # フックを有効化
   .\scripts\setup-hooks.ps1
   ```

2. **開発中のフォーマットチェック**
   ```powershell
   # チェックのみ（変更なし）
   .\build-scripts\Format.ps1 -CheckOnly
   
   # 問題があれば修正
   .\build-scripts\Format.ps1 -Fix
   ```

3. **コミット時**
   - pre-commitフックが自動実行されます（`src/`または`tests/`の変更がある場合のみ）
   - フォーマット問題があればcommitが中断されます
   - `.\build-scripts\Format.ps1 -Fix`で修正してから再度commit

## トラブルシューティング

### フックが動作しない場合
- PowerShellの実行ポリシーを確認：`Get-ExecutionPolicy`
- 必要に応じて変更：`Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser`

### dotnet-formatが見つからない場合
- 手動インストール：`dotnet tool install -g dotnet-format`

### 権限エラーが発生する場合
- 管理者権限でPowerShellを実行してセットアップ
