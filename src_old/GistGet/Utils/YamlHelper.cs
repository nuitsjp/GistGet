using GistGet.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.Collections.Generic;

namespace GistGet.Utils;

public static class YamlHelper
{
    public static string Serialize(Dictionary<string, GistGetPackage> packages)
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults | DefaultValuesHandling.OmitNull)
            .Build();

        // Convert to a dictionary where keys are Package IDs
        // The Package object itself shouldn't contain the ID in YAML
        return serializer.Serialize(packages);
    }

    public static Dictionary<string, GistGetPackage> Deserialize(string yaml)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        var packages = deserializer.Deserialize<Dictionary<string, GistGetPackage>>(yaml)
                       ?? new Dictionary<string, GistGetPackage>();

        // Populate ID back into the package object, ensuring null entries
        // are converted to empty package definitions.
        var keys = new List<string>(packages.Keys);
        foreach (var key in keys)
        {
            var package = packages[key] ?? new GistGetPackage();
            package.Id = key;
            packages[key] = package;
        }

        return packages;
    }
}
