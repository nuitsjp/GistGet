namespace GistGet;

public interface IGistGetService
{
    Task AuthLoginAsync();
    void AuthLogout();
    void AuthStatus();

    Task<WinGetPackage?> FindByIdAsync(PackageId id);
}
