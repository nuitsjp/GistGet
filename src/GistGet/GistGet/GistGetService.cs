using Microsoft.Extensions.Logging;

namespace GistGet;

public class GistGetService(
    IGitHubService gitHubService,
    IConsoleService consoleService,
    ICredentialService credentialService) 
    : IGistGetService
{
    public async Task AuthLoginAsync()
    {
        var credential = await gitHubService.LoginAsync();
        credentialService.SaveCredential("git:https://github.com", credential);
    }

    public void AuthLogout()
    {
        credentialService.DeleteCredential("git:https://github.com");
        consoleService.WriteInfo("Logged out");
    }

    public void AuthStatus()
    {
        if (credentialService.TryGetCredential("git:https://github.com", out var credential))
        {
             var maskedToken = !string.IsNullOrEmpty(credential.Token) ? new string('*', credential.Token.Length) : "**********";

             consoleService.WriteInfo("github.com");
             consoleService.WriteInfo($"  ✓ Logged in to github.com as {credential.Username} (keyring)");
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