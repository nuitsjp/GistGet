namespace GistGet;

public interface IGitHubService
{
    Task<Credential> LoginAsync();

    Task StatusAsync();
}
