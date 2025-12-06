namespace NuitsJp.GistGet.Infrastructure.Os;

/// <summary>
/// オペレーティングシステム操作のインターフェース
/// テスタビリティとモック化のためのシステム操作抽象化
/// </summary>
public interface IOsService
{
    /// <summary>
    /// システム再起動を実行
    /// </summary>
    /// <returns>再起動コマンドの実行タスク</returns>
    Task ExecuteRebootAsync();

    /// <summary>
    /// システムシャットダウンを実行
    /// </summary>
    /// <returns>シャットダウンコマンドの実行タスク</returns>
    // ReSharper disable once UnusedMember.Global
    Task ExecuteShutdownAsync();
}