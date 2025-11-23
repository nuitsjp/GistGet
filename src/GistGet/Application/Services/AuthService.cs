using GistGet.Infrastructure.Security;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GistGet.Application.Services;

public class AuthService : IAuthService
{
    private readonly ICredentialService _credentialService;
    private const string ClientId = "Ov23liao1bXwX4qJ5w0b"; // GistGet Client ID
    private const string CredentialTarget = "GistGet:GitHub:AccessToken";
    private const string CredentialUsername = "GistGet";

    public AuthService(ICredentialService credentialService)
    {
        _credentialService = credentialService;
    }

    public async Task LoginAsync()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Accept", "application/json");

        // 1. Initiate Device Flow
        var response = await client.PostAsync("https://github.com/login/device/code", 
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "client_id", ClientId },
                { "scope", "gist" }
            }));

        if (!response.IsSuccessStatusCode)
        {
            AnsiConsole.MarkupLine("[red]Failed to initiate device flow.[/]");
            return;
        }

        var deviceCode = await response.Content.ReadFromJsonAsync<DeviceCodeResponse>();
        if (deviceCode == null)
        {
            AnsiConsole.MarkupLine("[red]Failed to parse device code response.[/]");
            return;
        }

        AnsiConsole.MarkupLine($"Please visit [blue]{deviceCode.VerificationUri}[/] and enter code: [green bold]{deviceCode.UserCode}[/]");
        AnsiConsole.MarkupLine($"Waiting for authentication... (Expires in {deviceCode.ExpiresIn} seconds)");

        // 2. Poll for token
        var interval = deviceCode.Interval > 0 ? deviceCode.Interval : 5;
        var expiresAt = DateTime.UtcNow.AddSeconds(deviceCode.ExpiresIn);

        while (DateTime.UtcNow < expiresAt)
        {
            await Task.Delay(interval * 1000);

            var tokenResponse = await client.PostAsync("https://github.com/login/oauth/access_token",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "client_id", ClientId },
                    { "device_code", deviceCode.DeviceCode },
                    { "grant_type", "urn:ietf:params:oauth:grant-type:device_code" }
                }));

            if (tokenResponse.IsSuccessStatusCode)
            {
                var tokenData = await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>();
                if (tokenData != null && !string.IsNullOrEmpty(tokenData.AccessToken))
                {
                    _credentialService.SaveCredential(CredentialTarget, CredentialUsername, tokenData.AccessToken);
                    AnsiConsole.MarkupLine("[green]Successfully authenticated![/]");
                    return;
                }
                
                // Check for errors
                if (tokenData?.Error == "authorization_pending")
                {
                    continue;
                }
                else if (tokenData?.Error == "slow_down")
                {
                    interval += 5;
                    continue;
                }
                else if (tokenData?.Error == "expired_token")
                {
                    AnsiConsole.MarkupLine("[red]Code expired. Please try again.[/]");
                    return;
                }
                else if (tokenData?.Error == "access_denied")
                {
                    AnsiConsole.MarkupLine("[red]Access denied.[/]");
                    return;
                }
            }
        }
        
        AnsiConsole.MarkupLine("[red]Authentication timed out.[/]");
    }

    public Task LogoutAsync()
    {
        _credentialService.DeleteCredential(CredentialTarget);
        AnsiConsole.MarkupLine("[green]Logged out successfully.[/]");
        return Task.CompletedTask;
    }

    public Task<string?> GetAccessTokenAsync()
    {
        return Task.FromResult(_credentialService.GetCredential(CredentialTarget));
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        return !string.IsNullOrEmpty(await GetAccessTokenAsync());
    }

    private class DeviceCodeResponse
    {
        [JsonPropertyName("device_code")]
        public string DeviceCode { get; set; } = "";
        [JsonPropertyName("user_code")]
        public string UserCode { get; set; } = "";
        [JsonPropertyName("verification_uri")]
        public string VerificationUri { get; set; } = "";
        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
        [JsonPropertyName("interval")]
        public int Interval { get; set; }
    }

    private class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }
        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }
        [JsonPropertyName("scope")]
        public string? Scope { get; set; }
        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }
}
