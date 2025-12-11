namespace GistGet;

using System.Diagnostics.CodeAnalysis;

public interface ICredentialService
{
    bool TryGetCredential(string target, [NotNullWhen(true)] out string? username, [NotNullWhen(true)] out string? token);
    bool SaveCredential(string target, string username, string token);
    bool DeleteCredential(string target);

}