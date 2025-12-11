using System.Diagnostics;
using GistGet.Presentation;
using Octokit;

namespace GistGet.Infrastructure.GitHub;

public class AuthService(
    ICredentialService credentialService,
    IConsoleService consoleService) : IAuthService
{
    private const string ClientId = "Ov23lihQJhLB6hCnEIvS"; // GistGet Client ID
    private const string GitHubTarget = "git:https://github.com";
    private const string ProductHeader = "GistGet";

    public async Task<Credential> LoginAsync()
    {
        var client = new GitHubClient(new ProductHeaderValue(ProductHeader));
        var request = new OauthDeviceFlowRequest(ClientId);

        var deviceFlowResponse = await client.Oauth.InitiateDeviceFlow(request);

        consoleService.WriteInfo("First sequence of Device Flow complete.");
        consoleService.WriteInfo($"Please visit {deviceFlowResponse.VerificationUri} and enter the code: {deviceFlowResponse.UserCode}");
        
        // Open browser automatically if possible, or just let user do it.
        // The requirement says "display code and URL".
        try 
        {
            Process.Start(new ProcessStartInfo(deviceFlowResponse.VerificationUri) { UseShellExecute = true });
        }
        catch { /* Ignore if browser cannot be opened */ }

        var token = await client.Oauth.CreateAccessTokenForDeviceFlow(ClientId, deviceFlowResponse);

        client.Credentials = new Credentials(token.AccessToken);
        var user = await client.User.Current();

        return new Credential(user.Login, token.AccessToken);
    }



    public Task StatusAsync()
    {
        // Status check is handled by GistGetService currently, as per design interpretation in plan.
        // Plan says: "Task.CompletedTask を返す (または実装なし)"
        return Task.CompletedTask;
    }
}