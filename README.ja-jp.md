# GistGetとは？

GistGetはWinGetのインストールリストをGistで管理するためのPowerShell Moduleです。

PSGalaryからモジュールをインストールして利用します。

```pwsh
Install-Module GistGet
```

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

Gistの定義に従ってパッケージのインストールをします。定義ファイルに追加があった場合、ローカルにインストールされていないものだけが、インストールされます。

```pwsh
Sync-GistGetPackage
```

# Functions

|Function|概略|使用例|
|--|--|--|
|Set-GitHubToken|インストールパッケージの定義Gistを取得・更新するためのGitHubトークンを設定します。|Set-GitHubToken "&lt;Your Access Token&gt;"|
|Sync-GistGetPackage|Gistの定義にローカルのパッケージを同期します。Gistだけに定義されているパッケージはインストールされ、Gistにアンインストールがマークされていればパッケージはアンインストールされます。|Sync-GistGetPackage|
|Get-GistGetPackage|Gistに定義されているパッケージの一覧を取得します。|Get-GistGetPackage|
|Install-GistGetPackage|WinGetからパッケージをインストールし、合わせてGist上の定義ファイルを更新します。|Install-Package -Id 7zip.7zip|
|Uninstall-GistGetPackage|パッケージをアンインストールし、合わせてGist上のアンインストールをマークします。|Uninstall-Package -Id 7zip.7zip|
|Set-GistGetGistId|GistをGist descriptionではなくIdやファイル名から取得したい場合に、Idなどを設定します。|Set-GistGetGistId|
|Get-GistGetGistId|設定されているGistのIdなどを取得します。|Get-GistGetGistId|
