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
Import-GistGetPackage
```

# Functions
