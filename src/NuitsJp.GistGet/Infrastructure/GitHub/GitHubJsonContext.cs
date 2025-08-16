using System.Text.Json;
using System.Text.Json.Serialization;
using NuitsJp.GistGet.Models;

namespace NuitsJp.GistGet.Infrastructure.GitHub;

/// <summary>
/// GitHub API JSON処理用のソース生成コンテキスト
/// トリミング（PublishTrimmed）と AOT 対応
/// </summary>
[JsonSerializable(typeof(DeviceCodeResponse))]
[JsonSerializable(typeof(AccessTokenResponse))]
[JsonSerializable(typeof(OAuthErrorResponse))]
[JsonSerializable(typeof(JsonElement))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
internal partial class GitHubJsonContext : JsonSerializerContext
{
}