# Sync-GistGetPackage

GistGetのYAML定義ファイルを指定して、パッケージをインストール/アンインストールします。

```pwsh
Sync-GistGetPackage
```

デフォルトでは「Gist description...」に「GistGet」とだけ記述されているGistの先頭のファイルが利用されます。

GistGetでは定義ファイルを、[GistFile](#Gist)・[Uri](#Uri)・[ファイル](#File)の何れかから取得して利用できます。

## YAML

YAMLファイルは、つぎのように記述します。[YAML定義の詳細はこちら。](YAML-Definition.md)

```yaml
7zip.7zip:
Microsoft.VisualStudioCode.Insiders:
  custom: /MERGETASKS=!runcode,addcontextmenufiles,addcontextmenufolders,associatewithfiles,addtopath
Zoom.Zoom:
  uninstall: true
```

idはWinGetのIDを利用します。

```pwsh
PS D:\GistGet> winget search 7zip
名前              ID                     バージョン         一致          ソース
--------------------------------------------------------------------------------
7-Zip             7zip.7zip              24.08              Moniker: 7zip winget
```

customを指定することで、インストーラーに追加パラメーターを渡すことができます。

またuninstallにtrueを指定すると、Sync-GistGetPackageを実行した端末に、対象パッケージがインストールされていた場合は、アンインストールされます。

このあたりが、WinGetとimportと比較し、とくに使い勝手が良い点です。

## Gist

デフォルトでは「Gist description...」に「GistGet」とだけ記述されているGistの先頭のファイルが利用されます。

```pwsh
Sync-GistGetPackage
```

GistのIdを指定することも可能です。この場合、そのGist内の先頭のファイルが利用されます。

```pwsh
Sync-GistGetPackage -GistId <Your Gist Id>
```

またファイル名を指定することも可能です。
```pwsh
Sync-GistGetPackage -GistId <Your Gist Id> -GistFileName <Gist File Name>
```

## Uri

Web上に公開されているYAMLファイルを指定することが可能です。

```pwsh
Sync-GistGetPackage -Uri <YAML Uri>
```

## File

gitリポジトリーにファイルを登録しておき、それを利用して同期することも可能です。

```pwsh
Sync-GistGetPackage -Path <YAML File Path>
```
