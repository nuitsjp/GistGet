using WinGetPassthroughTest;

// WinGetStreamingPassthroughクラスを使用してwinget listを実行
var passthrough = new WinGetStreamingPassthrough();
var exitCode = await passthrough.ExecuteAsync(new[] { "list" });

Environment.Exit(exitCode);