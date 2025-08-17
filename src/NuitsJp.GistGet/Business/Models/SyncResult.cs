namespace NuitsJp.GistGet.Business.Models;

/// <summary>
/// syncコマンドの実行結果を表すモデル
/// </summary>
public class SyncResult
{
    /// <summary>
    /// 正常にインストールされたパッケージIDのリスト
    /// </summary>
    public List<string> InstalledPackages { get; } = [];

    /// <summary>
    /// 正常にアンインストールされたパッケージIDのリスト
    /// </summary>
    public List<string> UninstalledPackages { get; } = [];

    /// <summary>
    /// インストールまたはアンインストールに失敗したパッケージIDのリスト
    /// </summary>
    public List<string> FailedPackages { get; } = [];

    /// <summary>
    /// 再起動が必要かどうかを示すフラグ
    /// </summary>
    public bool RebootRequired { get; set; }

    /// <summary>
    /// プロセスの終了コード（0: 成功、1: エラー）
    /// </summary>
    public int ExitCode { get; set; }

    /// <summary>
    /// 実行結果が成功かどうかを判定
    /// </summary>
    public bool IsSuccess => ExitCode == 0 && FailedPackages.Count == 0;

    /// <summary>
    /// 処理されたパッケージの総数を取得
    /// </summary>
    public int TotalProcessedCount => InstalledPackages.Count + UninstalledPackages.Count + FailedPackages.Count;

    /// <summary>
    /// 何らかの変更が行われたかどうかを判定
    /// </summary>
    public bool HasChanges => InstalledPackages.Count > 0 || UninstalledPackages.Count > 0;
}