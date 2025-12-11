namespace GistGet;

public record WinGetPackage(
    string Name,
    PackageId Id,
    Version Version,
    Version? UsableVersion);