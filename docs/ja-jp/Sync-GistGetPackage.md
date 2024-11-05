# Sync-GistGetPackage

GistGetのYAML定義ファイルを指定して、パッケージを同期（インストール/アンインストール）します。

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

[Uninstall-GistGetPackage](Uninstall-GistGetPackage.md)を利用すると、アンインストール時に自動的にuninstall: trueが設定されます。

このあたりがWinGetのimportと比較し、とくに使い勝手が良い点です。

## Gist

デフォルトでは「Gist description...」に「GistGet」とだけ記述されているGistの先頭のファイルが利用されます。

```pwsh
Sync-GistGetPackage
```

GistのIdとFileを明示的に指定することもできます。

```pwsh
Set-GistFile -GistId 49990de4389f126d1f6d57c10c408a0c -File GistGet.yml
Sync-GistGetPackage
```

IdとFileを設定していない場合、Descriptionから対象のYAMLを探しに行くため、指定しておくことでやや体験が改善されます。


## Uri

Web上に公開されているYAMLファイルを指定することが可能です。

```pwsh
Sync-GistGetPackage -Uri https://gist.githubusercontent.com/nuitsjp/49990de4389f126d1f6d57c10c408a0c/raw/73583e15d292e3a461abebc548a3e6820046e81a/GistGet.yml
```

もちろんUriにはGist以外も指定することができます。

## File

たとえばgitリポジトリーにファイルを登録しておき、それを利用して同期する様な運用も可能です。

```pwsh
Sync-GistGetPackage -Path .\GistGet.yml
```

特定のプロダクトで利用するパッケージを管理したい場合などで有用でしょう。
