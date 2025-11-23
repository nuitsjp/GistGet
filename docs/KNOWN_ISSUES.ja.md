# 既知の問題

## Winget への依存
-   GistGet は、システム PATH に `winget` (Windows Package Manager) がインストールされ、利用可能であることを必要とします。
-   一部のサーバー環境（Windows Server など）では、`winget` がデフォルトで利用できない場合があります。

## 認証
-   Device Flow 認証プロセスを完了するには、ブラウザが必要です。

## 同期の制限
-   現在、`sync` コマンドはパッケージを順次処理します。並列インストールはまだサポートされていません。
-   `winget` からのシステム再起動要求は報告されますが、自動的には実行されません。

## COM API
-   このツールは Windows Package Manager COM API に依存しています。Microsoft によるこの API の変更は、機能に影響を与える可能性があります。
