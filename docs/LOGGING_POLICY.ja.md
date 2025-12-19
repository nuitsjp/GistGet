# GistGetService ログ出力方針

このドキュメントは `GistGetService.cs` に対するログ出力の方針を定義します。

## 基本方針

### 1. ログ出力レベル

| レベル | 用途 | メソッド |
|--------|------|----------|
| **Info** | 通常の処理フロー、操作開始/完了 | `WriteInfo()` |
| **Warning** | 注意が必要な状況、スキップされた処理 | `WriteWarning()` |
| **Progress** | 時間がかかる処理のスピナー表示 | `WriteProgress()` (**新規追加**) |
| **Step** | 複数ステップの進捗表示 | `WriteStep()` (**新規追加**) |
| **Success** | 処理の成功完了 | `WriteSuccess()` (**新規追加**) |
| **Error** | エラー情報 | `WriteError()` (**新規追加**) |

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
| 認証処理 | `LoginAsync()` | `GitHub へログイン中...` |
| sync 処理 | 全体の進捗 | `[1/10] パッケージ xxx をインストール中...` |

---

## IConsoleService への追加メソッド

### 新規追加インターフェース

```csharp
/// <summary>
/// スピナー付きプログレス表示を開始します。
/// バックグラウンドでスピナーをアニメーション表示し、Dispose 時に停止します。
/// </summary>
/// <param name="message">表示するメッセージ</param>
/// <returns>Dispose 時にスピナーを停止する IDisposable</returns>
IDisposable WriteProgress(string message);

/// <summary>
/// ステップ進捗を表示します（単純な一行表示）。
/// </summary>
/// <param name="current">現在のステップ番号</param>
/// <param name="total">総ステップ数</param>
/// <param name="message">表示するメッセージ</param>
void WriteStep(int current, int total, string message);

/// <summary>
/// 成功メッセージを表示します。
/// </summary>
/// <param name="message">表示するメッセージ</param>
void WriteSuccess(string message);

/// <summary>
/// エラーメッセージを表示します。
/// </summary>
/// <param name="message">表示するメッセージ</param>
void WriteError(string message);
```

### 実装例（ConsoleService.cs）

```csharp
/// <summary>
/// スピナー付きプログレス表示を開始します。
/// </summary>
public IDisposable WriteProgress(string message)
{
    return new SpinnerProgress(message);
}

private sealed class SpinnerProgress : IDisposable
{
    private static readonly string[] SpinnerFrames = ["⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏"];
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _spinnerTask;
    private readonly string _message;
    private int _frameIndex;

    public SpinnerProgress(string message)
    {
        _message = message;
        Console.CursorVisible = false;
        _spinnerTask = RunSpinnerAsync(_cts.Token);
    }

    private async Task RunSpinnerAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            Console.Write($"\r{SpinnerFrames[_frameIndex]} {_message}");
            _frameIndex = (_frameIndex + 1) % SpinnerFrames.Length;
            try
            {
                await Task.Delay(100, token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        try { _spinnerTask.Wait(); } catch { }
        Console.Write($"\r{new string(' ', _message.Length + 2)}\r");
        Console.CursorVisible = true;
        _cts.Dispose();
    }
}

/// <summary>
/// ステップ進捗を表示します（単純な一行表示）。
/// </summary>
public void WriteStep(int current, int total, string message) =>
    Console.WriteLine($"[{current}/{total}] {message}");

/// <summary>
/// 成功メッセージを表示します。
/// </summary>
public void WriteSuccess(string message) =>
    Console.WriteLine($"✓ {message}");

/// <summary>
/// エラーメッセージを表示します。
/// </summary>
public void WriteError(string message) =>
    Console.Error.WriteLine($"✗ {message}");
```

---

## 各 public メソッドのログ方針

### 1. AuthLoginAsync()

```
開始:   (なし - winget に任せる)
進捗:   WriteProgress("GitHub へログイン中...")
終了:   WriteSuccess("GitHub へのログインが完了しました")
エラー: WriteError("ログインに失敗しました: {エラーメッセージ}")
```

### 2. AuthLogout()

```
開始:   (なし)
終了:   WriteInfo("Logged out") ← 既存維持
```

### 3. AuthStatusAsync()

```
開始:   (なし)
終了:   WriteInfo(...) ← 既存維持
エラー: WriteInfo(...) ← 既存維持
```

### 4. InstallAndSaveAsync()

```
開始:   (なし - winget に任せる)
進捗1:  WriteProgress("Gist からパッケージ情報を取得中...")
進捗2:  (winget install 実行 - パススルー)
進捗3:  WriteProgress("パッケージ情報を Gist に保存中...")
終了:   WriteSuccess("{PackageId} のインストールと Gist への保存が完了しました")
エラー: (winget のエラー出力に任せる)
```

### 5. UninstallAndSaveAsync()

```
開始:   (なし - winget に任せる)
進捗1:  WriteProgress("Gist からパッケージ情報を取得中...")
進捗2:  (winget uninstall 実行 - パススルー)
進捗3:  WriteProgress("パッケージ情報を Gist に保存中...")
終了:   WriteSuccess("{PackageId} のアンインストールと Gist への保存が完了しました")
エラー: (winget のエラー出力に任せる)
```

### 6. UpgradeAndSaveAsync()

```
開始:   (なし - winget に任せる)
進捗1:  (winget upgrade 実行 - パススルー)
進捗2:  WriteProgress("Gist からパッケージ情報を取得中...")
進捗3:  WriteProgress("パッケージ情報を Gist に保存中...")
終了:   WriteSuccess("{PackageId} のアップグレードと Gist への保存が完了しました")
エラー: (winget のエラー出力に任せる)
```

### 7. PinAddAndSaveAsync()

```
開始:   (なし - winget に任せる)
進捗1:  WriteProgress("Gist からパッケージ情報を取得中...")
進捗2:  (winget pin add 実行 - パススルー)
進捗3:  WriteProgress("ピン情報を Gist に保存中...")
終了:   WriteSuccess("{PackageId} のピン設定と Gist への保存が完了しました")
```

### 8. PinRemoveAndSaveAsync()

```
開始:   (なし - winget に任せる)
進捗1:  WriteProgress("Gist からパッケージ情報を取得中...")
進捗2:  (winget pin remove 実行 - パススルー)
進捗3:  WriteProgress("ピン情報を Gist に保存中...")
終了:   WriteSuccess("{PackageId} のピン解除と Gist への保存が完了しました")
```

### 9. SyncAsync()

```
開始:   WriteInfo("[sync] 同期を開始します...")
進捗1:  WriteProgress("パッケージ情報を取得中...")
進捗2:  WriteStep(current, total, "パッケージ {Id} をインストール中...")
進捗3:  WriteStep(current, total, "パッケージ {Id} をアンインストール中...")
進捗4:  WriteStep(current, total, "パッケージ {Id} のピンを設定中...")
終了:   WriteSuccess("同期が完了しました")
サマリ: WriteInfo("インストール: {n}, アンインストール: {m}, 失敗: {k}")
```

> [!NOTE]
> 既存の `[sync]` プレフィックス付きログは維持し、一貫性を保ちます。

### 10. RunPassthroughAsync()

```
(ログなし - winget に完全委譲)
```

### 11. ExportAsync()

```
開始:   WriteProgress("インストール済みパッケージを取得中...")
終了:   WriteInfo(...) ← 既存維持 (出力先がある場合のみ)
```

### 12. ImportAsync()

```
開始:   WriteProgress("YAML ファイルを読み込み中...")
進捗:   WriteProgress("Gist にパッケージ情報を保存中...")
終了:   WriteInfo(...) ← 既存維持
```

---

## 実装手順

ログ機能の実装は、各メソッドを1つずつ順番に実装していきます。

### チェックリスト

- [ ] IConsoleService に新規メソッドを追加
- [ ] ConsoleService に実装を追加
- [ ] AuthLoginAsync() にログを追加
- [ ] AuthLogout() - 既存維持
- [ ] AuthStatusAsync() - 既存維持
- [ ] InstallAndSaveAsync() にログを追加
- [ ] UninstallAndSaveAsync() にログを追加
- [ ] UpgradeAndSaveAsync() にログを追加
- [ ] PinAddAndSaveAsync() にログを追加
- [ ] PinRemoveAndSaveAsync() にログを追加
- [ ] SyncAsync() にログを追加
- [ ] RunPassthroughAsync() - ログなし（維持）
- [ ] ExportAsync() にログを追加
- [ ] ImportAsync() にログを追加

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
