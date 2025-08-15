using NuitsJp.GistGet.Models;

namespace NuitsJp.GistGet.Business.Models;

/// <summary>
/// syncコマンドの実行計画を表すモデル
/// Gist定義とローカル状態の差分検出結果を格納
/// </summary>
public class SyncPlan
{
    /// <summary>
    /// インストール対象のパッケージリスト
    /// </summary>
    public List<PackageDefinition> ToInstall { get; set; } = [];

    /// <summary>
    /// アンインストール対象のパッケージリスト（Uninstallフラグがtrueのもの）
    /// </summary>
    public List<PackageDefinition> ToUninstall { get; set; } = [];

    /// <summary>
    /// 既にインストール済みのパッケージリスト（冪等性確保のためスキップ）
    /// </summary>
    public List<PackageDefinition> AlreadyInstalled { get; set; } = [];

    /// <summary>
    /// Gist定義に存在するが、WinGetカタログで見つからないパッケージリスト
    /// </summary>
    public List<PackageDefinition> NotFound { get; set; } = [];

    /// <summary>
    /// 計画が空（実行すべき操作がない）かどうかを判定
    /// </summary>
    public bool IsEmpty => ToInstall.Count == 0 && ToUninstall.Count == 0;

    /// <summary>
    /// 計画の総パッケージ数を取得
    /// </summary>
    public int TotalCount => ToInstall.Count + ToUninstall.Count + AlreadyInstalled.Count + NotFound.Count;
}