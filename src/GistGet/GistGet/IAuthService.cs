namespace GistGet;

public interface IAuthService
{
    Task LoginAsync();
    Task LogoutAsync();
    Task<string?> GetAccessTokenAsync();
    Task<bool> IsAuthenticatedAsync();
}
