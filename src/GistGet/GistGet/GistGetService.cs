using Microsoft.Extensions.Logging;

namespace GistGet;

public class GistGetService(
    IAuthService authService,
    IConsoleService consoleService) 
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