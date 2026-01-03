// Abstraction for storing and retrieving credentials.

namespace NuitsJp.GistGet;

/// <summary>
/// Defines operations for persisting and accessing authentication credentials.
/// </summary>
public interface ICredentialService
{
    /// <summary>
    /// Attempts to read the stored credential.
    /// </summary>
    bool TryGetCredential(out Credential credential);

    /// <summary>
    /// Persists a credential for later use.
    /// </summary>
    bool SaveCredential(Credential credential);

    /// <summary>
    /// Deletes any stored credential.
    /// </summary>
    bool DeleteCredential();
}




