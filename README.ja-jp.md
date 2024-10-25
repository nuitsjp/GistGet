# GistGetとは？

GistGetはWinGetのインストールリストをGistで管理するためのPowerShell Moduleです。

Gist以外にも、Uriやファイルパスを利用することもできるため、プロダクトの開発環境を整えるため、開発端末の設定を同期するといった使い方もできます。

WinGetのexport/importとは次の点で異なります。

1. install/uninstall時に、設定がGistに同期されます
2. installパラメーターを利用できます
3. uninstallを同期することも可能です


# Introduction

PowerShell GalleryからModuleをインストールします。

```pwsh
Install-Module GistGet
```

[GitHubからGistを更新するためのトークンを取得し](docs/ja-jp/Set-GitHubToken.md)、設定します。

```pwsh
Set-GitHubToken "<Your Access Token>"
```

インストールリストをGistに作成します。 

**このとき「Gist description...」に「GistGet」を設定します。** ファイル名は任意です。

```yaml
- id: 7zip.7zip
- id: Adobe.Acrobat.Reader.64-bit
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
- id: 7zip.7zip
- id: Adobe.Acrobat.Reader.64-bit
- id: Git.Git
```

このため別の端末でSync-GistGetPackageを実行することで、環境を容易に同期することが可能です。

アンインストールも同期できます。

```pwsh
Uninstall-GistGetPackage -Id Git.Git
```

Gist上の定義ファイルも同期されます。

```yaml
- id: 7zip.7zip
- id: Adobe.Acrobat.Reader.64-bit
- id: Git.Git
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
|Set-GitHubToken|インストールパッケージの定義Gistを取得・更新するためのGitHubトークンを設定します。|
|Sync-GistGetPackage|Gistの定義にローカルのパッケージを同期します。|
|Get-GistGetPackage|Gistに定義されているパッケージの一覧を取得します。|
|Install-GistGetPackage|WinGetからパッケージをインストールし、合わせてGist上の定義ファイルを更新します。|
|Uninstall-GistGetPackage|パッケージをアンインストールし、合わせてGist上のアンインストールをマークします。|
|Set-GistGetGistId|GistをGist descriptionではなくIdやファイル名から取得したい場合に、Idなどを設定します。|
|Get-GistGetGistId|設定されているGistのIdなどを取得します。|
