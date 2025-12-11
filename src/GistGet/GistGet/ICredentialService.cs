namespace GistGet;

using System.Diagnostics.CodeAnalysis;

public interface ICredentialService
{
    bool TryGetCredential(string target, [NotNullWhen(true)] out Credential? credential);
    bool SaveCredential(string target, Credential credential);
    bool DeleteCredential(string target);

}