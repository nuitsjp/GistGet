# Development Scripts

このディレクトリには、開発作業を支援するスクリプトが含まれています。

## Pre-commitフックの設定

Git commitの前にコードフォーマットとコード品質をチェックするフックを設定できます：

```powershell
.\scripts\setup-hooks.ps1
```

一度実行すると、以降のcommit時に自動的にフォーマットチェックが実行されます。

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
   - pre-commitフックが自動実行されます
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
