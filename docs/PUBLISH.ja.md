# GistGet パッケージング・発行ガイド

このドキュメントでは、GistGet CLI を WinGet にパッケージングして発行するための設計と手順を説明します。

## 目次

- [概要](#概要)
- [アーキテクチャ](#アーキテクチャ)
- [ビルド設定](#ビルド設定)
- [リリースフロー](#リリースフロー)
- [手動リリース手順](#手動リリース手順)
- [WinGet マニフェスト](#winget-マニフェスト)
- [GitHub Secrets の設定](#github-secrets-の設定)
- [トラブルシューティング](#トラブルシューティング)

---

## 概要

GistGet は以下の方法で配布されます:

| 配布先 | 形式 | 対応アーキテクチャ |
|--------|------|-------------------|
| GitHub Releases | ZIP アーカイブ | x64, ARM64 |
| WinGet | Portable アプリ | x64, ARM64 |

### パッケージ識別子

- **WinGet PackageIdentifier**: `nuitsjp.GistGet`
- **Moniker**: `gistget`

### バージョニング

- セマンティックバージョニング (`MAJOR.MINOR.PATCH`) を採用
- Git タグ形式: `v0.1.0`, `v1.0.0` など
- プレリリース: `v0.1.0-beta.1`, `v1.0.0-rc.1` など

---

## アーキテクチャ

### ビルド成果物

```
artifacts/
├── GistGet-win-x64.zip           # x64 向け ZIP アーカイブ
├── GistGet-win-x64.zip.sha256    # x64 SHA256 ハッシュ
├── GistGet-win-arm64.zip         # ARM64 向け ZIP アーカイブ
├── GistGet-win-arm64.zip.sha256  # ARM64 SHA256 ハッシュ
├── SHA256SUMS.txt                # 全アーカイブのハッシュ一覧
└── publish/
    ├── win-x64/                  # x64 発行済みファイル
    │   └── GistGet.exe
    └── win-arm64/                # ARM64 発行済みファイル
        └── GistGet.exe
```

### CI/CD パイプライン

```
┌─────────────────────────────────────────────────────────────────┐
│ タグプッシュ (v*.*.*)                                            │
└─────────────────────┬───────────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────────┐
│ Build Job (並列実行)                                             │
│ ┌─────────────────────┐  ┌─────────────────────┐               │
│ │ win-x64             │  │ win-arm64           │               │
│ │ - dotnet publish    │  │ - dotnet publish    │               │
│ │ - ZIP 作成          │  │ - ZIP 作成          │               │
│ │ - SHA256 計算       │  │ - SHA256 計算       │               │
│ └─────────────────────┘  └─────────────────────┘               │
└─────────────────────┬───────────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────────┐
│ Release Job                                                      │
│ - アーティファクト収集                                            │
│ - GitHub Releases 作成                                           │
│ - リリースノート生成                                              │
└─────────────────────┬───────────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────────┐
│ WinGet Publish Job (正式リリースのみ)                            │
│ - wingetcreate でマニフェスト生成                                │
│ - microsoft/winget-pkgs へ PR 自動作成                          │
└─────────────────────────────────────────────────────────────────┘
```

---

## ビルド設定

### csproj の発行設定

[src/GistGet/GistGet.csproj](../src/GistGet/GistGet.csproj) に以下の設定が含まれています:

```xml
<PropertyGroup>
    <!-- パッケージ情報 -->
    <Version>0.1.0</Version>
    <Authors>nuitsjp</Authors>
    <Company>nuitsjp</Company>
    <Product>GistGet</Product>
    <Description>Windows Package Manager Cloud Sync Tool</Description>

    <!-- 発行設定 -->
    <PublishSingleFile>true</PublishSingleFile>
    <SelfContained>true</SelfContained>
    <EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>
    <IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
</PropertyGroup>
```

### ビルドコマンド

```powershell
# x64 向けビルド
dotnet publish src/GistGet/GistGet.csproj -c Release -r win-x64 --self-contained true -o artifacts/publish/win-x64

# ARM64 向けビルド
dotnet publish src/GistGet/GistGet.csproj -c Release -r win-arm64 --self-contained true -o artifacts/publish/win-arm64
```

---

## リリースフロー

### 自動リリース (推奨)

1. **バージョンを更新**
   ```powershell
   # csproj のバージョンを更新
   # <Version>0.2.0</Version>
   ```

2. **コミットしてタグをプッシュ**
   ```powershell
   git add .
   git commit -m "chore: bump version to 0.2.0"
   git tag v0.2.0
   git push origin main --tags
   ```

3. **GitHub Actions が自動実行**
   - x64/ARM64 両方のビルドが並列実行
   - GitHub Releases に自動アップロード
   - winget-pkgs への PR が自動作成 (正式リリースのみ)

### プレリリース

プレリリースタグ (`-alpha`, `-beta`, `-rc` など) を使用すると、winget-pkgs への PR 作成がスキップされます:

```powershell
git tag v0.2.0-beta.1
git push origin --tags
```

---

## 手動リリース手順

ローカルでリリースビルドを作成する場合:

### 1. ビルドスクリプトの実行

```powershell
# デフォルト (csproj のバージョンを使用)
.\scripts\Publish-Release.ps1

# バージョンを明示的に指定
.\scripts\Publish-Release.ps1 -Version 0.2.0
```

### 2. 出力確認

```
artifacts/
├── GistGet-win-x64.zip
├── GistGet-win-arm64.zip
└── SHA256SUMS.txt
```

### 3. GitHub Releases への手動アップロード

1. [GitHub Releases](https://github.com/nuitsjp/GistGet/releases) を開く
2. "Draft a new release" をクリック
3. タグを選択または新規作成 (`v0.2.0`)
4. アーティファクトをアップロード
5. リリースノートを記入
6. "Publish release" をクリック

### 4. winget-pkgs への PR (手動)

```powershell
# wingetcreate のインストール
winget install Microsoft.WingetCreate

# マニフェストをローカル出力し、PortableCommandAlias を反映してから PR 作成
$outDir = "./winget-manifest"
if (Test-Path $outDir) { Remove-Item -Recurse -Force $outDir }

wingetcreate update nuitsjp.GistGet `
   --version 0.2.0 `
   --urls https://github.com/nuitsjp/GistGet/releases/download/v0.2.0/GistGet-win-x64.zip `
         https://github.com/nuitsjp/GistGet/releases/download/v0.2.0/GistGet-win-arm64.zip `
   --out $outDir

# installer マニフェスト (*.installer.yaml) の NestedInstallerFiles に以下を設定して保存
#   - RelativeFilePath: GistGet.exe
#   - PortableCommandAlias: gistget

wingetcreate submit $outDir --prtitle "Update nuitsjp.GistGet to 0.2.0"
```

---

## WinGet マニフェスト

### マニフェスト形式

GistGet は **Singleton マニフェスト形式** (単一ファイル) を使用しています。

> **注意**: ローカルにマニフェストファイルは保持していません。
> `wingetcreate` が winget-pkgs リポジトリのマニフェストを自動管理します。

### マニフェストスキーマ

- **スキーマバージョン**: 1.6.0
- **マニフェストタイプ**: singleton
- **インストーラータイプ**: zip + portable

### 必須フィールド

| フィールド | 説明 | 例 |
|-----------|------|-----|
| `PackageIdentifier` | 一意の識別子 | `nuitsjp.GistGet` |
| `PackageVersion` | バージョン | `0.1.0` |
| `PackageLocale` | ロケール | `en-US` |
| `Publisher` | 発行者 | `nuitsjp` |
| `PackageName` | 名前 | `GistGet` |
| `License` | ライセンス | `MIT` |
| `ShortDescription` | 短い説明 | `Windows Package Manager Cloud Sync Tool` |
| `InstallerSha256` | ハッシュ | (ビルド時に生成) |
| `ManifestType` | タイプ | `singleton` |
| `ManifestVersion` | バージョン | `1.6.0` |

### Portable インストーラーの設定

```yaml
Installers:
  - Architecture: x64
    InstallerUrl: https://github.com/nuitsjp/GistGet/releases/download/v0.1.0/GistGet-win-x64.zip
    InstallerSha256: <SHA256_HASH>
    InstallerType: zip
    NestedInstallerType: portable
    NestedInstallerFiles:
      - RelativeFilePath: GistGet.exe
        PortableCommandAlias: gistget
```

### インストール先

WinGet portable アプリは以下にインストールされます:

```
%LOCALAPPDATA%\Microsoft\WinGet\Packages\nuitsjp.GistGet_Microsoft.Winget.Source_<hash>\
```

シンボリックリンク (PATH に追加される):
```
%LOCALAPPDATA%\Microsoft\WinGet\Links\gistget.exe
```

---

## GitHub Secrets の設定

### 必要な Secrets

| Secret 名 | 説明 | 用途 |
|-----------|------|------|
| `GITHUB_TOKEN` | 自動生成 | GitHub Releases の作成 |
| `WINGET_GITHUB_TOKEN` | PAT (Personal Access Token) | winget-pkgs への PR 作成 |

### WINGET_GITHUB_TOKEN の作成手順

1. [GitHub Settings > Developer settings > Personal access tokens](https://github.com/settings/tokens) を開く
2. "Generate new token (classic)" をクリック
3. 以下のスコープを選択:
   - `public_repo` (パブリックリポジトリへのアクセス)
4. トークンを生成してコピー
5. リポジトリの Settings > Secrets and variables > Actions > New repository secret
6. 名前: `WINGET_GITHUB_TOKEN`、値: 生成したトークン

---

## トラブルシューティング

### ビルドエラー

#### "SDK not found" エラー
```
error NETSDK1045: The current .NET SDK does not support targeting .NET 8.0
```

**解決策**: .NET 8.0 SDK をインストール
```powershell
winget install Microsoft.DotNet.SDK.8
```

#### ARM64 ビルドの失敗
```
error MSB3073: The command "..." exited with code 1.
```

**解決策**: Windows SDK のインストールを確認
```powershell
winget install Microsoft.WindowsSDK.10.0.26100
```

### WinGet 発行エラー

#### "Invalid manifest" エラー

**解決策**: wingetcreate でマニフェストを検証
```powershell
# wingetcreate が生成したマニフェストを確認
wingetcreate show nuitsjp.GistGet
```

#### "SHA256 mismatch" エラー

**解決策**: 正しいハッシュを生成
```powershell
Get-FileHash -Algorithm SHA256 artifacts/GistGet-win-x64.zip
```

### CI/CD エラー

#### wingetcreate の実行失敗

**確認項目**:
1. `WINGET_GITHUB_TOKEN` が設定されているか
2. トークンに `public_repo` スコープがあるか
3. トークンが有効期限切れでないか

---

## チェックリスト

### リリース前チェック

- [ ] すべてのテストがパスしている
- [ ] `csproj` のバージョンが更新されている
- [ ] CHANGELOG.md が更新されている
- [ ] `WINGET_GITHUB_TOKEN` シークレットが設定されている

### リリース後チェック

- [ ] GitHub Releases にアーティファクトがアップロードされている
- [ ] SHA256 ハッシュが正しい
- [ ] winget-pkgs への PR が作成されている (正式リリースの場合)
- [ ] `winget search nuitsjp.GistGet` で検索可能 (PR マージ後)

---

## 参考リンク

- [WinGet CLI マニフェスト仕様](https://github.com/microsoft/winget-cli/blob/master/doc/ManifestSpecv1.6.md)
- [wingetcreate ドキュメント](https://github.com/microsoft/winget-create)
- [microsoft/winget-pkgs リポジトリ](https://github.com/microsoft/winget-pkgs)
- [.NET Self-Contained 発行](https://learn.microsoft.com/ja-jp/dotnet/core/deploying/)
