using System.Text.Json;
using System.Text.Json.Serialization;

namespace NuitsJp.GistGet.Models;

/// <summary>
/// アプリケーション内部のJSON処理用のソース生成コンテキスト
/// トリミング（PublishTrimmed）と AOT 対応
/// PascalCase命名規則を使用
/// </summary>
[JsonSerializable(typeof(GistConfiguration))]
[JsonSerializable(typeof(PackageList))]
[JsonSerializable(typeof(WinGetPackage))]
[JsonSerializable(typeof(List<WinGetPackage>))]
[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(TokenData))]
[JsonSerializable(typeof(EncryptedTokenData))]
internal partial class AppJsonContext : JsonSerializerContext
{
}

/// <summary>
/// トークン情報（DPAPI暗号化前）
/// </summary>
public record TokenData(string AccessToken, DateTime CreatedAt);

/// <summary>
/// 暗号化されたトークン情報（ファイル保存用）
/// </summary>
public record EncryptedTokenData(string EncryptedToken, DateTime CreatedAt);