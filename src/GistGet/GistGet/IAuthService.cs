namespace GistGet;

public interface IAuthService
{
    Task<Credential> LoginAsync();

    Task StatusAsync();
}
