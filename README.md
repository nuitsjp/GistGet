## GistGet - WinGet + Gist 同期ツール

### 0. 現在の実装状況（MVP Phase）

#### 🚧 MVP実装中
現在、最小限の動作確認を優先したMVP実装を進めています。

**実装状況:**
- [ ] Phase 1: パススルー実装（50行） - 作業中
- [ ] Phase 2: COMルーティング（200行） - 未着手
- [ ] Phase 3: Gistスタブ（250行） - 未着手

**動作確認済みコマンド:**
```bash
# Phase 1完了後に動作予定
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
| **Gist同期専用コマンド** |
| sync | - | COM利用 | 読込 | 要管理者 | 最高 | Gist定義パッケージをインストール（追加のみ） |
| export | - | COM利用 | 読込 | 不要 | 高 | Gistから定義ファイルをダウンロード |
| import | - | COM利用 | 作成 | 不要 | 高 | 現在の環境をGistへアップロード |
| **情報表示（パススルー）** |
| list | ls | パススルー | - | 不要 | 中 | インストール済み表示 |
| search | find | パススルー | - | 不要 | 低 | パッケージ検索 |
| show | view | パススルー | - | 不要 | 低 | パッケージ詳細表示 |
| **管理系（パススルー）** |
| source | - | パススルー | - | 要管理者 | 低 | ソース管理 |
| settings | config | パススルー | - | 不要 | 低 | 設定管理 |
| pin | - | パススルー | - | 不要 | 低 | ローカルバージョン固定 |
| **その他（パススルー）** |
| configure | - | パススルー | - | 要管理者 | 低 | システム構成 |
| download | - | パススルー | - | 不要 | 低 | インストーラDL |
| repair | - | パススルー | - | 要管理者 | 低 | パッケージ修復 |
| hash | - | パススルー | - | 不要 | 低 | ハッシュ計算 |
| validate | - | パススルー | - | 不要 | 低 | マニフェスト検証 |
| features | - | パススルー | - | 不要 | 低 | 実験的機能 |

#### B. Gist同期方式

| 同期方式 | 説明 | 対象コマンド |
|----------|------|-------------|
| **更新** | コマンド実行後、Gist定義を自動更新 | install, uninstall, upgrade |
| **作成** | 現在の環境からGist定義を作成・更新 | import |
| **読込** | Gistから定義を読み込み | export, sync |

#### C. 実装方針

- **COM利用**: Gist同期が必要なコマンドはCOM API経由で実装し、操作後に自動的にGist定義を更新
- **パススルー**: 表示系・管理系はwinget.exeへ引数をそのまま渡して実行
- **バージョン固定**: YAML定義内の`Version`フィールドで指定（Gist側で管理）
- **認証**: OAuth Device Flowで自動実装（トークン設定コマンドは不要）

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

#### C. GitHub認証（OAuth Device Flow）

1. `device_code` と `verification_uri` を取得し、ブラウザを起動
2. ユーザーが認証後、アプリは `access_token` をポーリングで取得
3. 以降は `Authorization: Bearer <token>` で Gist API 呼び出し
4. トークンは **Windows DPAPI** でユーザー領域に暗号化保存

---

### 4. 技術スタック・実装計画

#### A. 技術スタック

- **フレームワーク**: .NET 8（自己完結型）
- **COM API**: Microsoft.WindowsPackageManager.ComInterop
- **引数パーサー**: System.CommandLine
- **HTTP通信**: HttpClient（GitHub API用）
- **YAML処理**: YamlDotNet（Gist同期用）
- **暗号化**: Windows DPAPI（トークン保存用）

#### B. MVP実装優先順位

1. **Phase 1**: CLIパススルー基盤（1日）
2. **Phase 2**: export/import のCOM API実装（3日）
3. **Phase 3**: Gist同期機能（3日）
4. **Phase 4**: syncコマンド実装（2日）

#### C. 品質保証

- **テスト**: xUnit, Moq, Shouldly
- **CI/CD**: GitHub Actions
- **パッケージ化**: 自己完結型実行ファイル
