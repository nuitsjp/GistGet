# Build Scripts Directory

このディレクトリには、ビルドプロセス関連のスクリプトが含まれています。

各スクリプトは個別に実行することも、Invoke-Build タスクから実行することもできます。

## 利用可能なスクリプト

### Setup.ps1
必要な開発ツールをインストールします。

```powershell
.\build-scripts\Setup.ps1
# または 
Invoke-Build Setup
```

インストールされるツール：
- JetBrains.ReSharper.GlobalTools
- dotnet-reportgenerator-globaltool  
- dotnet-format

### Build.ps1
ソリューションをビルドします。

```powershell
.\build-scripts\Build.ps1 -Configuration Release -Verbosity minimal
# または
Invoke-Build Build
```

### Test.ps1
テストを実行します。

```powershell
.\build-scripts\Test.ps1 -Configuration Release -CollectCoverage
# または
Invoke-Build Test
```

### Coverage.ps1
カバレッジレポートを生成します。

```powershell
.\build-scripts\Coverage.ps1 -ReportDirectory "coverage-report" -ShowSummary
# または
Invoke-Build Coverage
```

### CodeInspection.ps1
ReSharperを使用してコード検査を実行します。

```powershell
.\build-scripts\CodeInspection.ps1 -ShowSummary
# または
Invoke-Build CodeInspection
```

### Format.ps1
コードフォーマットをチェックまたは修正します。

```powershell
# フォーマットチェック（変更なし）
.\build-scripts\Format.ps1 -CheckOnly
# または
Invoke-Build FormatCheck

# フォーマット修正
.\build-scripts\Format.ps1 -Fix
# または
Invoke-Build FormatFix
```

### Clean.ps1
ビルド成果物をクリーンアップします。

```powershell
.\build-scripts\Clean.ps1 -IncludeCoverage -IncludeInspection
# または
Invoke-Build Clean
```

## 典型的な使用例

### 開発中の基本チェック
```powershell
# フォーマットチェック
Invoke-Build FormatCheck

# ビルドとテスト
Invoke-Build

# コード品質チェック
Invoke-Build CodeInspection
```

### CI/CDパイプライン
```powershell
# 完全なビルドプロセス
Invoke-Build Full
```

### 初回セットアップ
```powershell
# 開発環境のセットアップ
Invoke-Build Setup
```

## エラーハンドリング

各スクリプトは適切なエラーハンドリングを含んでおり、失敗時には非ゼロの終了コードを返します。
これにより、CI/CDパイプラインでの使用に適しています。

## カスタマイズ

各スクリプトはパラメーターを受け取るため、異なる設定で実行できます。
詳細なパラメーター情報については、各スクリプトファイルを参照してください。