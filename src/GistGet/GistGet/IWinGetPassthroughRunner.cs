namespace GistGet;

/// <summary>
/// WinGetコマンドをパススルーで実行するサービスインターフェース。
/// winget.exeを直接呼び出し、結果をそのまま返します。
/// list、search、showなどGist同期が不要なコマンドに使用されます。
/// </summary>
public interface IWinGetPassthroughRunner
{
    /// <summary>
    /// 指定された引数でWinGetを実行します。
    /// </summary>
    /// <param name="args">WinGetに渡すコマンドライン引数</param>
    /// <returns>WinGetプロセスの終了コード</returns>
    Task<int> RunAsync(string[] args);
}