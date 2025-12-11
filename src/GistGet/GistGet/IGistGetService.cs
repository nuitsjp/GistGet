namespace GistGet;

public interface IGistGetService
{
    Task AuthLoginAsync();
    Task AuthLogoutAsync();
    void AuthStatus();

    Task<WinGetPackage?> FindByIdAsync(PackageId id);
}
