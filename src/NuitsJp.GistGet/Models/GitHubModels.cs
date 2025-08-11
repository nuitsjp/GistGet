using System.Text.Json.Serialization;

namespace NuitsJp.GistGet.Models;

/// <summary>
/// GitHub Device Code APIレスポンス
/// </summary>
public class DeviceCodeResponse
{
    [JsonPropertyName("device_code")]
    public string DeviceCode { get; set; } = string.Empty;

    [JsonPropertyName("user_code")]
    public string UserCode { get; set; } = string.Empty;

    [JsonPropertyName("verification_uri")]
    public string VerificationUri { get; set; } = string.Empty;

    [JsonPropertyName("verification_uri_complete")]
    public string? VerificationUriComplete { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("interval")]
    public int Interval { get; set; }
}

/// <summary>
/// GitHub Access Token APIレスポンス
/// </summary>
public class AccessTokenResponse
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("token_type")]
    public string? TokenType { get; set; }

    [JsonPropertyName("scope")]
    public string? Scope { get; set; }
}

/// <summary>
/// GitHub OAuth エラーレスポンス
/// </summary>
public class OAuthErrorResponse
{
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("error_description")]
    public string? ErrorDescription { get; set; }

    [JsonPropertyName("error_uri")]
    public string? ErrorUri { get; set; }
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
    public List<WinGetPackage> Packages { get; set; } = new();
    public DateTime ExportedAt { get; set; } = DateTime.UtcNow;
    public string ExportedBy { get; set; } = "GistGet";
}
