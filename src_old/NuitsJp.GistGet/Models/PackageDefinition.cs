namespace NuitsJp.GistGet.Models;

public class PackageDefinition : IEquatable<PackageDefinition>, IComparable<PackageDefinition>
{
    private static readonly HashSet<string> ValidScopes = new(StringComparer.OrdinalIgnoreCase) { "user", "machine" };

    private static readonly HashSet<string> ValidArchitectures = new(StringComparer.OrdinalIgnoreCase)
        { "x86", "x64", "arm", "arm64" };

    private static readonly HashSet<string> ValidModes = new(StringComparer.OrdinalIgnoreCase)
        { "default", "silent", "interactive" };

    private static readonly HashSet<string> ValidInstallerTypes = new(StringComparer.OrdinalIgnoreCase)
        { "exe", "msi", "msix", "zip", "appx", "burn", "inno", "nullsoft", "portable" };

    public PackageDefinition(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Package ID cannot be null or empty", nameof(id));

        Id = id;
    }

    public PackageDefinition(string id, string? version = null, bool? uninstall = null,
        string? architecture = null, string? scope = null,
        string? source = null, string? custom = null,
        bool? allowHashMismatch = null, bool? force = null, string? header = null,
        string? installerType = null, string? locale = null, string? location = null,
        string? log = null, string? mode = null, string? overrideArgs = null,
        bool? skipDependencies = null, bool? confirm = null, bool? whatIf = null) : this(id)
    {
        Version = version;
        Uninstall = uninstall;
        Architecture = architecture;
        Scope = scope;
        Source = source;
        Custom = custom;
        AllowHashMismatch = allowHashMismatch;
        Force = force;
        Header = header;
        InstallerType = installerType;
        Locale = locale;
        Location = location;
        Log = log;
        Mode = mode;
        Override = overrideArgs;
        SkipDependencies = skipDependencies;
        Confirm = confirm;
        WhatIf = whatIf;
    }

    public string Id { get; }
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

    public int CompareTo(PackageDefinition? other)
    {
        if (other is null) return 1;
        return string.Compare(Id, other.Id, StringComparison.OrdinalIgnoreCase);
    }

    public bool Equals(PackageDefinition? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return string.Equals(Id, other.Id, StringComparison.OrdinalIgnoreCase);
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Id))
            throw new ArgumentException("Package ID cannot be null or empty");

        if (!string.IsNullOrWhiteSpace(Scope) && !ValidScopes.Contains(Scope))
            throw new ArgumentException($"Invalid scope '{Scope}'. Valid scopes are: {string.Join(", ", ValidScopes)}");

        if (!string.IsNullOrWhiteSpace(Architecture) && !ValidArchitectures.Contains(Architecture))
            throw new ArgumentException(
                $"Invalid architecture '{Architecture}'. Valid architectures are: {string.Join(", ", ValidArchitectures)}");

        if (!string.IsNullOrWhiteSpace(Mode) && !ValidModes.Contains(Mode))
            throw new ArgumentException($"Invalid mode '{Mode}'. Valid modes are: {string.Join(", ", ValidModes)}");

        if (!string.IsNullOrWhiteSpace(InstallerType) && !ValidInstallerTypes.Contains(InstallerType))
            throw new ArgumentException($"Invalid installer type '{InstallerType}'. Valid types are: {string.Join(", ", ValidInstallerTypes)}");
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as PackageDefinition);
    }

    public override int GetHashCode()
    {
        return StringComparer.OrdinalIgnoreCase.GetHashCode(Id);
    }

    public static bool operator ==(PackageDefinition? left, PackageDefinition? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(PackageDefinition? left, PackageDefinition? right)
    {
        return !Equals(left, right);
    }

    public override string ToString()
    {
        return Id;
    }
}