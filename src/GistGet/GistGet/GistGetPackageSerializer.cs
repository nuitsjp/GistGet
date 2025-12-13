using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace GistGet;

/// <summary>
/// GistGetPackage の YAML シリアライズ/デシリアライズを担当する静的クラス。
/// packages.yaml 形式への変換と読み込みを提供します。
/// </summary>
public static class GistGetPackageSerializer
{
    /// <summary>
    /// パッケージ一覧を YAML 形式の文字列にシリアライズします。
    /// パッケージ ID をキーとしたマップ形式で出力されます。
    /// </summary>
    /// <param name="packages">シリアライズするパッケージ一覧</param>
    /// <returns>YAML 形式の文字列</returns>
    /// <exception cref="ArgumentException">パッケージ ID が未設定の場合</exception>
    public static string Serialize(IReadOnlyList<GistGetPackage> packages)
    {
        var dict = new Dictionary<string, GistGetPackage>(StringComparer.OrdinalIgnoreCase);
        foreach (var package in packages)
        {
            if (string.IsNullOrWhiteSpace(package.Id))
            {
                throw new ArgumentException("Package Id is required.", nameof(packages));
            }

            // ID 以外のプロパティをコピー（ID は YAML キーとして使用）
            var copy = new GistGetPackage
            {
                Version = package.Version,
                Pin = package.Pin,
                PinType = package.PinType,
                Custom = package.Custom,
                Uninstall = package.Uninstall,
                Scope = package.Scope,
                Architecture = package.Architecture,
                Location = package.Location,
                Locale = package.Locale,
                AllowHashMismatch = package.AllowHashMismatch,
                Force = package.Force,
                AcceptPackageAgreements = package.AcceptPackageAgreements,
                AcceptSourceAgreements = package.AcceptSourceAgreements,
                SkipDependencies = package.SkipDependencies,
                Header = package.Header,
                InstallerType = package.InstallerType,
                Log = package.Log,

                Override = package.Override,

                Interactive = package.Interactive,
                Silent = package.Silent
            };

            dict[package.Id] = copy;
        }

        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults | DefaultValuesHandling.OmitNull)
            .Build();

        return serializer.Serialize(dict);
    }

    /// <summary>
    /// YAML 形式の文字列をパッケージ一覧にデシリアライズします。
    /// パッケージ ID は YAML のキーから設定されます。
    /// </summary>
    /// <param name="yaml">YAML 形式の文字列</param>
    /// <returns>パッケージ一覧（ID 順でソート済み）</returns>
    public static IReadOnlyList<GistGetPackage> Deserialize(string yaml)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        var dict = deserializer.Deserialize<Dictionary<string, GistGetPackage>>(yaml)
                   ?? new Dictionary<string, GistGetPackage>();

        var list = new List<GistGetPackage>();
        foreach (var (id, package) in dict)
        {
            var item = package ?? new GistGetPackage();
            item.Id = id;
            list.Add(item);
        }

        return list.OrderBy(p => p.Id, StringComparer.OrdinalIgnoreCase).ToList();
    }
}
