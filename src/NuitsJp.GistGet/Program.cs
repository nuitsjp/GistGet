namespace NuitsJp.GistGet;

/// <summary>
/// MVP Phase 1: 最小限のパススルー実装
/// </summary>
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var passthrough = new WinGetPassthrough();
        return await passthrough.ExecuteAsync(args);
    }
}