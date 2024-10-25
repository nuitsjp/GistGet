# GistGetとは？

GistGetはWinGetのインストールリストをGistで管理するためのPowerShell Moduleです。

```pwsh
Install-Module GistGet
```

Uriやファイルパスを指定することもできるため、プロダクトの開発環境を整えるため、開発端末の設定を同期するといった使い方もできます。

# Introduction

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

別の端末などから定義が追加されていた場合、追加インストールされます。

また別の端末でアンインストールした場合、アンインストールをマークすると、同期時に合わせてアンインストールされます。

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
