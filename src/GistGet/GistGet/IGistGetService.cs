namespace GistGet;

public interface IGistGetService
{
    Task<WinGetPackage?> FindByIdAsync(PackageId id);
}