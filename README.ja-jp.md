# GistGetとは？

GistGetはWinGetのインストールリストをGistで管理するためのPowerShell Moduleです。

Gist以外にも、Uriやファイルパスを利用することもできるため、プロダクトの開発環境を整えるため、開発端末の設定を同期するといった使い方もできます。

WinGetのexport/importとは次の点で異なります。

1. 定義ファイルをクラウド上（Gist, Web）で扱うことが最初から想定されている 
2. インストーラーにパラメーターを渡すことができる
3. install/uninstall時に、設定がGistに同期される
4. uninstallを同期することも可能です

# Table of Contents 

- [Getting started](#getting-started)
- [Functions](#functions)
- [YAML定義](docs/ja-jp/YAML-Definition.md)

# Getting started

PowerShell GalleryからModuleをインストールします。

```pwsh
Install-Module GistGet
```

[GitHubからGistを更新するためのトークンを取得](https://github.com/settings/personal-access-tokens/new)し、設定します。取得するトークンに必要な権限などは[こちら](https://github.com/nuitsjp/GistGet/blob/main/docs/ja-jp/Set-GitHubToken.md#%E5%89%B2%E3%82%8A%E5%BD%93%E3%81%A6%E6%A8%A9%E9%99%90)を参照してください。

```pwsh
Set-GitHubToken github_pat_11AD3NELA0SGEHcrynCMSo...
```

インストールリストをGistに作成します。 

**このとき「Gist description...」に「GistGet」を設定します。** ファイル名は任意です。

```yaml
7zip.7zip:
Microsoft.VisualStudioCode:
  custom: /VERYSILENT /NORESTART /MERGETASKS=!runcode,addcontextmenufiles,addcontextmenufolders,associatewithfiles,addtopath
```

Gistの定義に従ってパッケージを同期します。

```pwsh
Sync-GistGetPackage
```

パッケージをすべてアップデート（wingetのupgrade）します。

```pwsh
Update-GistGetPackage
```

新たなパッケージをインストールします。

```pwsh
Install-GistGetPackage -Id Git.Git
```

GistGetのコマンドを通してインストールすると、Gist上の定義ファイルも更新されます。

```yaml
7zip.7zip:
Microsoft.VisualStudioCode:
  custom: /VERYSILENT /NORESTART /MERGETASKS=!runcode,addcontextmenufiles,addcontextmenufolders,associatewithfiles,addtopath
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
  custom: /VERYSILENT /NORESTART /MERGETASKS=!runcode,addcontextmenufiles,addcontextmenufolders,associatewithfiles,addtopath
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
|[Update-GistGetPackage](docs/ja-jp/Update-GistGetPackage.md)|Gistの定義にローカルのパッケージを同期します。|
|[Install-GistGetPackage](docs/ja-jp/Install-GistGetPackage.md)|WinGetからパッケージをインストールし、合わせてGist上の定義ファイルを更新します。|
|[Uninstall-GistGetPackage](docs/ja-jp/Uninstall-GistGetPackage.md)|パッケージをアンインストールし、合わせてGist上のアンインストールをマークします。|
|[Set-GistFile](docs/ja-jp/Set-GistFile.md)|GistをGist descriptionではなくIdやファイル名から取得したい場合に、Idなどを設定します。|
|[Get-GistFile](docs/ja-jp/Get-GistFile.md)|設定されているGistのIdなどを取得します。|
