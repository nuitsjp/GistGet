namespace GistGet;

public interface IGistGetService
{
    IAuth Auth { get; }

    Task<WinGetPackage?> FindByIdAsync(PackageId id);
}