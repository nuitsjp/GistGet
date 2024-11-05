# Set-GistFile

GistGetを利用する際のGistのIDをファイル名を設定します。

```pwsh
Set-GistFile -GistId 49990de4389f126d1f6d57c10c408a0c -GistFileName GistGet.yml
```

GitGistでは、デフォルトでは「Gist description...」に「GistGet」とだけ記述されているGistの先頭のファイルが利用されます。

これはGistのIDなどが分かりにくいため、容易に利用するための設計です。そのため内部的には、つぎの手順で動作しています。

1. Gist全体から「Gist description...」に「GistGet」とだけ記述されているものを探す
2. Gistからパッケージをロードする

その結果、2回のネットワーク通信が発生しています。

Set-GistFileでIDとファイル名を設定しておくことで、その分軽量に利用することができます。