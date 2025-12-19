# GistGetService ログ出力方針

このドキュメントは `GistGetService.cs` に対するログ出力の方針を定義します。

## 基本方針

### 1. ログ出力レベル

| レベル | 用途 | メソッド |
|--------|------|----------|
| **Info** | 通常の処理フロー、操作開始/完了 | `WriteInfo()` |
| **Warning** | 注意が必要な状況、スキップされた処理 | `WriteWarning()` |
| **Progress** | 時間がかかる処理のスピナー表示 | `WriteProgress()` |
| **Step** | 複数ステップの進捗表示 | `WriteStep()` |
| **Success** | 処理の成功完了 | `WriteSuccess()` |
| **Error** | エラー情報 | `WriteError()` |

### 2. winget パススルーの原則

winget コマンドへのパススルー処理では、**winget 自体の出力に任せる**ことを原則とします。

- `passthroughRunner.RunAsync()` の呼び出し前後に冗長なログを出力しない
- winget が独自に進捗やエラーを表示するため、二重出力を避ける
- GistGet 固有の処理（Gist 操作など）のみログを出力する

### 3. プログレス表示が必要な箇所

以下の処理では進捗表示を行います：

| 処理 | 説明 | 表示例 |
|------|------|--------|
| Gist の取得 | `GetPackagesAsync()` | `Gist からパッケージリストを取得中...` |
| Gist の保存 | `SavePackagesAsync()` | `パッケージリストを Gist に保存中...` |
| sync 処理 | ソース別の取得 | `ファイルからパッケージ情報を読み込み中...` |

---

## 注意事項

> [!IMPORTANT]
> winget のパススルー処理では、GistGet からの二重ログ出力を避けてください。
> winget 自体が進捗やエラーを適切に表示します。

> [!TIP]
> プログレス表示 (`WriteProgress`) は `IDisposable` を返します。
> `using` ブロックで使用することで、例外発生時も確実にプログレスが終了します。
>
> ```csharp
> using (consoleService.WriteProgress("処理中..."))
> {
>     await SomeLongRunningTask();
> }
> ```

> [!CAUTION]
> `WriteStep()` での番号付けは、処理前にカウントを確定してから使用してください。
> 動的にパッケージを追加/スキップする場合は、総数の再計算が必要です。
