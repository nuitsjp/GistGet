namespace GistGet;

public interface IAuthService
{
    Task LoginAsync();
    Task LogoutAsync();
    Task StatusAsync();
    Task<string?> GetAccessTokenAsync();
    Task<bool> IsAuthenticatedAsync();
}
