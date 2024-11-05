# Install-GistGetPackage

パッケージをインストールし、あわせてGist上のYAML定義ファイルを更新します。

```pwsh
Install-GistGetPackage -Id Git.Git
```

インストール前:

```yaml
7zip.7zip:
Adobe.Acrobat.Reader.64-bit:
```

インストール後:

```yaml
7zip.7zip:
Adobe.Acrobat.Reader.64-bit:
Git.Git:
```

インストール時にパラメーターを追加してインストールすることが可能です。

```pwsh
Install-GistGetPackage -Id Microsoft.VisualStudioCode.Insiders -Custom "/VERYSILENT /NORESTART /MERGETASKS=!runcode,addcontextmenufiles,addcontextmenufolders,associatewithfiles,addtopath"
```

インストール後:

```yaml
7zip.7zip:
Adobe.Acrobat.Reader.64-bit:
Git.Git:
Microsoft.VisualStudioCode.Insiders:
  custom: /VERYSILENT /NORESTART /MERGETASKS=!runcode,addcontextmenufiles,addcontextmenufolders,associatewithfiles,addtopath
```

定義ファイルを更新せず、ローカルへのみインストールしたい場合はWinGetを直接利用してください。

```pwsh
winget install --id Git.Git
```

## Parameters

Install-GistGetPackageは、内部的に次のように動作します。

1. Find-WinGetPackage を呼び出してパッケージを取得
2. Install-WinGetPackage を呼び出してパッケージをインストール

したがってInstall-GistGetPackageでは、それらの2つのFunctionのパラメーターがおおむね利用できます。

利用できるパラメーターをつぎに示します。詳細はFind-WinGetPackageとInstall-WinGetPackageのヘルプもあわせてご覧ください。

|Parameter|用途|説明|
|--|--|--|
|Query|Find|パッケージを検索する文字列を指定します。PackageIdentifier、PackageName、Moniker、Tagsに対してマッチングを行います。|
|Id|Find|パッケージ識別子を指定します。|
|Name|Find|パッケージ名を指定します。|
|Source|Find|パッケージをインストールするWinGetソースを指定します。|
|Moniker|Find|パッケージのMonikerを指定します。|
|MatchOption|Find|パッケージ検索の一致オプションを指定します。(Equals, EqualsCaseInsensitive, StartsWithCaseInsensitive, ContainsCaseInsensitive)|
|AllowHashMismatch|Install|インストーラーまたは依存関係のSHA256ハッシュが一致しない場合でもダウンロードを許可します。|
|Architecture|Install|インストーラーのプロセッサアーキテクチャを指定します。(Default, X86, Arm, X64, Arm64)|
|Custom|Install|インストーラーに追加の引数を渡します。|
|Force|Install|通常のチェックをスキップして強制的にインストールします。|
|Header|Install|WinGet RESTソースに渡すカスタムHTTPヘッダー値を指定します。|
|InstallerType|Install|使用するインストーラーの種類を指定します。(Default, Inno, Wix, Msi, Nullsoft, Zip, Msix, Exe, Burn, MSStore, Portable)|
|Locale|Install|インストーラーのロケールをBCP47形式で指定します(例: en-US)。|
|Location|Install|パッケージのインストールパスを指定します。|
|Log|Install|インストーラーのログファイルのパスを指定します。|
|Mode|Install|インストーラーの実行モードを指定します。(Default, Silent, Interactive)|
|Override|Install|インストーラーに渡される既存の引数を上書きします。|
|Scope|Install|インストールスコープを指定します。(Any, User, System, UserOrUnknown, SystemOrUnknown)|
|SkipDependencies|Install|依存関係のインストールをスキップします。|
|Version|Install|インストールするパッケージのバージョンを指定します。|
|Confirm|Install|実行前に確認のプロンプトを表示します。|
|WhatIf|Install|実際の実行は行わず、実行されるアクションを表示します。|