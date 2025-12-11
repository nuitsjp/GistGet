namespace GistGet;

public record Auth() : IAuth
{
    public Task LoginAsync()
    {
        throw new NotImplementedException();
    }

    public Task LogoutAsync()
    {
        throw new NotImplementedException();
    }

    public Task<string?> GetAccessTokenAsync()
    {
        throw new NotImplementedException();
    }

    public Task<bool> IsAuthenticatedAsync()
    {
        throw new NotImplementedException();
    }
}