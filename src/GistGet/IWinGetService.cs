namespace GistGet;

public interface IWinGetService
{
    Task<WinGetPackage> FindByIdAsync(PackageId id);
}