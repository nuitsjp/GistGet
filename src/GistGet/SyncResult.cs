// Result model for a sync operation between Gist and local packages.

namespace GistGet;

/// <summary>
/// Captures the outcome of a sync operation, including changes and errors.
/// </summary>
public class SyncResult
{
    public IList<GistGetPackage> Installed { get; } = new List<GistGetPackage>();
    public IList<GistGetPackage> Uninstalled { get; } = new List<GistGetPackage>();
    public IList<GistGetPackage> PinUpdated { get; } = new List<GistGetPackage>();
    public IList<GistGetPackage> PinRemoved { get; } = new List<GistGetPackage>();
    public IList<GistGetPackage> Failed { get; } = new List<GistGetPackage>();
    public IList<string> Errors { get; } = new List<string>();
    public bool Success => Errors.Count == 0 && Failed.Count == 0;
}

