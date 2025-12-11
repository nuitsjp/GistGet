namespace GistGet;

public interface IAuth
{
    Task LoginAsync();
    Task LogoutAsync();
    Task<string?> GetAccessTokenAsync();
    Task<bool> IsAuthenticatedAsync();
}