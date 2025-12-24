# WinGet マニフェスト仕様

## ファイル構成

WinGetマニフェストは3つのYAMLファイルで構成:

| ファイル | 用途 |
|----------|------|
| `{PackageId}.yaml` | バージョン情報 |
| `{PackageId}.installer.yaml` | インストーラー情報 |
| `{PackageId}.locale.{locale}.yaml` | ローカライズ情報 |

## ディレクトリ構造

```
manifests/{first-letter}/{Publisher}/{PackageName}/{Version}/
```

例: `manifests/n/NuitsJp/GistGet/1.0.5/`

## 必須フィールド

### version.yaml
- `PackageIdentifier`: パッケージ識別子
- `PackageVersion`: バージョン
- `DefaultLocale`: デフォルトロケール
- `ManifestType`: `version`
- `ManifestVersion`: スキーマバージョン

### installer.yaml
- `PackageIdentifier`: パッケージ識別子
- `PackageVersion`: バージョン
- `InstallerType`: インストーラータイプ
- `Installers`: インストーラー配列
  - `Architecture`: アーキテクチャ
  - `InstallerUrl`: ダウンロードURL
  - `InstallerSha256`: SHA256ハッシュ
- `ManifestType`: `installer`
- `ManifestVersion`: スキーマバージョン

### locale.yaml
- `PackageIdentifier`: パッケージ識別子
- `PackageVersion`: バージョン
- `PackageLocale`: ロケール
- `Publisher`: パブリッシャー名
- `PackageName`: パッケージ名
- `License`: ライセンス
- `ShortDescription`: 短い説明
- `ManifestType`: `defaultLocale`
- `ManifestVersion`: スキーマバージョン

## InstallerType一覧

| タイプ | 説明 |
|--------|------|
| `exe` | 実行可能ファイル |
| `msi` | MSIインストーラー |
| `msix` | MSIXパッケージ |
| `zip` | ZIPアーカイブ |
| `portable` | ポータブル実行ファイル |

### ZIP + Portable の設定

```yaml
InstallerType: zip
NestedInstallerType: portable
NestedInstallerFiles:
- RelativeFilePath: GistGet.exe
  PortableCommandAlias: gistget
```

## バリデーション

```powershell
# マニフェストの検証
winget validate --manifest <path-to-manifest-folder>

# ローカルインストールテスト
winget install --manifest <path-to-manifest-folder>
```

## PR提出チェックリスト

- [ ] CLA署名済み
- [ ] 同一パッケージの未マージPRがないことを確認
- [ ] 1 PRにつき1マニフェストのみ
- [ ] `winget validate` でバリデーション成功
- [ ] `winget install --manifest` でインストール成功
- [ ] セキュリティスキャン（ウイルス対策）通過

## スキーマURL

| マニフェスト | スキーマ |
|--------------|----------|
| version | `https://aka.ms/winget-manifest.version.1.10.0.schema.json` |
| installer | `https://aka.ms/winget-manifest.installer.1.10.0.schema.json` |
| defaultLocale | `https://aka.ms/winget-manifest.defaultLocale.1.10.0.schema.json` |

## 参考リンク

- [Create your package manifest](https://learn.microsoft.com/windows/package-manager/package/manifest)
- [Submit your manifest](https://learn.microsoft.com/windows/package-manager/package/repository)
- [winget-pkgs リポジトリ](https://github.com/microsoft/winget-pkgs)
