namespace GistGet;

using System.Diagnostics.CodeAnalysis;

public interface ICredentialService
{
    bool TryGetCredential([NotNullWhen(true)] out Credential? credential);
    bool SaveCredential(Credential credential);
    bool DeleteCredential();

}