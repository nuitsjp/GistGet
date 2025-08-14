using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using NuitsJp.GistGet.Models;

namespace NuitsJp.GistGet.Business;

public class PackageYamlConverter : IPackageYamlConverter
{
    private readonly ISerializer _serializer;
    private readonly IDeserializer _deserializer;

    public PackageYamlConverter()
    {
        _serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
            .Build();

        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    public string ToYaml(PackageCollection packages)
    {
        if (packages == null)
            throw new ArgumentNullException(nameof(packages));

        try
        {
            var sortedPackages = packages.ToSortedList();
            var yamlModel = new PackageYamlModel
            {
                Packages = sortedPackages.Select(p => new PackageYamlItem
                {
                    Id = p.Id,
                    Version = string.IsNullOrWhiteSpace(p.Version) ? null : p.Version,
                    Uninstall = string.IsNullOrWhiteSpace(p.Uninstall) ? null : p.Uninstall,
                    Architecture = string.IsNullOrWhiteSpace(p.Architecture) ? null : p.Architecture,
                    Scope = string.IsNullOrWhiteSpace(p.Scope) ? null : p.Scope,
                    Source = string.IsNullOrWhiteSpace(p.Source) ? null : p.Source,
                    Custom = string.IsNullOrWhiteSpace(p.Custom) ? null : p.Custom
                }).ToList()
            };

            return _serializer.Serialize(yamlModel);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to serialize packages to YAML: {ex.Message}", ex);
        }
    }

    public PackageCollection FromYaml(string yaml)
    {
        if (string.IsNullOrWhiteSpace(yaml))
            throw new ArgumentException("YAML content cannot be null or empty", nameof(yaml));

        try
        {
            var yamlModel = _deserializer.Deserialize<PackageYamlModel>(yaml);

            if (yamlModel?.Packages == null)
            {
                return new PackageCollection();
            }

            var collection = new PackageCollection();
            foreach (var item in yamlModel.Packages)
            {
                if (string.IsNullOrWhiteSpace(item.Id))
                    continue;

                var package = new PackageDefinition(
                    id: item.Id,
                    version: item.Version,
                    uninstall: item.Uninstall,
                    architecture: item.Architecture,
                    scope: item.Scope,
                    source: item.Source,
                    custom: item.Custom
                );

                collection.Add(package);
            }

            return collection;
        }
        catch (YamlDotNet.Core.YamlException ex)
        {
            throw new ArgumentException($"Invalid YAML format: {ex.Message}", nameof(yaml), ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to deserialize YAML to packages: {ex.Message}", ex);
        }
    }

    private class PackageYamlModel
    {
        public List<PackageYamlItem> Packages { get; set; } = new();
    }

    private class PackageYamlItem
    {
        public string Id { get; set; } = string.Empty;
        public string? Version { get; set; }
        public string? Uninstall { get; set; }
        public string? Architecture { get; set; }
        public string? Scope { get; set; }
        public string? Source { get; set; }
        public string? Custom { get; set; }
    }
}