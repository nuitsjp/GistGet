- カスタムインストールパラメーターのサポート

### 背景
- 既存PowerShell版: https://github.com/nuitsjp/GistGet/tree/main/powershell
- PowerShell版の課題：PAT認証の煩雑さ、winget出力解析の脆弱性、配布の困難さ

## 🚀 実装開始前の事前調査タスク
- [ ] エラー処理の具体的なパターン
- [ ] PowerShell版で対応済みの特殊ケース
- [ ] ユーザーフィードバックから得られた改善点

### 📋 Task 2: wingetコマンド仕様の完全調査

**目的**: wingetの最新仕様を把握し、完全互換を保証

#### 調査コマンド
```bash
# ヘルプから全コマンドリスト取得
winget --help
winget install --help
winget uninstall --help
winget upgrade --help
winget list --help
winget search --help
winget show --help
winget source --help
winget export --help
winget import --help
winget settings --help

# 各コマンドの実行例と出力形式確認
winget list --format json
winget search vscode
winget show Microsoft.VisualStudioCode
```

#### 成果物
- [ ] `docs/SPEC.md` - 詳細なコマンド仕様書
- [ ] `docs/MIGRATION.md` - PowerShell版からの移行ガイド
- [ ] `docs/KNOWN_ISSUES.md` - 既知の課題と回避策

## 🏗 アーキテクチャ

### 技術スタック
- **言語**: C# 13
- **フレームワーク**: .NET 10
- **パッケージ情報取得**: Windows Package Manager COM API
- **GitHub連携**: Octokit.NET
- **認証**: Microsoft.Identity.Client (MSAL) - Device Flow
- **YAML処理**: YamlDotNet
- **認証情報管理**: Windows Credential Manager
- **配布**: winget

### プロジェクト構造
```
GistGet/
├── src/
│   ├── GistGet/
│   │   ├── Program.cs
│   │   ├── GistGet.csproj
│   │   ├── Commands/
│   │   │   ├── ICommand.cs
│   │   │   ├── SyncCommand.cs
│   │   │   ├── ExportCommand.cs
│   │   │   ├── ImportCommand.cs
│   │   │   └── PassthroughCommand.cs
│   │   ├── Services/
│   │   │   ├── IGistService.cs
│   │   │   ├── GistService.cs
│   │   │   ├── IPackageService.cs
│   │   │   ├── PackageService.cs
│   │   │   ├── IAuthService.cs
│   │   │   ├── AuthService.cs
│   │   │   └── ICredentialService.cs
│   │   ├── Models/
│   │   │   ├── Package.cs
│   │   │   ├── PackageList.cs
│   │   │   └── SyncResult.cs
│   │   └── Utils/
│   │       ├── YamlHelper.cs
│   │       ├── WinGetCOM.cs
│   │       └── ConsoleHelper.cs
│   └── GistGet.Tests/
├── docs/
│   ├── README.md
│   ├── PLAN.md (本文書)
│   ├── SPEC.md
│   ├── ARCHITECTURE.md
│   ├── USER_GUIDE.md
│   ├── MIGRATION.md
│   └── KNOWN_ISSUES.md
├── powershell/ (参考実装保存用)
└── .github/
    └── workflows/
        └── release.yml
```

## 📝 データモデル

### packages.yaml 構造
```yaml
# 通常インストール
Microsoft.PowerToys: 

# アンインストール指定
DeepL.DeepL:
  uninstall: true

# カスタムパラメーター付きインストール
Microsoft.VisualStudioCode:
  custom: /VERYSILENT /NORESTART /MERGETASKS=!runcode,addcontextmenufiles
```

### 内部データモデル
```csharp
public class Package
{
    public string Id { get; set; }
    public bool Uninstall { get; set; }
    public string Custom { get; set; }
    public string InstalledVersion { get; set; }
    public string AvailableVersion { get; set; }
}

public class SyncResult
{
    public List<Package> Installed { get; set; }
    public List<Package> Uninstalled { get; set; }
    public List<Package> Failed { get; set; }
    public List<string> Errors { get; set; }
}
```

## 🔧 コマンド仕様

### 基本コマンド（winget互換）
```bash
# wingetへのパススルー
gistget install <package-id> [options]
gistget uninstall <package-id> [options]
gistget upgrade <package-id> [options]
gistget list [options]
gistget search <query> [options]
gistget show <package-id> [options]
```

### 拡張コマンド
```bash
# Gist同期
gistget sync [--url <gist-url>]

# エクスポート（現在の状態をYAML出力）
gistget export [--output <file>]

# インポート（YAMLを読み込んでGistに保存）
gistget import <file> [--create-gist]

# 認証設定
gistget auth login
gistget auth logout
gistget auth status
```

## 🔐 認証フロー

### Device Flow認証
1. `gistget auth login` 実行
2. デバイスコードとURLを表示
3. ユーザーがブラウザでコード入力
4. アクセストークン取得
5. Windows Credential Managerに保存

### 必要なGitHubスコープ
- `gist`: Gistの読み書き

### 認証情報の管理
- **プライマリGist**: 個人用のデフォルトGist（認証必須）
- **一時的URL指定**: `--url`オプションで公開Gistを参照（認証不要）
- **トークン保存**: Windows Credential Manager使用
- **トークンキー**: `GistGet:GitHub:AccessToken`

## 📊 同期ロジック

### sync コマンドの処理フロー
```
1. 認証確認（URL指定時はスキップ可）
2. Gist（またはURL）からpackages.yaml取得
3. Windows Package Manager COM APIでローカル状態取得
- ✅ 複数端末間での同期成功率 95%以上
- ✅ カスタムパラメーター対応
- ✅ 認証の安全性（トークンの適切な保護）

### 非機能要件
- ✅ 起動時間 < 1秒
- ✅ 同期処理の明確な進捗表示
- ✅ エラー時の分かりやすいメッセージ
- ✅ wingetアップデートへの追従性

### 品質基準
- ✅ ユニットテストカバレッジ > 80%
- ✅ Gist形式の完全な後方互換性


**合計見積もり**: 約3週間

## 🔄 移行計画

### リポジトリ構成
1. 新ブランチ `csharp-version` 作成
2. PowerShell版を `powershell/` ディレクトリに保持（参考用）
3. この計画書を `docs/PLAN.md` として配置
4. 新実装開始

### ユーザー移行サポート
- 既存のGist形式（YAML）は完全互換
- 移行ガイドドキュメント提供
- PowerShell版は一定期間メンテナンス
- 明確なEOLアナウンス

## 📝 今後の検討事項

### 将来的な機能拡張（v2.0以降）
- 差分表示機能（--dry-run）
- 複数Gistプロファイル管理
- パッケージグループ化機能
- 依存関係の自動解決
- GUI版の開発
- macOS/Linux対応（Homebrew/apt統合）

### 技術的改善案
- キャッシュ機構の実装
- 非同期処理の最適化
- プラグインアーキテクチャ

## ⚠️ リスクと対策

| リスク | 影響度 | 対策 |
|--------|--------|------|
| COM API仕様変更 | 高 | フォールバック機構実装 |
| winget仕様変更 | 中 | バージョン検出と対応 |
| GitHub API制限 | 低 | レート制限対応 |
| 認証トークン漏洩 | 高 | 暗号化保存、定期更新促進 |
**文書バージョン**: 1.0
**作成日**: 2025-01-XX
**最終更新**: 2025-01-XX
### Phase 7: Refactoring & Testing
- [x] Remove PowerShell wrapper
- [x] Implement COM API for reading
- [x] Implement ProcessRunner for execution
- [x] Unit Tests (>95% coverage for logic)
- [x] Architectural Restructuring (Layered Architecture, CommandBuilder)

**承認者**: [プロジェクトオーナー]