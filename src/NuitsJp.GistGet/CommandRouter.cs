namespace NuitsJp.GistGet;

public class CommandRouter
{
    private readonly WinGetComClient _comClient = new();
    private readonly WinGetPassthrough _passthrough = new();

    public async Task<int> ExecuteAsync(string[] args)
    {
        if (args.Length == 0) return await _passthrough.ExecuteAsync(args);

        var command = args[0].ToLowerInvariant();
        var usesCom = command is "install" or "uninstall" or "upgrade";
        var usesGist = command is "sync" or "export" or "import";

        if (usesGist)
        {
            return command switch
            {
                "sync" => await GistSyncStub.SyncAsync(),
                "export" => await GistSyncStub.ExportAsync(),
                "import" => await GistSyncStub.ImportAsync(),
                _ => await _passthrough.ExecuteAsync(args)
            };
        }

        if (usesCom)
        {
            await _comClient.InitializeAsync();
            try
            {
                return command switch
                {
                    "install" => await _comClient.InstallPackageAsync(args),
                    "uninstall" => await _comClient.UninstallPackageAsync(args),
                    "upgrade" => await _comClient.UpgradePackageAsync(args),
                    _ => await _passthrough.ExecuteAsync(args)
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"COM API Error: {ex.Message}");
                return 1;
            }
        }

        return await _passthrough.ExecuteAsync(args);
    }
}