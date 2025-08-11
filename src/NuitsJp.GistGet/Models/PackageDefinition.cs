namespace NuitsJp.GistGet.Models;

public class PackageDefinition : IEquatable<PackageDefinition>, IComparable<PackageDefinition>
{
    private static readonly HashSet<string> ValidScopes = new(StringComparer.OrdinalIgnoreCase) { "user", "machine" };
    private static readonly HashSet<string> ValidArchitectures = new(StringComparer.OrdinalIgnoreCase) { "x86", "x64", "arm", "arm64" };

    public string Id { get; private set; }
    public string? Version { get; set; }
    public string? Uninstall { get; set; }
    public string? Architecture { get; set; }
    public string? Scope { get; set; }
    public string? Source { get; set; }
    public string? Custom { get; set; }

    public PackageDefinition(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Package ID cannot be null or empty", nameof(id));

        Id = id;
    }

    public PackageDefinition(string id, string? version = null, string? uninstall = null,
                           string? architecture = null, string? scope = null,
                           string? source = null, string? custom = null) : this(id)
    {
        Version = version;
        Uninstall = uninstall;
        Architecture = architecture;
        Scope = scope;
        Source = source;
        Custom = custom;
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Id))
            throw new ArgumentException("Package ID cannot be null or empty");

        if (!string.IsNullOrWhiteSpace(Scope) && !ValidScopes.Contains(Scope))
        {
            throw new ArgumentException($"Invalid scope '{Scope}'. Valid scopes are: {string.Join(", ", ValidScopes)}");
        }

        if (!string.IsNullOrWhiteSpace(Architecture) && !ValidArchitectures.Contains(Architecture))
        {
            throw new ArgumentException($"Invalid architecture '{Architecture}'. Valid architectures are: {string.Join(", ", ValidArchitectures)}");
        }
    }

    public bool Equals(PackageDefinition? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return string.Equals(Id, other.Id, StringComparison.OrdinalIgnoreCase);
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

    public int CompareTo(PackageDefinition? other)
    {
        if (other is null) return 1;
        return string.Compare(Id, other.Id, StringComparison.OrdinalIgnoreCase);
    }

    public override string ToString()
    {
        return Id;
    }
}