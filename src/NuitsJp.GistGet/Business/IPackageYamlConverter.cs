using NuitsJp.GistGet.Models;

namespace NuitsJp.GistGet.Business;

/// <summary>
/// PackageYamlConverter のインターフェース
/// テスト可能性とBusiness層の抽象化を提供
/// </summary>
public interface IPackageYamlConverter
{
    PackageCollection FromYaml(string yamlContent);
    string ToYaml(PackageCollection packages);
}