using System.Xml.Serialization;
using YamlDotNet.Serialization;

namespace GistGet;

public class GistGetPackage
{
    [YamlIgnore]
    public string Id { get; set; } = "";

    [YamlMember(Alias = "version", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public string? Version { get; set; }

    [YamlMember(Alias = "pin", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public string? Pin { get; set; }

    [YamlMember(Alias = "pinType", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public string? PinType { get; set; }

    [YamlMember(Alias = "custom", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public string? Custom { get; set; }

    [YamlMember(Alias = "uninstall", DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)]
    public bool Uninstall { get; set; }

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

    [YamlMember(Alias = "acceptPackageAgreements", DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)]
    public bool AcceptPackageAgreements { get; set; }

    [YamlMember(Alias = "acceptSourceAgreements", DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)]
    public bool AcceptSourceAgreements { get; set; }

    [YamlMember(Alias = "skipDependencies", DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)]
    public bool SkipDependencies { get; set; }

    [YamlMember(Alias = "header", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public string? Header { get; set; }

    [YamlMember(Alias = "installerType", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public string? InstallerType { get; set; }

    [YamlMember(Alias = "log", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public string? Log { get; set; }



    [YamlMember(Alias = "override", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public string? Override { get; set; }





    [YamlMember(Alias = "interactive", DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)]
    public bool Interactive { get; set; }

    [YamlMember(Alias = "silent", DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)]
    public bool Silent { get; set; }
}
