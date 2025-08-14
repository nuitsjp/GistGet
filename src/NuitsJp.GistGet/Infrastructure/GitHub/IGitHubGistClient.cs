using System.Threading.Tasks;

namespace NuitsJp.GistGet.Infrastructure.GitHub;

/// <summary>
/// GitHubGistClient のインターフェース
/// テスト可能性とInfrastructure層の抽象化を提供
/// </summary>
public interface IGitHubGistClient
{
    Task<bool> ExistsAsync(string gistId);
    Task<string> GetFileContentAsync(string gistId, string fileName);
    Task UpdateFileContentAsync(string gistId, string fileName, string content);
}