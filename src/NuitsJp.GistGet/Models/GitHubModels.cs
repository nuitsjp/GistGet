using System.Text.Json.Serialization;

namespace NuitsJp.GistGet.Models;

/// <summary>
/// GitHub Device Code APIレスポンス
/// </summary>
public class DeviceCodeResponse
{
    [JsonPropertyName("device_code")] public string DeviceCode { get; init; } = string.Empty;

    [JsonPropertyName("user_code")] public string UserCode { get; init; } = string.Empty;

    [JsonPropertyName("verification_uri")] public string VerificationUri { get; init; } = string.Empty;

    [JsonPropertyName("verification_uri_complete")]
    public string? VerificationUriComplete { get; init; }

    [JsonPropertyName("expires_in")] public int ExpiresIn { get; init; }

    [JsonPropertyName("interval")] public int Interval { get; init; }
}

/// <summary>
/// GitHub Access Token APIレスポンス
/// </summary>
public class AccessTokenResponse
{
    [JsonPropertyName("access_token")] public string? AccessToken { get; init; }

    [JsonPropertyName("token_type")] public string? TokenType { get; init; }

    [JsonPropertyName("scope")] public string? Scope { get; init; }
}

/// <summary>
/// GitHub OAuth エラーレスポンス
/// </summary>
public class OAuthErrorResponse
{
    [JsonPropertyName("error")] public string? Error { get; init; }

    [JsonPropertyName("error_description")]
    public string? ErrorDescription { get; init; }

    [JsonPropertyName("error_uri")] public string? ErrorUri { get; init; }
}

/// <summary>
/// WinGetパッケージ情報
/// </summary>
public class WinGetPackage
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
}

/// <summary>
/// Gist用のパッケージリストコンテナ
/// </summary>
public class PackageList
{
    public List<WinGetPackage> Packages { get; set; } = [];
    public DateTime ExportedAt { get; set; } = DateTime.UtcNow;
    public string ExportedBy { get; set; } = "GistGet";
}