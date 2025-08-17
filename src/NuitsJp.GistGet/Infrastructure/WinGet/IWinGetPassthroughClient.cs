namespace NuitsJp.GistGet.Infrastructure.WinGet;

/// <summary>
/// WinGetパススルークライアントのインターフェース
/// </summary>
public interface IWinGetPassthroughClient
{
    /// <summary>
    /// winget.exeに引数をそのまま渡して実行する
    /// </summary>
    /// <param name="args">wingetコマンド引数</param>
    /// <returns>実行結果コード</returns>
    Task<int> ExecuteAsync(string[] args);
}
