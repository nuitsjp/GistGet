# Sync-GistGetPackage

GistGetのYAML定義ファイルを指定して、パッケージをインストール/アンインストールします。

```pwsh
Sync-GistGetPackage
```

デフォルトでは「Gist description...」に「GistGet」とだけ記述されているGistの先頭のファイルが利用されます。

GistGetでは定義ファイルを、Gist・Uri・ファイルの何れかから取得して利用できます。

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
