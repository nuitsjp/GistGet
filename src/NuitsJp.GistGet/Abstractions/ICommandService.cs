namespace NuitsJp.GistGet.Abstractions;

/// <summary>
/// コマンド実行サービスのインターフェース
/// </summary>
public interface ICommandService
{
    /// <summary>
    /// コマンドライン引数を解析して適切なサービスにルーティングし、実行する
    /// </summary>
    /// <param name="args">コマンドライン引数</param>
    /// <returns>プロセス終了コード</returns>
    Task<int> ExecuteAsync(string[] args);
}