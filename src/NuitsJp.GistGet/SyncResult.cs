// Result model for a sync operation between Gist and local packages.

namespace NuitsJp.GistGet;

/// <summary>
/// Captures the outcome of a sync operation, including changes and errors.
/// </summary>
public class SyncResult
{
    public IList<GistGetPackage> Installed { get; } = new List<GistGetPackage>();
    public IList<GistGetPackage> Uninstalled { get; } = new List<GistGetPackage>();
    public IList<GistGetPackage> PinUpdated { get; } = new List<GistGetPackage>();
    public IList<GistGetPackage> PinRemoved { get; } = new List<GistGetPackage>();
    public IDictionary<GistGetPackage, int> Failed { get; } = new Dictionary<GistGetPackage, int>();
    public bool Success => Failed.Count == 0;
}





