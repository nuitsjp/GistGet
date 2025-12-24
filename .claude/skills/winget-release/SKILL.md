---
name: winget-release
description: GistGetのWinGetパッケージリリースを支援。(1) 新バージョンのリリース、(2) WinGetマニフェスト作成・更新、(3) winget-pkgsへのPR作成。「WinGetにリリース」「winget-pkgsにPR」「マニフェスト作成」などのキーワードで使用。
---

# GistGet WinGet リリース

GistGetをWinGetパッケージとしてリリースするためのスキル。

## クイックスタート

### 自動リリース（推奨）

**手順**
1. `src/GistGet/GistGet.csproj` の `<Version>` と `CHANGELOG.md` を更新
2. プレビュー実行（任意）
3. `skills/winget-release/scripts/Publish-WinGet.ps1` でリリース実行
4. タグプッシュでActionsが動く場合は完了まで待機

```powershell
# プレビュー実行（変更なし）
.\skills\winget-release\scripts\Publish-WinGet.ps1 -Version 1.0.5 -DryRun

# フルリリースパイプライン
.\skills\winget-release\scripts\Publish-WinGet.ps1 -Version 1.0.5

# Actions完了待ち（必要な場合）
gh run watch --repo nuitsjp/GistGet --workflow release.yml --exit-status
```

## リリースの流れ

### Publish-WinGet.ps1（ローカルで完結）

```
品質チェック → ビルド/ZIP/SHA256 → タグ作成/プッシュ → GitHub Release → winget-pkgs同期 → マニフェスト生成 → PR作成
```

### GitHub Actions（release.yml）

```
タグプッシュ or workflow_dispatch → CI → ビルド → GitHub Release → WinGet PR → Summary
```

**補足**: `Publish-WinGet.ps1` はタグをプッシュするため `release.yml` が起動する。必要なら `gh run watch --repo nuitsjp/GistGet --workflow release.yml --exit-status` で完了待ち。

## PR作成

### 自動（Publish-WinGet.ps1 / release.yml）

どちらも自動でPRを作成。

## 参照ファイル

- **GistGet固有情報**: [references/gistget-context.md](references/gistget-context.md)
