namespace GistGet;

public interface IGistGetService
{
    Task AuthLoginAsync();
    Task AuthLogoutAsync();
    Task AuthStatusAsync();

    Task<WinGetPackage?> FindByIdAsync(PackageId id);
}
