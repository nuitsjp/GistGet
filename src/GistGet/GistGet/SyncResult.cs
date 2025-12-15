// Result model for a sync operation between Gist and local packages.

namespace GistGet;

/// <summary>
/// Captures the outcome of a sync operation, including changes and errors.
/// </summary>
public class SyncResult
{
    public List<GistGetPackage> Installed { get; set; } = new();
    public List<GistGetPackage> Uninstalled { get; set; } = new();
    public List<GistGetPackage> PinUpdated { get; set; } = new();
    public List<GistGetPackage> PinRemoved { get; set; } = new();
    public List<GistGetPackage> Failed { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public bool Success => Errors.Count == 0 && Failed.Count == 0;
}
