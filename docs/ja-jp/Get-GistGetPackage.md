# Get-GistGetPackage

Gistに定義されているパッケージの一覧を取得します。

```pwsh
Get-GistGetPackage
```

つぎの優先順位でGistから定義情報を取得します。

1. Set-GistGetGistIdで設定されたGistのID
2. 「Gist description...」に「GistGet」のみが設定されたGist

デフォルトは2.になります。

ただ1.の方が高速に動作するため、頻繁に使う場合はSet-GistGetGistIdでIDを明示的に登録することを推奨します。
