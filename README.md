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

| コマンド | エイリアス | 分類 | Gist操作 | 説明 |
|----------|------------|------|----------|------|
| **パッケージ管理（GistGet独自実装）** |
| install | add | COM利用 | 更新 | パッケージインストール + Gist定義更新 |
| uninstall | remove, rm | COM利用 | 更新 | パッケージアンインストール + Gist定義更新 |
| upgrade | update | COM利用 | 更新 | パッケージアップグレード + Gist定義更新 |
| sync | - | COM利用 | 読込 | Gist定義パッケージをインストール（Gist→ローカル一方向同期） |
| download | - | COM利用 | 読込 | Gist定義ファイルのダウンロード |
| upload | - | COM利用 | 置換 | ローカルファイルのGistへのアップロード |
| **Gist管理コマンド（新規追加）** |
| gist set | - | 独立実装 | - | Gist情報設定（ID・ファイル名） |
| gist status | - | 独立実装 | - | Gist設定状態確認 |
| gist show | - | 独立実装 | 読込 | Gist内容表示 |
| gist clear | - | 独立実装 | - | Gist設定クリア |
| **情報表示（パススルー）** |
| list | ls | パススルー | - | インストール済み表示 |
| search | find | パススルー | - | パッケージ検索 |
| show | view | パススルー | - | パッケージ詳細表示 |
| **管理系（パススルー）** |
| source | - | パススルー | - | ソース管理 |
| settings | config | パススルー | - | 設定管理 |
| pin | - | パススルー | - | ローカルバージョン固定 |
| **認証管理（GistGet独自実装）** |
| login | - | 独立実装 | - | GitHubログイン・トークン管理 |
| configure | - | パススルー | - | システム構成 |
| download | - | パススルー | - | インストーラDL |
| repair | - | パススルー | - | パッケージ修復 |
| hash | - | パススルー | - | ハッシュ計算 |
| validate | - | パススルー | - | マニフェスト検証 |
| features | - | パススルー | - | 実験的機能 |
| export | - | パススルー | - | 現在の環境をファイルへ書き出し（winget passthrough） |
| import | - | パススルー | - | 定義ファイルからインストール（winget passthrough） |

---


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

## 開発者向け情報

### 開発環境のセットアップ

このプロジェクトに貢献する場合、以下の手順で開発環境をセットアップしてください：

#### 1. Pre-commitフックの設定

コード品質を保つため、コミット前に自動的にフォーマットチェックが実行されるようになっています：

```powershell
# 初回のみ実行
.\scripts\setup-hooks.ps1
```


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

### 7. トラブルシューティング

**WinGet COM API初期化エラー**：
```powershell
# Windows Package Managerの更新
winget upgrade Microsoft.AppInstaller
```
