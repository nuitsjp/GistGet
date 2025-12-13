namespace GistGet;

/// <summary>
/// uninstall コマンドの CLI 引数を表す record。
/// 永続化用の GistGetPackage とは分離し、Presentation層からApplication層への受け渡しに使用。
/// </summary>
public record UninstallOptions
{
    /// <summary>パッケージ ID（必須）</summary>
    public required string Id { get; init; }

    /// <summary>インストールスコープ (user | machine)</summary>
    public string? Scope { get; init; }

    /// <summary>対話型アンインストール</summary>
    public bool Interactive { get; init; }

    /// <summary>サイレントアンインストール</summary>
    public bool Silent { get; init; }

    /// <summary>強制実行</summary>
    public bool Force { get; init; }
}
