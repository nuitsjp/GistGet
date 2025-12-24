---
name: winget-release
description: GistGetのWinGetパッケージリリースを支援。(1) 新バージョンのリリース、(2) WinGetマニフェスト作成・更新、(3) winget-pkgsへのPR作成。「WinGetにリリース」「winget-pkgsにPR」「マニフェスト作成」などのキーワードで使用。
---

# GistGet WinGet リリース

GistGetをWinGetパッケージとしてリリースするためのスキル。

## クイックスタート

### リリース手順

**手順**
1. `src/GistGet/GistGet.csproj` の `<Version>` と `CHANGELOG.md` を更新
2. プレビュー実行（任意）
3. `skills/winget-release/scripts/Publish-WinGet.ps1` でリリース実行

```powershell
# プレビュー実行（変更なし）
.\skills\winget-release\scripts\Publish-WinGet.ps1 -Version 1.0.6 -DryRun

# フルリリースパイプライン
.\skills\winget-release\scripts\Publish-WinGet.ps1 -Version 1.0.6
```

## リリースの流れ

### Publish-WinGet.ps1（唯一のリリースフロー）

```
品質チェック → ビルド/ZIP/SHA256 → タグ作成/プッシュ → GitHub Release → winget-pkgs同期 → マニフェスト生成 → PR作成
```

**重要**: Publish-WinGet.ps1が唯一のリリースフローです。GitHub Actionsのrelease.ymlは削除されました（ハッシュ不一致の問題を防ぐため）。

## PR作成

### 自動（Publish-WinGet.ps1）

スクリプトが自動でwinget-pkgsへPRを作成します。

## 参照ファイル

- **GistGet固有情報**: [references/gistget-context.md](references/gistget-context.md)
