using NuitsJp.GistGet.Models;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace NuitsJp.GistGet.Business;

public class PackageYamlConverter : IPackageYamlConverter
{
    private readonly IDeserializer _deserializer;
    private readonly ISerializer _serializer;

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

            // 空のコレクションの場合は空の辞書を返す
            if (!sortedPackages.Any())
            {
                return "{}";
            }

            // 辞書形式でシリアライズ
            var dictionaryModel = new Dictionary<string, object>();

            foreach (var package in sortedPackages)
            {
                var properties = new Dictionary<string, object>();

                // null/空文字以外のプロパティのみ追加
                if (!string.IsNullOrWhiteSpace(package.Version))
                    properties["version"] = package.Version;
                if (package.Uninstall.HasValue)
                    properties["uninstall"] = package.Uninstall.Value;
                if (!string.IsNullOrWhiteSpace(package.Architecture))
                    properties["architecture"] = package.Architecture;
                if (!string.IsNullOrWhiteSpace(package.Scope))
                    properties["scope"] = package.Scope;
                if (!string.IsNullOrWhiteSpace(package.Source))
                    properties["source"] = package.Source;
                if (!string.IsNullOrWhiteSpace(package.Custom))
                    properties["custom"] = package.Custom;
                if (package.AllowHashMismatch.HasValue)
                    properties["allowHashMismatch"] = package.AllowHashMismatch.Value;
                if (package.Force.HasValue)
                    properties["force"] = package.Force.Value;
                if (!string.IsNullOrWhiteSpace(package.Header))
                    properties["header"] = package.Header;
                if (!string.IsNullOrWhiteSpace(package.InstallerType))
                    properties["installerType"] = package.InstallerType;
                if (!string.IsNullOrWhiteSpace(package.Locale))
                    properties["locale"] = package.Locale;
                if (!string.IsNullOrWhiteSpace(package.Location))
                    properties["location"] = package.Location;
                if (!string.IsNullOrWhiteSpace(package.Log))
                    properties["log"] = package.Log;
                if (!string.IsNullOrWhiteSpace(package.Mode))
                    properties["mode"] = package.Mode;
                if (!string.IsNullOrWhiteSpace(package.Override))
                    properties["override"] = package.Override;
                if (package.SkipDependencies.HasValue)
                    properties["skipDependencies"] = package.SkipDependencies.Value;
                if (package.Confirm.HasValue)
                    properties["confirm"] = package.Confirm.Value;
                if (package.WhatIf.HasValue)
                    properties["whatIf"] = package.WhatIf.Value;

                // プロパティがない場合はnullを設定
                dictionaryModel[package.Id] = properties.Count > 0 ? (object)properties : null!;
            }

            return _serializer.Serialize(dictionaryModel);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to serialize packages to YAML: {ex.Message}", ex);
        }
    }

    public PackageCollection FromYaml(string yaml)
    {
        if (string.IsNullOrWhiteSpace(yaml))
            return new PackageCollection();

        try
        {
            // 辞書形式として解析を試行
            return TryDeserializeDictionaryFormat(yaml) ?? TryDeserializeLegacyFormat(yaml);
        }
        catch (YamlException ex)
        {
            throw new ArgumentException($"Invalid YAML format: {ex.Message}", nameof(yaml), ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to deserialize YAML to packages: {ex.Message}", ex);
        }
    }

    private PackageCollection? TryDeserializeDictionaryFormat(string yaml)
    {
        try
        {
            var dictionaryModel = _deserializer.Deserialize<Dictionary<string, PackageYamlProperties>>(yaml);

            if (dictionaryModel == null) return null;

            var collection = new PackageCollection();
            foreach (var kvp in dictionaryModel)
            {
                if (string.IsNullOrWhiteSpace(kvp.Key))
                    continue;

                var props = kvp.Value ?? new PackageYamlProperties();
                var package = new PackageDefinition(
                    kvp.Key,
                    props.Version,
                    props.Uninstall,
                    props.Architecture,
                    props.Scope,
                    props.Source,
                    props.Custom,
                    props.AllowHashMismatch,
                    props.Force,
                    props.Header,
                    props.InstallerType,
                    props.Locale,
                    props.Location,
                    props.Log,
                    props.Mode,
                    props.Override,
                    props.SkipDependencies,
                    props.Confirm,
                    props.WhatIf
                );

                collection.Add(package);
            }

            return collection;
        }
        catch (YamlException)
        {
            return null; // 辞書形式で失敗した場合はnullを返す
        }
    }

    private PackageCollection TryDeserializeLegacyFormat(string yaml)
    {
        var yamlModel = _deserializer.Deserialize<PackageYamlModel>(yaml);

        if (yamlModel?.Packages == null) return [];

        var collection = new PackageCollection();
        foreach (var item in yamlModel.Packages)
        {
            if (string.IsNullOrWhiteSpace(item.Id))
                continue;

            var package = new PackageDefinition(
                item.Id,
                item.Version,
                item.Uninstall,
                item.Architecture,
                item.Scope,
                item.Source,
                item.Custom
            );

            collection.Add(package);
        }

        return collection;
    }

    private class PackageYamlModel
    {
        public List<PackageYamlItem> Packages { get; set; } = [];
    }

    private class PackageYamlItem
    {
        public string Id { get; init; } = string.Empty;
        public string? Version { get; init; }
        public bool? Uninstall { get; init; }
        public string? Architecture { get; init; }
        public string? Scope { get; init; }
        public string? Source { get; init; }
        public string? Custom { get; init; }
    }

    private class PackageYamlProperties
    {
        public string? Version { get; init; }
        public bool? Uninstall { get; init; }
        public string? Architecture { get; init; }
        public string? Scope { get; init; }
        public string? Source { get; init; }
        public string? Custom { get; init; }
        public bool? AllowHashMismatch { get; init; }
        public bool? Force { get; init; }
        public string? Header { get; init; }
        public string? InstallerType { get; init; }
        public string? Locale { get; init; }
        public string? Location { get; init; }
        public string? Log { get; init; }
        public string? Mode { get; init; }
        public string? Override { get; init; }
        public bool? SkipDependencies { get; init; }
        public bool? Confirm { get; init; }
        public bool? WhatIf { get; init; }
    }
}