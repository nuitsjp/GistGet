# WinGet バージョン指定とPin機能の挙動

このドキュメントは、winget の install/upgrade 時のバージョン指定と pin 機能の関係について、実際の検証結果を元にまとめたものです。

> **検証環境**: Windows Package Manager (winget) v1.12.420  
> **検証日**: 2025年12月2日  
> **テストパッケージ**: jqlang.jq (jq)

## 概要

- **バージョン指定でのインストールは、バージョン固定ではない**
- バージョンを固定するには、インストール後に明示的な `pin add` が必要
- pinの種類によって挙動が異なる

## バージョン指定インストールの挙動

```powershell
# バージョン指定でインストール
winget install jqlang.jq --version 1.7
```

この操作では：
- ✅ 指定バージョン (1.7) がインストールされる
- ❌ pinは**自動的に追加されない**
- ⚠️ 以降の `winget upgrade` や `winget upgrade --all` でアップグレード対象になる

## Pin機能の種類

WinGetは3種類のpinをサポートしています：

### 1. Pinning（デフォルト）

```powershell
winget pin add jqlang.jq --installed
```

- `winget upgrade --all` から除外される
- `winget upgrade <package>` では**アップグレードされる**
- `--include-pinned` オプションで `upgrade --all` に含めることが可能

### 2. Blocking

```powershell
winget pin add jqlang.jq --installed --blocking
```

- `winget upgrade --all` から除外される
- ⚠️ **ドキュメントとの相違**: 公式ドキュメントでは `winget upgrade <package>` もブロックされると記載されているが、v1.12.420 では**明示的なパッケージ指定でアップグレードが可能**
- `--force` オプションでオーバーライド可能

### 3. Gating（バージョン範囲指定）

```powershell
winget pin add jqlang.jq --version 1.7.*
```

- 指定したバージョン範囲（例: `1.7.0` ～ `1.7.x`）に制限
- 範囲外へのアップグレードは `--force` が必要

## 各Pin種類の挙動比較

| pin種類 | `upgrade --all` | `upgrade <package>` | `upgrade --version <v>` |
|---------|-----------------|---------------------|-------------------------|
| **なし** | ✅ アップグレード | ✅ アップグレード | ✅ アップグレード |
| **Pinning** | ❌ スキップ | ✅ アップグレード | ✅ アップグレード |
| **Blocking** | ❌ スキップ | ✅ アップグレード※ | ✅ アップグレード※ |
| **Gating** | 範囲内のみ✅ | 範囲内のみ✅ | 範囲内のみ✅ |

※ v1.12.420での検証結果。ドキュメントと異なる動作。

## バージョン固定の推奨手順

特定バージョンに固定したい場合：

```powershell
# Step 1: バージョン指定でインストール
winget install jqlang.jq --version 1.7

# Step 2: インストール済みバージョンをpin
winget pin add jqlang.jq --installed --blocking
```

## Pin後のバージョン変更

### pinを削除せずに別バージョンへアップグレードした場合

```powershell
# Blocking pinがある状態で
winget upgrade jqlang.jq --version 1.8.0
```

結果：
- アップグレードが**成功する**（v1.12.420での検証結果）
- pinのバージョンも**新しいバージョンに自動更新**される

### 正式な手順

```powershell
# 1. pinを削除
winget pin remove jqlang.jq

# 2. アップグレード
winget upgrade jqlang.jq --version 1.8.0

# 3. 新しいバージョンで再度pin
winget pin add jqlang.jq --installed --blocking
```

## その他の重要な挙動

### パッケージアンインストール時のpin

```powershell
winget uninstall jqlang.jq
```

- ⚠️ パッケージをアンインストールしても、**pinは自動的に削除されない**
- pinを削除するには明示的に `winget pin remove` または `winget pin reset --force` が必要
- 残存したpinは、再インストール時に影響を与える可能性がある

### Pinの確認

```powershell
# 全てのpinを一覧表示
winget pin list

# 特定パッケージのpin確認
winget pin list --id jqlang.jq
```

### Pinの削除

```powershell
# 特定パッケージのpin削除
winget pin remove jqlang.jq

# 全てのpinをリセット（確認のみ）
winget pin reset

# 全てのpinを強制リセット
winget pin reset --force
```

## GistGetでの考慮事項

GistGetのYAML設定でバージョンを指定した場合の推奨動作：

1. `version` が指定されている場合、`winget install --version <version>` でインストール
2. バージョン固定が必要な場合は、インストール後に `winget pin add --installed` を実行
3. pinの種類（Pinning/Blocking/Gating）はユーザーが選択可能にする

## 参考リンク

- [winget pin コマンド (Microsoft Learn)](https://learn.microsoft.com/ja-jp/windows/package-manager/winget/pinning)
- [winget upgrade コマンド (Microsoft Learn)](https://learn.microsoft.com/ja-jp/windows/package-manager/winget/upgrade)
- [winget install コマンド (Microsoft Learn)](https://learn.microsoft.com/ja-jp/windows/package-manager/winget/install)
