namespace NuitsJp.GistGet.Presentation;

/// <summary>
/// コマンドルーティングサービスのインターフェース
/// </summary>
public interface ICommandRouter
{
    /// <summary>
    /// コマンドライン引数を解析して適切なサービスにルーティングし、実行する
    /// </summary>
    /// <param name="args">コマンドライン引数</param>
    /// <returns>プロセス終了コード</returns>
    Task<int> ExecuteAsync(string[] args);
}