namespace GistGet.Infrastructure.Security;

public interface ICredentialService
{
    string? GetCredential(string target);
    bool SaveCredential(string target, string username, string password);
    bool DeleteCredential(string target);
}
