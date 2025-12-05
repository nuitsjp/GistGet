using System.Threading.Tasks;

namespace GistGet.Application.Services;

public interface IAuthService
{
    Task LoginAsync();
    Task LogoutAsync();
    Task<string?> GetAccessTokenAsync();
    Task<bool> IsAuthenticatedAsync();
    Task<GitHubUserInfo?> GetUserInfoAsync();
}


