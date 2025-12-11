namespace GistGet;

public interface IAuthService
{
    Task LoginAsync();
    Task LogoutAsync();
    Task StatusAsync();
}
