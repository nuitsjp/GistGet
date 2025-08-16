# Scripts Directory

このディレクトリには、開発・テスト・ビルドに関連するすべてのスクリプトが含まれています。

## 開発ツール

- **`format-check.ps1`** - コードフォーマットチェック
- **`format-fix.ps1`** - コードフォーマット自動修正
- **`fix-code-issues.ps1`** - コード品質問題の修正
- **`setup-hooks.ps1`** - Git pre-commitフックの設定
- **`run-code-inspection.ps1`** - コード検査の実行

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

## 手動でのフォーマットチェック

CI/CDと同じフォーマットチェックをローカルで実行：

```powershell
.\scripts\format-check.ps1
```

## フォーマット問題の修正

フォーマット問題を自動的に修正：

```powershell
.\scripts\format-fix.ps1
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
   .\scripts\format-check.ps1
   
   # 問題があれば修正
   .\scripts\format-fix.ps1
   ```

3. **コミット時**
   - pre-commitフックが自動実行されます（`src/`または`tests/`の変更がある場合のみ）
   - フォーマット問題があればcommitが中断されます
   - `.\scripts\format-fix.ps1`で修正してから再度commit

## トラブルシューティング

### フックが動作しない場合
- PowerShellの実行ポリシーを確認：`Get-ExecutionPolicy`
- 必要に応じて変更：`Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser`

### dotnet-formatが見つからない場合
- 手動インストール：`dotnet tool install -g dotnet-format`

### 権限エラーが発生する場合
- 管理者権限でPowerShellを実行してセットアップ
