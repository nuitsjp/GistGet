namespace NuitsJp.GistGet;

/// <summary>
/// MVP Phase 3: Gist同期のスタブ実装
/// </summary>
public class GistSyncStub
{
    public static void AfterInstall(string packageId)
    {
        Console.WriteLine($"Gist updated (after installing {packageId})");
    }

    public static void AfterUninstall(string packageId)
    {
        Console.WriteLine($"Gist updated (after uninstalling {packageId})");
    }

    public static async Task<int> SyncAsync()
    {
        Console.WriteLine("Syncing from Gist...");
        await Task.Delay(500);
        Console.WriteLine("Sync completed successfully");
        return 0;
    }

    public static async Task<int> ExportAsync()
    {
        Console.WriteLine("Exporting to Gist...");
        await Task.Delay(500);
        Console.WriteLine("Export completed successfully");
        return 0;
    }

    public static async Task<int> ImportAsync()
    {
        Console.WriteLine("Importing from Gist...");
        await Task.Delay(500);
        Console.WriteLine("Import completed successfully");
        return 0;
    }
}