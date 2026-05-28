# クイックスタート: sync dry-run

## 前提

- Windows 環境で実行する。
- .NET SDK と winget が利用できる。
- 通常の Gist 同期元を使う場合は GitHub 認証済みである。

## ビルド

```powershell
dotnet build src/GistGet.slnx -c Debug
```

## TDD の進め方

1. `sync --dry-run` が `SyncAsync(null, null, true)` を呼ぶ失敗テストを追加して RED を確認する。
2. `--dry-run` フラグを追加し、最小実装で GREEN にする。
3. dry-run で実操作が呼ばれない失敗テストを追加して RED を確認する。
4. dry-run 分岐を最小実装し、状態取得と分類だけを行って GREEN にする。
5. 表示と件数の失敗テストを追加し、表形式の出力で GREEN にする。
6. テストが通った状態で重複や命名だけをリファクタリングする。

## 手動確認

通常の同期元をプレビューする。

```powershell
dotnet run --project src/GistGet/GistGet.csproj -- sync --dry-run
```

ローカルファイルを同期元としてプレビューする。

```powershell
dotnet run --project src/GistGet/GistGet.csproj -- sync --dry-run --file .\GistGet.yaml
```

URL を同期元としてプレビューする。

```powershell
dotnet run --project src/GistGet/GistGet.csproj -- sync --dry-run --url https://example.com/GistGet.yaml
```

## 期待結果

- 実行予定の操作種別と対象件数が表示される。
- 差分があってもプレビュー完了時の終了コードは `0` になる。
- dry-run 後にパッケージ導入状態、ピン状態、Gist 内容が変更されていない。

## 回帰確認

```powershell
dotnet test src/GistGet.Tests/GistGet.Tests.csproj -c Debug --collect:"XPlat Code Coverage" --results-directory TestResults
```
