using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NuitsJp.GistGet.Models;
using Octokit;

namespace NuitsJp.GistGet.Infrastructure.GitHub;

/// <summary>
/// GitHub Device Flow認証サービス
/// </summary>
public class GitHubAuthService : IGitHubAuthService
{
    // GitHub OAuth App設定 (GistGet独自OAuth App)
    private const string ClientId = "Ov23lihQJhLB6hCnEIvS"; // GistGet専用Client ID
    private const string AppName = "GistGet";
    private const string DeviceCodeUrl = "https://github.com/login/device/code";
    private const string AccessTokenUrl = "https://github.com/login/oauth/access_token";
    private readonly HttpClient _httpClient;
    private readonly ILogger<GitHubAuthService> _logger;
    private readonly string _tokenFilePath;

    public GitHubAuthService(ILogger<GitHubAuthService> logger)
    {
        _logger = logger;
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var gistGetDir = Path.Combine(appDataPath, "GistGet");
        Directory.CreateDirectory(gistGetDir);
        _tokenFilePath = Path.Combine(gistGetDir, "token.json");

        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", AppName);
    }

    /// <summary>
    /// Device Flow認証を実行
    /// </summary>
    public async Task<bool> AuthenticateAsync()
    {
        try
        {
            _logger.LogInformation("GitHub Device Flow認証を開始します...");

            // Step 1: Device Codeの取得
            var deviceCodeResponse = await RequestDeviceCodeAsync();
            if (deviceCodeResponse == null)
            {
                Console.WriteLine("Device Codeの取得に失敗しました。");
                return false;
            }

            Console.WriteLine("=== GitHub Device Flow 認証 ===");
            Console.WriteLine("ブラウザで以下のURLを開いてください:");
            Console.WriteLine($"{deviceCodeResponse.VerificationUri}");
            Console.WriteLine();
            Console.WriteLine("表示されたページで以下のコードを入力してください:");
            Console.WriteLine($"{deviceCodeResponse.UserCode}");
            Console.WriteLine();
            Console.WriteLine("認証を完了したら自動的に次に進みます...");

            // ブラウザを自動で開く
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = deviceCodeResponse.VerificationUri,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ブラウザの自動起動に失敗しました");
            }

            // Step 2: アクセストークンをポーリングで取得
            var accessToken = await PollForAccessTokenAsync(deviceCodeResponse);
            if (string.IsNullOrEmpty(accessToken))
            {
                Console.WriteLine("認証がタイムアウトまたは失敗しました。");
                return false;
            }

            // Step 3: トークンを保存
            await SaveTokenAsync(accessToken);

            // Step 4: 認証済みユーザー情報を表示
            var client = await GetAuthenticatedClientAsync();
            if (client != null)
            {
                var user = await client.User.Current();
                Console.WriteLine($"認証成功！ ユーザー: {user.Login} ({user.Name})");
            }

            _logger.LogInformation("GitHub認証が正常に完了しました");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GitHub認証中にエラーが発生しました");
            Console.WriteLine($"認証エラー: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 保存されたトークンの有効性を確認
    /// </summary>
    public async Task<bool> IsAuthenticatedAsync()
    {
        try
        {
            var client = await GetAuthenticatedClientAsync();
            if (client == null) return false;

            // 簡単なAPI呼び出しでトークンの有効性を確認
            var user = await client.User.Current();
            return user != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 認証済みのGitHubクライアントを取得
    /// </summary>
    public async Task<GitHubClient?> GetAuthenticatedClientAsync()
    {
        try
        {
            var token = await LoadTokenAsync();
            if (string.IsNullOrEmpty(token)) return null;

            var client = new GitHubClient(new ProductHeaderValue(AppName))
            {
                Credentials = new Credentials(token)
            };

            return client;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "認証済みクライアントの取得に失敗しました");
            return null;
        }
    }

    /// <summary>
    /// 認証状態を表示
    /// </summary>
    public async Task ShowAuthStatusAsync()
    {
        try
        {
            var isAuthenticated = await IsAuthenticatedAsync();
            if (isAuthenticated)
            {
                var client = await GetAuthenticatedClientAsync();
                var user = await client!.User.Current();
                Console.WriteLine($"認証済み: {user.Login} ({user.Name})");
            }
            else
            {
                Console.WriteLine("未認証: 'gistget auth' コマンドで認証してください");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"認証状態の確認でエラーが発生しました: {ex.Message}");
            _logger.LogError(ex, "認証状態の確認でエラーが発生しました");
        }
    }

    public async Task SaveTokenAsync(string token)
    {
        var tokenData = new TokenData(token, DateTime.UtcNow);

        // DPAPIで暗号化
        var jsonBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(tokenData, AppJsonContext.Default.TokenData));
        var encryptedBytes = ProtectedData.Protect(
            jsonBytes,
            null,
            DataProtectionScope.CurrentUser);

        var encryptedData = new EncryptedTokenData(
            Convert.ToBase64String(encryptedBytes),
            DateTime.UtcNow);

        var json = JsonSerializer.Serialize(encryptedData, AppJsonContext.Default.EncryptedTokenData);
        await File.WriteAllTextAsync(_tokenFilePath, json);
        _logger.LogInformation("アクセストークンを暗号化して保存しました: {TokenPath}", _tokenFilePath);
    }

    public async Task<string?> LoadTokenAsync()
    {
        if (!File.Exists(_tokenFilePath)) return null;

        try
        {
            var json = await File.ReadAllTextAsync(_tokenFilePath);
            var jsonElement = JsonSerializer.Deserialize(json, GitHubJsonContext.Default.JsonElement);

            // DPAPI暗号化形式のトークンを復号化
            if (jsonElement.TryGetProperty("EncryptedToken", out var encryptedTokenProp))
            {
                var encryptedBytes = Convert.FromBase64String(encryptedTokenProp.GetString()!);
                var decryptedBytes = ProtectedData.Unprotect(
                    encryptedBytes,
                    null,
                    DataProtectionScope.CurrentUser);

                var decryptedJson = Encoding.UTF8.GetString(decryptedBytes);
                var tokenData = JsonSerializer.Deserialize(decryptedJson, GitHubJsonContext.Default.JsonElement);
                return tokenData.GetProperty("AccessToken").GetString();
            }

            return null;
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "トークンの復号化に失敗しました。再認証が必要です。");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "トークンファイルの読み込みに失敗しました");
            return null;
        }
    }

    private async Task<DeviceCodeResponse?> RequestDeviceCodeAsync()
    {
        var requestBody = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("client_id", ClientId),
            new KeyValuePair<string, string>("scope", "gist")
        ]);

        var response = await _httpClient.PostAsync(DeviceCodeUrl, requestBody);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Device Code要求が失敗しました: {StatusCode}", response.StatusCode);
            return null;
        }

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize(json, GitHubJsonContext.Default.DeviceCodeResponse);
    }

    private async Task<string?> PollForAccessTokenAsync(DeviceCodeResponse deviceCode)
    {
        var interval = deviceCode.Interval;
        var expiresAt = DateTime.UtcNow.AddSeconds(deviceCode.ExpiresIn);

        while (DateTime.UtcNow < expiresAt)
        {
            await Task.Delay(TimeSpan.FromSeconds(interval));

            var requestBody = new FormUrlEncodedContent([
                new KeyValuePair<string, string>("client_id", ClientId),
                new KeyValuePair<string, string>("device_code", deviceCode.DeviceCode),
                new KeyValuePair<string, string>("grant_type", "urn:ietf:params:oauth:grant-type:device_code")
            ]);

            var response = await _httpClient.PostAsync(AccessTokenUrl, requestBody);
            var json = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var tokenResponse = JsonSerializer.Deserialize(json, GitHubJsonContext.Default.AccessTokenResponse);

                if (!string.IsNullOrEmpty(tokenResponse?.AccessToken)) return tokenResponse.AccessToken;
            }

            // エラーレスポンスをチェック
            var errorResponse = JsonSerializer.Deserialize(json, GitHubJsonContext.Default.OAuthErrorResponse);

            if (errorResponse?.Error == "authorization_pending")
            {
                // まだ認証されていない、続行
            }
            else if (errorResponse?.Error == "slow_down")
            {
                // レート制限、インターバルを増やす
                interval += 5;
            }
            else
            {
                // その他のエラー
                _logger.LogError("アクセストークン取得エラー: {Error}", errorResponse?.Error);
                break;
            }
        }

        return null;
    }
}