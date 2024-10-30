# GistGetとは？

GistGetはWinGetのインストールリストをGistで管理するためのPowerShell Moduleです。

Gist以外にも、Uriやファイルパスを利用することもできるため、プロダクトの開発環境を整えるため、開発端末の設定を同期するといった使い方もできます。

WinGetのexport/importとは次の点で異なります。

1. install/uninstall時に、設定がGistに同期されます
2. インストーラーにパラメーターを渡すことができます
3. uninstallを同期することも可能です


# Introduction

PowerShell GalleryからModuleをインストールします。

```pwsh
Install-Module GistGet
```

[GitHubからGistを更新するためのトークンを取得](docs/ja-jp/Set-GitHubToken.md)し、設定します。

```pwsh
Set-GitHubToken "<Your Access Token>"
```

インストールリストをGistに作成します。 

**このとき「Gist description...」に「GistGet」を設定します。** ファイル名は任意です。

```yaml
7zip.7zip:
Microsoft.VisualStudioCode:
  override: /VERYSILENT /NORESTART /MERGETASKS=!runcode,addcontextmenufiles,addcontextmenufolders,associatewithfiles,addtopath
```

Gistの定義に従ってパッケージを同期します。

```pwsh
Sync-GistGetPackage
```

新たなパッケージをインストールします。

```pwsh
Install-GistGetPackage -Id Git.Git
```

GistGetのコマンドを通してインストールすると、Gist上の定義ファイルも更新されます。

```yaml
7zip.7zip:
Microsoft.VisualStudioCode:
  override: /VERYSILENT /NORESTART /MERGETASKS=!runcode,addcontextmenufiles,addcontextmenufolders,associatewithfiles,addtopath
Git.Git:
```

このため別の端末でSync-GistGetPackageを実行することで、環境を容易に同期することが可能です。

インストール済みのパッケージをアンインストールします。

```pwsh
Uninstall-GistGetPackage -Id Git.Git
```

Gist上の定義ファイルも同期されます。

```yaml
7zip.7zip:
Microsoft.VisualStudioCode:
  override: /VERYSILENT /NORESTART /MERGETASKS=!runcode,addcontextmenufiles,addcontextmenufolders,associatewithfiles,addtopath
Git.Git:
  uninstall: true
```

別の端末でSync-GistGetPackageを実行すると、その端末からもアンインストールされます。

アンインストールを同期したくない場合は、WinGetの標準コマンドを利用してください。

```pwsh
winget uninstall --id Git.Git
```

# Functions

|Function|概略|
|--|--|
|[Set-GitHubToken](docs/ja-jp/Set-GitHubToken.md)|インストールパッケージの定義Gistを取得・更新するためのGitHubトークンを設定します。|
|[Sync-GistGetPackage](docs/ja-jp/Sync-GistGetPackage.md)|Gistの定義にローカルのパッケージを同期します。|
|[Install-GistGetPackage]()|WinGetからパッケージをインストールし、合わせてGist上の定義ファイルを更新します。|
|Uninstall-GistGetPackage|パッケージをアンインストールし、合わせてGist上のアンインストールをマークします。|
|Get-GistFile|GistをGist descriptionではなくIdやファイル名から取得したい場合に、Idなどを設定します。|
|Set-GistFile|設定されているGistのIdなどを取得します。|
