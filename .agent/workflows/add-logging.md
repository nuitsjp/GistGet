---
description: GistGetService.cs にログ出力を追加する
---

# GistGetService ログ追加ワークフロー

このワークフローは `GistGetService.cs` にログ出力を追加します。

## 前提条件

- `docs/LOGGING_POLICY.ja.md` のログ出力方針を確認済みであること

## 手順

### ステップ 1: IConsoleService に新規メソッドを追加

`src/GistGet/IConsoleService.cs` に以下のメソッドを追加します：

```csharp
/// <summary>
/// スピナー付きプログレス表示を開始します。
/// バックグラウンドでスピナーをアニメーション表示し、Dispose 時に停止します。
/// </summary>
/// <returns>Dispose 時にスピナーを停止する IDisposable</returns>
IDisposable WriteProgress(string message);

/// <summary>
/// ステップ進捗を表示します（単純な一行表示）。
/// </summary>
void WriteStep(int current, int total, string message);

/// <summary>
/// 成功メッセージを表示します。
/// </summary>
void WriteSuccess(string message);

/// <summary>
/// エラーメッセージを表示します。
/// </summary>
void WriteError(string message);
```

### ステップ 2: ConsoleService に実装を追加

`src/GistGet/Presentation/ConsoleService.cs` に実装を追加します：

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

### ステップ 3: 各 public メソッドにログを追加

`docs/LOGGING_POLICY.ja.md` の方針に従い、以下の順序で **1つずつ** ログを追加します：

1. `AuthLoginAsync()` - 進捗と成功ログを追加
2. `InstallAndSaveAsync()` - Gist 操作の進捗ログを追加
3. `UninstallAndSaveAsync()` - Gist 操作の進捗ログを追加
4. `UpgradeAndSaveAsync()` - Gist 操作の進捗ログを追加
5. `PinAddAndSaveAsync()` - Gist 操作の進捗ログを追加
6. `PinRemoveAndSaveAsync()` - Gist 操作の進捗ログを追加
7. `SyncAsync()` - ステップ付き進捗ログを追加
8. `ExportAsync()` - 進捗ログを追加
9. `ImportAsync()` - 進捗ログを追加

> [!IMPORTANT]
> 以下のメソッドはログ追加不要です：
> - `AuthLogout()` - 既存のログで十分
> - `AuthStatusAsync()` - 既存のログで十分
> - `RunPassthroughAsync()` - winget に完全委譲

### ステップ 4: ビルドとテストの確認

// turbo
```powershell
dotnet build
```

// turbo
```powershell
dotnet test
```

## 注意事項

- winget へのパススルー処理では、winget 自体の出力に任せてください
- プログレス表示 (`WriteProgress`) はスピナーアニメーション付きで、`using` ブロックで使用してください
- ステップ表示 (`WriteStep`) は単純な一行表示のみで、アニメーションなし

```csharp
// 例: スピナー付きプログレス
// 処理中は │ / ─ \ が回転して動いていることを表示
using (consoleService.WriteProgress("Gist からパッケージ情報を取得中..."))
{
    await gitHubService.GetPackagesAsync(...);
}

// 例: ステップ表示（シンプルな一行出力）
consoleService.WriteStep(1, 10, "パッケージ xxx をインストール中...");
// 出力: [1/10] パッケージ xxx をインストール中...
```

- sync 処理では既存の `[sync]` プレフィックスを維持してください
