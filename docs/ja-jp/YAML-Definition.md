# YAML定義

YAMLの定義について、つぎに例を示します。

```yaml
7zip.7zip:
Microsoft.VisualStudioCode:
  allowHashMismatch: true
  architecture: x64
  custom: /VERYSILENT /NORESTART /MERGETASKS=!runcode,addcontextmenufiles,addcontextmenufolders
  force: true
  header: 'Authorization: Bearer xxx'
  installerType: exe
  locale: en-US
  location: C:\Program Files\Microsoft VS Code
  log: C:\Temp\vscode_install.log
  mode: silent
  override: /SILENT
  scope: machine
  skipDependencies: true
  version: 1.85.0
  confirm: true
  whatIf: true
  uninstall: false
```

## パラメーター一覧
- [allowHashMismatch](#allowhashmatch)
- [architecture](#architecture)
- [custom](#custom)
- [force](#force)
- [header](#header)
- [installerType](#installertype)
- [locale](#locale)
- [location](#location)
- [log](#log)
- [mode](#mode)
- [override](#override)
- [scope](#scope)
- [skipDependencies](#skipdependencies)
- [version](#version)
- [confirm](#confirm)
- [whatIf](#whatif)
- [uninstall](#uninstall)

## パラメーター

### allowHashMismatch
インストーラーまたは依存関係のSHA256ハッシュが、WinGetパッケージマニフェストのハッシュと一致しない場合でもダウンロードを許可します。

### architecture
WinGetパッケージインストーラーのプロセッサアーキテクチャを指定します。
指定可能な値：
- Default
- X86
- Arm
- X64
- Arm64

### custom
インストーラーに追加の引数を渡すために使用します。複数の引数を含める場合は、インストーラーが期待する形式で文字列内に含める必要があります。

### force
WinGetが通常行うチェックをスキップして、強制的にインストーラーを実行します。

### header
WinGet RESTソースに渡すカスタムHTTPヘッダー値を指定します。

### installerType
パッケージのインストーラータイプを指定します。
指定可能な値：
- Default
- Inno
- Wix
- Msi
- Nullsoft
- Zip
- Msix
- Exe
- Burn
- MSStore
- Portable

### locale
インストーラーパッケージのロケールを指定します。BCP 47形式（例：en-US）で指定する必要があります。

### location
パッケージをインストールするファイルパスを指定します。インストーラーが代替インストール場所をサポートしている必要があります。

### log
インストーラーのログファイルの場所を指定します。完全修飾パスまたは相対パスとファイル名を含める必要があります。

### mode
インストーラーの出力モードを指定します。
指定可能な値：
- Default
- Silent
- Interactive

### override
インストーラーに渡される既存の引数を上書きします。パッケージマニフェストで指定された引数を上書きする単一の文字列値を指定します。

### scope
WinGetパッケージインストーラーのスコープを指定します。
指定可能な値：
- Any
- User
- System
- UserOrUnknown
- SystemOrUnknown

### skipDependencies
WinGetパッケージの依存関係のインストールをスキップします。

### version
インストールするパッケージのバージョンを指定します。

### confirm
コマンドレットを実行する前に確認のプロンプトを表示します。

### whatIf
コマンドレットを実際には実行せず、実行した場合に何が起こるかを表示します。

### uninstall
パッケージのアンインストール状態を指定します。trueの場合、Sync-GistGetPackage実行時にパッケージがアンインストールされます。