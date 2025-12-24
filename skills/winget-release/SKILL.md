---
name: winget-release
description: GistGetのWinGetパッケージリリースを支援。(1) 新バージョンのリリース、(2) WinGetマニフェスト作成・更新、(3) winget-pkgsへのPR作成。「WinGetにリリース」「winget-pkgsにPR」「マニフェスト作成」などのキーワードで使用。
---

# GistGet WinGet リリース

GistGetをWinGetパッケージとしてリリースするためのスキル。

## クイックスタート

### 自動リリース（推奨）

```powershell
# フルリリースパイプライン
.\scripts\Publish-WinGet.ps1 -Version 1.0.5

# プレビュー実行（変更なし）
.\scripts\Publish-WinGet.ps1 -Version 1.0.5 -DryRun
```

### 手動ステップ

1. 品質チェック: `.\scripts\Run-CodeQuality.ps1 -Configuration Release`
2. ビルド: `dotnet publish -c Release -r win-x64 --self-contained`
3. タグ作成: `git tag -a v1.0.5 -m "Release v1.0.5"`
4. GitHub Release作成: `gh release create v1.0.5 ...`
5. マニフェスト作成: テンプレートから生成
6. WinGet PR作成: `gh pr create --repo microsoft/winget-pkgs ...`

## リリースワークフロー

```
品質チェック → ビルド → タグ作成 → GitHub Release → WinGet PR
```

**自動化**: タグ `v*.*.*` をプッシュすると `.github/workflows/release.yml` が自動実行。

## マニフェスト作成

### テンプレート使用

`assets/manifest-templates/` のテンプレートを使用:

```powershell
$version = "1.0.5"
$sha256 = (Get-FileHash "GistGet-win-x64.zip").Hash
$releaseDate = (Get-Date).ToString("yyyy-MM-dd")

# テンプレート変数を置換
# {{VERSION}} → $version
# {{SHA256}} → $sha256
# {{RELEASE_DATE}} → $releaseDate
```

### 配置先

```
external/winget-pkgs/manifests/n/NuitsJp/GistGet/{VERSION}/
├── NuitsJp.GistGet.yaml
├── NuitsJp.GistGet.installer.yaml
└── NuitsJp.GistGet.locale.en-US.yaml
```

## バリデーション

```powershell
# スキル同梱スクリプト
.\scripts\validate-manifest.ps1 -ManifestPath "external/winget-pkgs/manifests/n/NuitsJp/GistGet/1.0.5"

# または直接
winget validate --manifest <path>
```

## PR作成

### 自動（Publish-WinGet.ps1使用時）

スクリプトが自動でPRを作成。

### 手動

```powershell
cd external/winget-pkgs

# upstreamと同期
git fetch upstream master
git checkout master
git reset --hard upstream/master

# ブランチ作成
git checkout -b NuitsJp.GistGet-1.0.5

# マニフェスト追加・コミット
git add manifests/n/NuitsJp/GistGet/1.0.5
git commit -m "New version: NuitsJp.GistGet version 1.0.5"
git push origin NuitsJp.GistGet-1.0.5

# PR作成
gh pr create --repo microsoft/winget-pkgs --base master --head nuitsjp:NuitsJp.GistGet-1.0.5 --title "New version: NuitsJp.GistGet version 1.0.5"
```

## 参照ファイル

- **GistGet固有情報**: [references/gistget-context.md](references/gistget-context.md)
- **WinGetマニフェスト仕様**: [references/winget-manifest.md](references/winget-manifest.md)
