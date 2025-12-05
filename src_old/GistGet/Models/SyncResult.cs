using System.Collections.Generic;

namespace GistGet.Models;

public class SyncResult
{
    public List<GistGetPackage> Installed { get; set; } = new();
    public List<GistGetPackage> Uninstalled { get; set; } = new();
    public List<GistGetPackage> Failed { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public bool Success => Errors.Count == 0 && Failed.Count == 0;
}
