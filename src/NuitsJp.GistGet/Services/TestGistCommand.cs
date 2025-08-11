using Microsoft.Extensions.Logging;
using NuitsJp.GistGet.Abstractions;

namespace NuitsJp.GistGet.Services;

/// <summary>
/// Gist読み取りテストコマンドの実装
/// </summary>
public class TestGistCommand
{
    private readonly IGitHubAuthService _authService;
    private readonly ILogger<TestGistCommand> _logger;

    public TestGistCommand(IGitHubAuthService authService, ILogger<TestGistCommand> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Gist読み取りテストコマンドを実行
    /// </summary>
    public async Task<int> ExecuteAsync()
    {
        try
        {
            Console.WriteLine("=== Gist読み取りテスト ===");
            
            // 認証確認
            var client = await _authService.GetAuthenticatedClientAsync();
            if (client == null)
            {
                Console.WriteLine("認証されていません。先に 'gistget auth' を実行してください。");
                return 1;
            }

            Console.WriteLine("認証済みGitHubクライアントを取得しました。");
            
            // 現在のユーザー情報を表示
            var user = await client.User.Current();
            Console.WriteLine($"ユーザー: {user.Login} ({user.Name})");
            Console.WriteLine();
            
            // Gist一覧を取得
            Console.WriteLine("Gist一覧を取得中...");
            var gists = await client.Gist.GetAllForUser(user.Login);
            
            Console.WriteLine($"取得したGist数: {gists.Count}");
            Console.WriteLine();
            
            if (gists.Count == 0)
            {
                Console.WriteLine("Gistが見つかりませんでした。");
                Console.WriteLine("テスト用にGistを作成することをお勧めします。");
                return 0;
            }
            
            // 最初の数個のGistを詳細表示
            var displayCount = Math.Min(5, gists.Count);
            Console.WriteLine($"最初の{displayCount}個のGistを表示:");
            Console.WriteLine(new string('-', 60));
            
            for (int i = 0; i < displayCount; i++)
            {
                var gist = gists[i];
                Console.WriteLine($"[{i + 1}] {gist.Id}");
                Console.WriteLine($"    説明: {gist.Description ?? "(説明なし)"}");
                Console.WriteLine($"    公開: {(gist.Public ? "Yes" : "No")}");
                Console.WriteLine($"    作成日: {gist.CreatedAt:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"    更新日: {gist.UpdatedAt:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"    ファイル数: {gist.Files.Count}");
                
                if (gist.Files.Count > 0)
                {
                    Console.WriteLine("    ファイル:");
                    foreach (var file in gist.Files.Take(3))
                    {
                        Console.WriteLine($"      - {file.Key} ({file.Value.Size} bytes)");
                    }
                    if (gist.Files.Count > 3)
                    {
                        Console.WriteLine($"      ... 他 {gist.Files.Count - 3} ファイル");
                    }
                }
                Console.WriteLine();
            }
            
            if (gists.Count > displayCount)
            {
                Console.WriteLine($"... 他 {gists.Count - displayCount} 個のGist");
            }
            
            Console.WriteLine("✅ Gist読み取りテストが正常に完了しました！");
            _logger.LogInformation("Gist読み取りテストが正常に完了しました。取得Gist数: {GistCount}", gists.Count);
            
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gist読み取りテスト中にエラーが発生しました");
            Console.WriteLine($"エラー: {ex.Message}");
            return 1;
        }
    }
}
