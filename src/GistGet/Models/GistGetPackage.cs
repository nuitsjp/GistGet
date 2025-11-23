using YamlDotNet.Serialization;

namespace GistGet.Models;

public class GistGetPackage
{
    [YamlIgnore]
    public string Id { get; set; } = "";

    [YamlMember(Alias = "version", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public string? Version { get; set; }

    [YamlMember(Alias = "custom", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public string? Custom { get; set; }

    [YamlMember(Alias = "uninstall", DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)]
    public bool Uninstall { get; set; }

    // Additional properties for full compatibility if needed
    [YamlMember(Alias = "scope", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public string? Scope { get; set; }

    [YamlMember(Alias = "architecture", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public string? Architecture { get; set; }
    
    [YamlMember(Alias = "location", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public string? Location { get; set; }
    
    [YamlMember(Alias = "locale", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public string? Locale { get; set; }
    
    [YamlMember(Alias = "allowHashMismatch", DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)]
    public bool AllowHashMismatch { get; set; }
    
    [YamlMember(Alias = "force", DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)]
    public bool Force { get; set; }
    
    [YamlMember(Alias = "skipDependencies", DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)]
    public bool SkipDependencies { get; set; }
}
