## GistGet - WinGet + Gist 同期ツール

### 0. 現在の実装状況（MVP Phase）

#### 🚧 MVP実装中
現在、最小限の動作確認を優先したMVP実装を進めています。

**実装状況:**
- [x] Phase 1: パススルー実装 - ✅完了
- [ ] Phase 2: COMルーティング - 作業中
- [x] Phase 3: Gistスタブ - スタブ作成済み

**現在のバージョン:** v0.1.0-alpha

**動作確認済みコマンド:**
```bash
# Phase 1完了・動作確認済み
gistget list            # → winget list
gistget search git      # → winget search git
gistget show --id Git.Git  # → winget show --id Git.Git
```

---

### 1. 概要

**課題**: WinGet を利用し、異なる環境間でパッケージを同期したいが標準でサポートしていない  
**目的**: .NET 8 + WinGet COM API でハイブリッド型同期ツールを構築

### 2. コマンド仕様

#### A. 対応コマンド一覧

| コマンド | エイリアス | 分類 | Gist同期 | 権限 | 優先度 | 説明 |
|----------|------------|------|----------|------|--------|------|
| **パッケージ管理（GistGet独自実装）** |
| install | add | COM利用 | 更新 | 要管理者 | 最高 | パッケージインストール + Gist定義更新 |
| uninstall | remove, rm | COM利用 | 更新 | 要管理者 | 最高 | パッケージアンインストール + Gist定義更新 |
| upgrade | update | COM利用 | 更新 | 要管理者 | 最高 | パッケージアップグレード + Gist定義更新 |
| sync | - | COM利用 | 読込 | 要管理者 | 最高 | Gist定義パッケージをインストール（追加のみ） |
| **Gist管理コマンド（新規追加）** |
| gist set | - | 独立実装 | - | 不要 | 最高 | Gist情報設定（ID・ファイル名） |
| gist status | - | 独立実装 | - | 不要 | 最高 | Gist設定状態確認 |
| gist show | - | 独立実装 | 読込 | 不要 | 高 | Gist内容表示 |
| gist clear | - | 独立実装 | - | 不要 | 中 | Gist設定クリア |
| **Gist同期専用コマンド** |
| **情報表示（パススルー）** |
| list | ls | パススルー | - | 不要 | 中 | インストール済み表示 |
| search | find | パススルー | - | 不要 | 低 | パッケージ検索 |
| show | view | パススルー | - | 不要 | 低 | パッケージ詳細表示 |
| **管理系（パススルー）** |
| source | - | パススルー | - | 要管理者 | 低 | ソース管理 |
| settings | config | パススルー | - | 不要 | 低 | 設定管理 |
| pin | - | パススルー | - | 不要 | 低 | ローカルバージョン固定 |
| **認証管理（GistGet独自実装）** |
| login | - | 独立実装 | - | 不要 | 最高 | GitHubログイン・トークン管理 |
| configure | - | パススルー | - | 要管理者 | 低 | システム構成 |
| download | - | パススルー | - | 不要 | 低 | インストーラDL |
| repair | - | パススルー | - | 要管理者 | 低 | パッケージ修復 |
| hash | - | パススルー | - | 不要 | 低 | ハッシュ計算 |
| validate | - | パススルー | - | 不要 | 低 | マニフェスト検証 |
| features | - | パススルー | - | 不要 | 低 | 実験的機能 |
| export | - | パススルー | 作成 | 不要 | 高 | 現在の環境をファイルへ書き出し（winget passthrough） |
| import | - | パススルー | 読込 | 不要 | 高 | 定義ファイルからインストール（winget passthrough） |

#### B. Gist同期方式

| 同期方式 | 説明 | 対象コマンド |
|----------|------|-------------|
| **更新** | コマンド実行後、Gist定義を自動更新 | install, uninstall, upgrade |
| **作成** | 現在の環境からGist定義を作成・更新 | import |
| **読込** | Gistから定義を読み込み | export, sync |

#### C. 実装方針

- **COM利用**: Gist同期が必要なコマンド（install/uninstall/upgrade）はCOM API経由で実装し、操作後に自動的にGist定義を更新
- **パススルー**: 表示系・管理系はwinget.exeへ引数をそのまま渡して実行
- **バージョン固定**: YAML定義内の`Version`フィールドで指定（Gist側で管理）
- **認証**: ローカルは OAuth Device Flow、CI は環境変数 `GIST_TOKEN`。手動でトークンを貼り付ける操作は不要。`login` コマンドで認証。
- **Gist管理**: 事前設定前提（`gistget gist set`でGist ID・ファイル名を設定）
- **自動認証・設定**: 認証・Gist設定が必要なコマンド実行時、未設定の場合は自動的に認証・設定フローを実行（CommandRouterで一元管理）

---

### 3. アーキテクチャ概要

#### A. ハイブリッドアーキテクチャ

```
┌─────────────────────────────────────────┐
│            CLI Interface                 │
├─────────────────────────────────────────┤
│         Command Router                   │
├──────────────┬──────────────────────────┤
│  COM利用     │    パススルー             │
│  (Gist同期)  │    (表示・管理系)         │
├──────────────┼──────────────────────────┤
│ WinGet COM   │   WinGet CLI             │
│   Client     │    Passthrough           │
├──────────────┼──────────────────────────┤
│ COM API      │   winget.exe             │
└──────────────┴──────────────────────────┘
```

#### B. Gist同期データ形式（PowerShell版準拠）

```yaml
# packages.yaml
Packages:
  - Id: Microsoft.VisualStudioCode
    Version: 1.85.0  # バージョン固定（省略可）
  - Id: Git.Git
  - Id: Microsoft.PowerToys
    Version: 0.76.0
```

#### C. GitHub認証（多層認証戦略）

**ローカル開発（推奨）:**
```bash
# 初回のみログインが必要
gistget login

# ログイン状態の確認
gistget login status
```

**Gist設定（ローカル開発・テスト用）:**
```bash
# GitHubでGist作成が必要（初回のみ）
# 1. https://gist.github.com/ でGist作成
# 2. packages.yaml ファイルを作成
# 3. 基本的なパッケージ定義を記述

# 対話形式でGist情報設定（推奨）
gistget gist set

# またはコマンドライン引数で直接設定
gistget gist set --gist-id abc123456789 --file packages.yaml

# Gist設定状態の確認
gistget gist status

# Gist内容の確認（設定テスト）
gistget gist show
```

**初回セットアップの完全な手順:**
```bash
# 1. GitHubログイン
gistget login

# 2. GitHubでGist作成（ブラウザ）
# https://gist.github.com/ にアクセス
# ファイル名: packages.yaml
# 内容例:
# Packages:
#   - Id: Git.Git
#   - Id: Microsoft.VisualStudioCode

# 3. Gist設定（GistGet側）
gistget gist set  # 対話形式で案内

# 4. 設定確認
gistget gist status
gistget gist show
```

**CI/CD環境:**
```yaml
# GitHub Secretsに設定（CIはデバイスフローを使わずトークンを使用）
env:
  GIST_TOKEN: ${{ secrets.GIST_TOKEN }}
```

**テスト環境:**
- ユニットテスト: モック認証を自動使用
- 統合テスト: `GIST_TOKEN`環境変数または事前認証要求

**認証フロー:**
1. ローカル開発: `gistget login` で認証（一度のみ）、または必要時に自動実行
2. CI/CD: 認証不要（ビルドのみ実行）
3. テスト: ローカル環境で手動実行

**自動認証・設定の動作:**
- 認証が必要なコマンド（sync, install, uninstall, upgrade, gist）実行時、未認証であれば自動的にログイン画面を表示
- Gist設定が必要なコマンド実行時、未設定であれば自動的にGist設定フローを実行
- CommandRouterレベルでの一元管理により、すべてのコマンドで統一的な動作を保証

---

### 4. 技術スタック・実装計画

#### A. 技術スタック

- **フレームワーク**: .NET 8（自己完結型）
- **COM API**: Microsoft.WindowsPackageManager.ComInterop
- **引数パーサー**: System.CommandLine
- **HTTP通信**: HttpClient（GitHub API用）
- **YAML処理**: YamlDotNet（Gist同期用）
- **暗号化**: Windows DPAPI（Gist設定・トークン保存用）

#### B. MVP実装優先順位

1. **Phase 1**: CLIパススルー基盤（1日）
2. **Phase 2**: export/import のCOM API実装（3日）
3. **Phase 3**: Gist同期機能（3日）
4. **Phase 4**: syncコマンド実装（2日）

#### C. 品質保証

- **テスト**: xUnit, Moq, Shouldly
- **CI/CD**: GitHub Actions
- **パッケージ化**: 自己完結型実行ファイル

### 5. CI/CD環境での課題と解決策

#### A. 主要な課題（更新版）

| 課題 | 影響 | 解決策 | 優先度 |
|------|------|--------|--------|
| **Linux CI環境** | Windows機能テスト不可 | コア機能とWindows機能を分離 | 高 |
| **認証の自動化** | Gist API呼び出し | GitHub Secretsでトークン管理 | 高 |
| **WinGet依存** | Linux環境で実行不可 | モック実装・条件付きコンパイル | 中 |
| **管理者権限** | CI環境で制限 | 権限不要テストの分離 | 中 |

#### C. GitHub Actions設定（ビルドのみ戦略）

```yaml
# CI/CDはビルド検証のみ実施
# 実際のテストはローカル開発環境で実施

name: Build
on: [push, pull_request]

jobs:
  # クロスプラットフォームビルド検証
  build:
    runs-on: windows-latest
    
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      
      - name: Restore dependencies
        run: dotnet restore
      
      - name: Build
        run: dotnet build --configuration Release --no-restore
      
      - name: Test (Unit only)
        run: dotnet test --filter "Category=Unit" --no-build --configuration Release
```

#### D. テスト戦略（ローカル開発重視）

```csharp
// テストカテゴリによる分離
[Fact]
[Trait("Category", "Unit")]  // CI実行可能
public void ArgumentParsing_ShouldWork() { }

[Fact]
[Trait("Category", "Local")]  // ローカルでのみ実行
public async Task GistSync_ShouldWork() { }

[Fact]
[Trait("Category", "Manual")]  // 手動検証が必要
public void InstallCommand_RequiresManualVerification() { }
```

#### E. ローカル開発でのテスト実行

```bash
# CI相当（ユニットテストのみ）
dotnet test --filter "Category=Unit"

# ローカル統合テスト
dotnet test --filter "Category=Local"

# 手動検証が必要なテスト（実行後に手動確認）
dotnet test --filter "Category=Manual"

# 全テスト実行
dotnet test
```

#### D. リリースパイプライン（ビルドのみ）

```yaml
# .github/workflows/release.yml
name: Release
on:
  push:
    tags: ['v*']

jobs:
  release:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      
      - name: Build Release
        run: |
          dotnet publish -c Release -r win-x64 \
            --self-contained -p:PublishSingleFile=true
      
      - name: Create Release
        uses: softprops/action-gh-release@v1
        with:
          files: |
            bin/Release/net8.0-windows10.0.26100/win-x64/publish/GistGet.exe
```

#### E. 品質保証戦略

- **CI/CD**: ビルド検証とユニットテストのみ
- **ローカル開発**: 統合テストと手動検証
- **リリース前**: 手動での総合テスト実施

### 6. 開発ロードマップ

#### バージョニング戦略
- **v0.x.x**: アルファ版（開発中）
- **v1.0.0-rc.x**: リリース候補版
- **v1.0.0**: 最初の正式リリース（目標: 2024 Q2）

#### Phase 1: MVP (v0.1.0 - v0.3.0)
- ✅ パススルー実装
- ⏳ COM APIルーティング
- ⏳ 基本的なGist読み込み

#### Phase 2: 認証とCI/CD (v0.4.0 - v0.6.0)
- [ ] OAuth Device Flow（Windows）
- [ ] GitHub Actions設定（Linux主体）
- [ ] 自動テスト整備

#### Phase 3: 完全なGist同期 (v0.7.0 - v0.9.0)
- [ ] 双方向同期
- [ ] 競合解決
- [ ] バージョン管理

#### Phase 4: 正式リリース準備 (v1.0.0-rc.x)
- [ ] エラーメッセージ改善
- [ ] プログレス表示
- [ ] ドキュメント完成
- [ ] パフォーマンス最適化

#### Phase 5: 正式リリース (v1.0.0)
- [ ] 安定版リリース
- [ ] WinGetマニフェスト作成
- [ ] 自動アップデート機能

---

## 開発者向け情報

### 開発環境のセットアップ

このプロジェクトに貢献する場合、以下の手順で開発環境をセットアップしてください：

#### 1. Pre-commitフックの設定

コード品質を保つため、コミット前に自動的にフォーマットチェックが実行されるようになっています：

```powershell
# 初回のみ実行
.\scripts\setup-hooks.ps1
```

#### 2. コードフォーマット

コミット前にフォーマットチェックを手動実行：

```powershell
# フォーマットチェック（変更なし）
.\scripts\format-check.ps1

# フォーマット問題の自動修正
.\scripts\format-fix.ps1
```

#### 3. 開発ワークフロー

1. 新しい機能開発やバグ修正の前に、必ずフォーマットが整っていることを確認
2. コード変更後、`.\scripts\format-check.ps1`でフォーマットをチェック
3. 問題があれば`.\scripts\format-fix.ps1`で修正
4. `git commit`時に自動的にフォーマットチェックが実行される
5. フォーマット問題があればcommitが中断されるので、修正してから再度commit

### テスト・ビルド要件

- .NET 8.0 SDK
- PowerShell 5.1以上
- Windows 10/11（WinGet COM API利用のため）
- 管理者権限（一部テストで必要）

詳細は`scripts/README.md`を参照してください。

## Windows Sandboxでのインストールテスト

WinGet配布前の最終検証として、Windows Sandboxでの動作テストを実施することを推奨します。

### 1. Windows Sandboxの有効化

```powershell
# 管理者権限でPowerShellを実行
Enable-WindowsOptionalFeature -Online -FeatureName "Containers-DisposableClientVM" -All
```

再起動後、Windows Sandboxが利用可能になります。

### 2. テスト用構成ファイルの生成

Windows Sandboxの設定ファイルを現在の環境に合わせて生成します：

```powershell
# 現在の環境用のsandbox.wsbを生成
.\sandbox-config.ps1

# カスタムパスで生成する場合
.\sandbox-config.ps1 -ProjectRoot "C:\YourPath\GistGet" -OutputFile "my-sandbox.wsb"
```

このスクリプトは以下の機能を提供します：
- **環境依存パスの自動解決**（開発環境に依存しない）
- **WinGetの自動インストール**（GitHub最新版）
- **.NET 8 Runtimeの自動インストール**
- **GistGetテスト環境の自動セットアップ**
- **統合されたスクリプト管理**（`scripts`フォルダに集約）

### 3. Sandboxでのテスト手順

1. **設定ファイル生成とSandboxの起動**：
   ```powershell
   # 1. 設定ファイル生成
   .\sandbox-config.ps1
   
   # 2. Sandboxの起動
   .\sandbox.wsb
   ```
   
   起動時に自動的に以下が実行されます：
   - WinGetの最新版インストール
   - .NET 8 Runtimeのインストール
   - 環境セットアップの確認
   - テスト環境準備完了の通知

2. **自動テストの実行**：
   ```powershell
   # Sandbox内でPowerShellを管理者権限で実行
   cd C:\Scripts
   
   # 基本テストのみ実行（推奨）
   .\run-tests.ps1 -BasicOnly
   
   # 全テスト実行（対話的）
   .\run-tests.ps1
   
   # 非対話モードでの全テスト
   .\run-tests.ps1 -SkipInteractive
   ```

3. **手動での動作確認**：
   ```powershell
   # Sandbox内でPowerShellを管理者権限で実行
   cd C:\GistGet
   
   # 基本動作確認
   .\GistGet.exe --help
   .\GistGet.exe list
   .\GistGet.exe search git
   
   # ログイン機能テスト（対話形式）
   .\GistGet.exe login
   
   # Gist設定テスト
   .\GistGet.exe gist status
   .\GistGet.exe gist set
   
   # サイレントモードテスト
   .\GistGet.exe --silent install Git.Git
   ```

4. **WinGetマニフェストテスト**：
   ```powershell
   # ローカルマニフェストでインストールテスト
   winget install --manifest C:\Manifests\NuitsJp\GistGet\1.0.0\
   
   # インストール後の動作確認
   gistget --help
   gistget list
   ```

### 5. テスト項目チェックリスト

- [ ] 基本コマンド動作（help, list, search）
- [ ] 認証フロー（GitHub OAuth Device Flow）
- [ ] Gist設定（対話形式、コマンドライン引数）
- [ ] サイレントモード（--silent フラグ）
- [ ] エラーハンドリング（認証失敗、ネットワークエラー）
- [ ] WinGetマニフェスト経由でのインストール
- [ ] アンインストール動作
- [ ] ファイルサイズ・起動速度の確認

### 6. 注意事項

- **環境依存の解決**：`sandbox-config.ps1`で現在の環境に合わせたWSBファイルを生成してください
- **管理者権限**：WinGet操作には管理者権限が必要です
- **ネットワーク**：GitHub APIアクセスとWinGet自動インストールのためインターネット接続が必要です
- **初回セットアップ時間**：WinGetと.NET 8の自動インストールに数分かかる場合があります
- **クリーンな環境**：Sandboxは毎回初期状態からスタートするため、実際のユーザー環境に近いテストが可能です
- **統合されたスクリプト**：すべてのスクリプトが`scripts`フォルダに集約されています
- **自動テストスクリプト**：`C:\Scripts\run-tests.ps1`で段階的なテストが実行できます
- **ログの確認**：エラーが発生した場合は、Windowsイベントログやアプリケーションログを確認してください

### 7. トラブルシューティング

**WinGet COM API初期化エラー**：
```powershell
# Windows Package Managerの更新
winget upgrade Microsoft.AppInstaller
```

**認証エラー**：
```powershell
# プロキシ設定の確認（企業環境の場合）
netsh winhttp show proxy
```

**ファイルアクセスエラー**：
```powershell
# 実行ファイルのプロパティで「ブロックの解除」を確認
Unblock-File C:\GistGet\GistGet.exe
```
