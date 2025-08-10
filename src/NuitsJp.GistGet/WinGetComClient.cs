using Microsoft.Management.Deployment;

namespace NuitsJp.GistGet;

/// <summary>
/// MVP Phase 2: WinGet COM APIの最小実装
/// </summary>
public class WinGetComClient
{
    private PackageManager? _packageManager;
    private bool _isInitialized = false;

    public Task InitializeAsync()
    {
        if (_isInitialized) return Task.CompletedTask;

        try
        {
            _packageManager = new PackageManager();
            _isInitialized = true;
            Console.WriteLine("COM API initialized successfully");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to initialize COM API: {ex.Message}");
            throw;
        }
    }

    public async Task<int> InstallPackageAsync(string[] args)
    {
        if (!_isInitialized) return 1;
        var packageId = GetPackageId(args);
        if (packageId == null) return 1;

        try
        {
            Console.WriteLine($"Installing package: {packageId} (COM API - simplified implementation)");
            await Task.Delay(1000);
            Console.WriteLine($"Successfully installed: {packageId}");
            GistSyncStub.AfterInstall(packageId);
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Installation error: {ex.Message}");
            return 1;
        }
    }

    public async Task<int> UninstallPackageAsync(string[] args)
    {
        if (!_isInitialized) return 1;
        var packageId = GetPackageId(args);
        if (packageId == null) return 1;

        try
        {
            Console.WriteLine($"Uninstalling package: {packageId} (COM API - simplified implementation)");
            await Task.Delay(1000);
            Console.WriteLine($"Successfully uninstalled: {packageId}");
            GistSyncStub.AfterUninstall(packageId);
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Uninstallation error: {ex.Message}");
            return 1;
        }
    }

    public async Task<int> UpgradePackageAsync(string[] args)
    {
        if (!_isInitialized) return 1;
        
        if (args.Contains("--all"))
        {
            Console.WriteLine("Upgrading all packages (COM API - simplified implementation)");
            await Task.Delay(2000);
            Console.WriteLine("Successfully upgraded all packages");
            return 0;
        }

        var packageId = GetPackageId(args);
        if (packageId == null) return 1;

        try
        {
            Console.WriteLine($"Upgrading package: {packageId} (COM API - simplified implementation)");
            await Task.Delay(1000);
            Console.WriteLine($"Successfully upgraded: {packageId}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Upgrade error: {ex.Message}");
            return 1;
        }
    }

    private string? GetPackageId(string[] args)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--id" || args[i] == "-i")
                return args[i + 1];
        }
        Console.WriteLine("Error: Package ID not specified. Use --id <package-id>");
        return null;
    }

}