namespace GistGet;

public interface IGistGetService
{
    Task AuthLoginAsync();
    Task AuthLogoutAsync();
    Task AuthStatusAsync();

    Task<WinGetPackage?> FindByIdAsync(PackageId id);
}

public class GistGetService(
    IAuthService authService) 
    : IGistGetService
{
    public Task AuthLoginAsync()
    {
        throw new NotImplementedException();
    }

    public Task AuthLogoutAsync()
    {
        throw new NotImplementedException();
    }

    public Task AuthStatusAsync()
    {
        throw new NotImplementedException();
    }

    public Task<WinGetPackage?> FindByIdAsync(PackageId id)
    {
        throw new NotImplementedException();
    }
}