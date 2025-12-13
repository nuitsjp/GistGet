namespace GistGet;

/// <summary>
/// upgrade コマンドの CLI 引数を表す record。
/// 永続化用の GistGetPackage とは分離し、Presentation層からApplication層への受け渡しに使用。
/// </summary>
public record UpgradeOptions
{
    /// <summary>パッケージ ID（必須）</summary>
    public required string Id { get; init; }

    /// <summary>アップグレード先のバージョン</summary>
    public string? Version { get; init; }

    /// <summary>インストールスコープ (user | machine)</summary>
    public string? Scope { get; init; }

    /// <summary>アーキテクチャ (x86 | x64 | arm | arm64)</summary>
    public string? Architecture { get; init; }

    /// <summary>インストール先パス</summary>
    public string? Location { get; init; }

    /// <summary>ロケール (BCP47形式)</summary>
    public string? Locale { get; init; }

    /// <summary>ログファイルパス</summary>
    public string? Log { get; init; }

    /// <summary>追加のインストーラー引数</summary>
    public string? Custom { get; init; }

    /// <summary>インストーラー引数の上書き</summary>
    public string? Override { get; init; }

    /// <summary>インストーラータイプ</summary>
    public string? InstallerType { get; init; }

    /// <summary>対話型アップグレード</summary>
    public bool Interactive { get; init; }

    /// <summary>サイレントアップグレード</summary>
    public bool Silent { get; init; }

    /// <summary>強制実行</summary>
    public bool Force { get; init; }

    /// <summary>パッケージ契約に同意</summary>
    public bool AcceptPackageAgreements { get; init; }

    /// <summary>ソース契約に同意</summary>
    public bool AcceptSourceAgreements { get; init; }

    /// <summary>ハッシュ不一致を無視</summary>
    public bool AllowHashMismatch { get; init; }

    /// <summary>依存関係をスキップ</summary>
    public bool SkipDependencies { get; init; }
}
