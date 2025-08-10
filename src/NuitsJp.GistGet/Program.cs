namespace NuitsJp.GistGet;

/// <summary>
/// MVP Phase 2: コマンドルーティング実装
/// </summary>
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var router = new CommandRouter();
        return await router.ExecuteAsync(args);
    }
}