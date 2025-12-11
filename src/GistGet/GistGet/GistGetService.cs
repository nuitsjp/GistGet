using Microsoft.Extensions.Logging;

namespace GistGet;

public class GistGetService(
    IAuthService authService,
    IConsoleService consoleService,
    ICredentialService credentialService) 
    : IGistGetService
{
    public async Task AuthLoginAsync()
    {
        await authService.LoginAsync();
    }

    public async Task AuthLogoutAsync()
    {
        await authService.LogoutAsync();
        consoleService.WriteInfo("Logged out");
    }

    public async Task AuthStatusAsync()
    {
        if (credentialService.TryGetCredential("git:https://github.com", out var user, out var token))
        {
             var maskedToken = !string.IsNullOrEmpty(token) ? new string('*', token.Length) : "**********";

             consoleService.WriteInfo("github.com");
             consoleService.WriteInfo($"  ✓ Logged in to github.com as {user} (keyring)");
             consoleService.WriteInfo($"  ✓ Token: {maskedToken}");
        }
        else
        {
            consoleService.WriteInfo("You are not logged in.");
        }
    }

    public Task<WinGetPackage?> FindByIdAsync(PackageId id)
    {
        throw new NotImplementedException();
    }
}