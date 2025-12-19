// YAML serialization utilities for the GistGet package manifest.

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace GistGet;

/// <summary>
/// Serializes and deserializes GistGet package manifests for storage.
/// </summary>
public static class GistGetPackageSerializer
{
    /// <summary>
    /// Serializes packages to YAML.
    /// </summary>
    public static string Serialize(IReadOnlyList<GistGetPackage> packages)
    {
        var dict = new Dictionary<string, GistGetPackage?>(StringComparer.OrdinalIgnoreCase);
        foreach (var package in packages.OrderBy(p => p.Id, StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(package.Id))
            {
                throw new ArgumentException("Package Id is required.", nameof(packages));
            }

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

            dict[package.Id] = HasValues(copy) ? copy : null;
        }

        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults | DefaultValuesHandling.OmitNull)
            .Build();

        return serializer.Serialize(dict);

        static bool HasValues(GistGetPackage package)
        {
            return !string.IsNullOrWhiteSpace(package.Version)
                   || !string.IsNullOrWhiteSpace(package.Pin)
                   || !string.IsNullOrWhiteSpace(package.PinType)
                   || !string.IsNullOrWhiteSpace(package.Custom)
                   || package.Uninstall
                   || !string.IsNullOrWhiteSpace(package.Scope)
                   || !string.IsNullOrWhiteSpace(package.Architecture)
                   || !string.IsNullOrWhiteSpace(package.Location)
                   || !string.IsNullOrWhiteSpace(package.Locale)
                   || package.AllowHashMismatch
                   || package.Force
                   || package.AcceptPackageAgreements
                   || package.AcceptSourceAgreements
                   || package.SkipDependencies
                   || !string.IsNullOrWhiteSpace(package.Header)
                   || !string.IsNullOrWhiteSpace(package.InstallerType)
                   || !string.IsNullOrWhiteSpace(package.Log)
                   || !string.IsNullOrWhiteSpace(package.Override)
                   || package.Interactive
                   || package.Silent;
        }
    }

    /// <summary>
    /// Deserializes YAML into packages.
    /// </summary>
    public static IReadOnlyList<GistGetPackage> Deserialize(string yaml)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

#pragma warning disable NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
        var dict = deserializer.Deserialize<Dictionary<string, GistGetPackage>>(yaml);
        dict ??= new Dictionary<string, GistGetPackage>(StringComparer.OrdinalIgnoreCase);
#pragma warning restore NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract

        var list = new List<GistGetPackage>();
        foreach (var (id, package) in dict)
        {
#pragma warning disable NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
            var item = package ?? new GistGetPackage();
#pragma warning restore NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
            item.Id = id;
            list.Add(item);
        }

        return list.OrderBy(p => p.Id, StringComparer.OrdinalIgnoreCase).ToList();
    }
}
