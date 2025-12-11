namespace GistGet;

public interface IAuthService
{
    Task<Credential> LoginAsync();
    Task LogoutAsync();
    Task StatusAsync();
}
