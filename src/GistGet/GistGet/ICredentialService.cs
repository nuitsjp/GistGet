namespace GistGet;

using System.Diagnostics.CodeAnalysis;

public interface ICredentialService
{
    bool TryGetCredential(string target, [NotNullWhen(true)] out string? password);
    bool SaveCredential(string target, string username, string password);
    bool DeleteCredential(string target);

}