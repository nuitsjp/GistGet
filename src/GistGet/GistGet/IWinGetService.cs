namespace GistGet;

public interface IWinGetService
{
    WinGetPackage? FindById(PackageId id);
}