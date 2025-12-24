# GistGet プロジェクト情報

## パッケージ情報

| 項目 | 値 |
|------|-----|
| PackageIdentifier | `NuitsJp.GistGet` |
| Publisher | `nuitsjp` |
| PackageName | `GistGet` |
| Moniker | `gistget` |
| License | MIT |

## リポジトリ構成

```
GistGet/
├── src/
│   └── GistGet/
│       └── GistGet.csproj      # Version定義
├── scripts/
│   └── Publish-WinGet.ps1      # リリースパイプライン
├── .github/workflows/
│   ├── ci.yml                   # CI/テスト
│   └── release.yml              # リリース自動化
├── external/
│   └── winget-pkgs/            # winget-pkgsサブモジュール
├── artifacts/                   # ビルド成果物
└── CHANGELOG.md                 # 変更履歴
```

## バージョン管理

- **定義場所**: `src/GistGet/GistGet.csproj` の `<Version>` タグ
- **タグ形式**: `v{major}.{minor}.{patch}` (例: `v1.0.5`)
- **変更履歴**: `CHANGELOG.md` (Keep a Changelog形式)

## リリース成果物

| ファイル | 説明 |
|----------|------|
| `GistGet-win-x64.zip` | Windows x64向け自己完結型ビルド |
| `SHA256SUMS.txt` | チェックサム |

## 既存スクリプト

### Publish-WinGet.ps1

完全なリリースパイプラインを実行するスクリプト。

```powershell
# フルリリース
.\scripts\Publish-WinGet.ps1 -Version 1.0.5

# プレビュー実行
.\scripts\Publish-WinGet.ps1 -Version 1.0.5 -DryRun

# 品質チェックをスキップ
.\scripts\Publish-WinGet.ps1 -Version 1.0.5 -SkipQualityCheck

# WinGet PRのみ作成
.\scripts\Publish-WinGet.ps1 -Version 1.0.5 -SkipQualityCheck -SkipGitHubRelease
```

**パラメータ**:
- `-Version`: リリースバージョン
- `-SkipQualityCheck`: 品質チェックをスキップ
- `-SkipGitHubRelease`: GitHub Release作成をスキップ
- `-SkipWinGetPR`: WinGet PR作成をスキップ
- `-SkipPRCreation`: マニフェストは作成するがPRはスキップ
- `-DryRun`: プレビュー実行
- `-Force`: 確認プロンプトをスキップ

### release.yml ワークフロー

タグプッシュ時に自動実行されるGitHub Actionsワークフロー。

**トリガー**: `v*.*.*` タグのプッシュ

**ジョブ構成**:
1. `ci` - テスト実行
2. `build` - win-x64ビルド、ZIPアーカイブ生成
3. `release` - GitHub Release作成
4. `winget-publish` - WinGet PR自動作成
5. `summary` - リリースサマリー出力

## winget-pkgs サブモジュール

```powershell
# 初期化
git submodule update --init external/winget-pkgs

# upstreamと同期
cd external/winget-pkgs
git fetch upstream master
git checkout master
git reset --hard upstream/master
```

## マニフェスト配置先

```
external/winget-pkgs/manifests/n/NuitsJp/GistGet/{VERSION}/
├── NuitsJp.GistGet.yaml
├── NuitsJp.GistGet.installer.yaml
└── NuitsJp.GistGet.locale.en-US.yaml
```
