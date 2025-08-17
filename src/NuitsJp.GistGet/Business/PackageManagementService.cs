using Microsoft.Extensions.Logging;
using NuitsJp.GistGet.Infrastructure.WinGet;

namespace NuitsJp.GistGet.Business;

/// <summary>
/// パッケージ管理操作とGist同期を統合するサービスの実装
/// </summary>
public class PackageManagementService(
    IWinGetClient winGetClient,
    IGistSyncService gistSyncService,
    ILogger<PackageManagementService> logger) : IPackageManagementService
{
    private readonly IWinGetClient _winGetClient = winGetClient ?? throw new ArgumentNullException(nameof(winGetClient));
    private readonly IGistSyncService _gistSyncService = gistSyncService ?? throw new ArgumentNullException(nameof(gistSyncService));
    private readonly ILogger<PackageManagementService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<int> InstallPackageAsync(string[] args)
    {
        var packageId = ExtractPackageId(args);
        if (packageId == null)
        {
            _logger.LogWarning("Package ID not specified in args: {Args}", string.Join(" ", args));
            return 1;
        }

        // --no-gistオプションの確認
        var skipGistUpdate = args.Contains("--no-gist");

        try
        {
            _logger.LogInformation("Installing package: {PackageId}", packageId);

            // WinGet COM APIまたはwinget.exeでインストール実行
            var exitCode = await _winGetClient.InstallPackageAsync(args);

            if (exitCode == 0)
            {
                _logger.LogInformation("Package installation successful: {PackageId}", packageId);

                // Gist自動更新（--no-gistオプションが指定されていない場合のみ）
                if (!skipGistUpdate)
                {
                    await UpdateGistAfterInstallAsync(packageId);
                }
                else
                {
                    _logger.LogInformation("Skipping Gist update due to --no-gist option");
                }
            }
            else
            {
                _logger.LogWarning("Package installation failed: {PackageId}, exit code: {ExitCode}", packageId, exitCode);
            }

            return exitCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during package installation: {PackageId}", packageId);
            throw;
        }
    }

    public async Task<int> UninstallPackageAsync(string[] args)
    {
        var packageId = ExtractPackageId(args);
        if (packageId == null)
        {
            _logger.LogWarning("Package ID not specified in args: {Args}", string.Join(" ", args));
            return 1;
        }

        // --no-gistオプションの確認
        var skipGistUpdate = args.Contains("--no-gist");

        try
        {
            _logger.LogInformation("Uninstalling package: {PackageId}", packageId);

            // WinGet COM APIまたはwinget.exeでアンインストール実行
            var exitCode = await _winGetClient.UninstallPackageAsync(args);

            if (exitCode == 0)
            {
                _logger.LogInformation("Package uninstallation successful: {PackageId}", packageId);

                // Gist自動更新（--no-gistオプションが指定されていない場合のみ）
                if (!skipGistUpdate)
                {
                    await UpdateGistAfterUninstallAsync(packageId);
                }
                else
                {
                    _logger.LogInformation("Skipping Gist update due to --no-gist option");
                }
            }
            else
            {
                _logger.LogWarning("Package uninstallation failed: {PackageId}, exit code: {ExitCode}", packageId, exitCode);
            }

            return exitCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during package uninstallation: {PackageId}", packageId);
            throw;
        }
    }

    public async Task<int> UpgradePackageAsync(string[] args)
    {
        var packageId = ExtractPackageId(args);
        if (packageId == null)
        {
            _logger.LogWarning("Package ID not specified in args: {Args}", string.Join(" ", args));
            return 1;
        }

        // --no-gistオプションの確認
        var skipGistUpdate = args.Contains("--no-gist");

        try
        {
            _logger.LogInformation("Upgrading package: {PackageId}", packageId);

            // WinGet COM APIまたはwinget.exeでアップグレード実行
            var exitCode = await _winGetClient.UpgradePackageAsync(args);

            if (exitCode == 0)
            {
                _logger.LogInformation("Package upgrade successful: {PackageId}", packageId);

                // Gist自動更新（--no-gistオプションが指定されていない場合のみ）
                // アップグレードは既存パッケージの更新なので、インストール扱いでGist更新
                if (!skipGistUpdate)
                {
                    await UpdateGistAfterInstallAsync(packageId);
                }
                else
                {
                    _logger.LogInformation("Skipping Gist update due to --no-gist option");
                }
            }
            else
            {
                _logger.LogWarning("Package upgrade failed: {PackageId}, exit code: {ExitCode}", packageId, exitCode);
            }

            return exitCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during package upgrade: {PackageId}", packageId);
            throw;
        }
    }

    public string? ExtractPackageId(string[] args)
    {
        // --id フラグが指定されている場合
        for (var i = 0; i < args.Length - 1; i++)
            if (args[i] == "--id" || args[i] == "-i")
                return args[i + 1];

        // winget.exe形式の引数解析: 最初の引数（install/uninstall/upgrade）の後がパッケージID
        if (args.Length >= 2 &&
            (args[0] == "install" || args[0] == "uninstall" || args[0] == "upgrade"))
            // 2番目の引数がオプション（--で始まる）でない場合、それがパッケージID
            if (!args[1].StartsWith("--") && !args[1].StartsWith("-"))
                return args[1];

        return null;
    }

    private async Task UpdateGistAfterInstallAsync(string packageId)
    {
        try
        {
            _logger.LogDebug("Updating Gist after installing {PackageId}", packageId);
            await _gistSyncService.AfterInstallAsync(packageId);
            _logger.LogInformation("Gist updated successfully after installing {PackageId}", packageId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update Gist after installing {PackageId}", packageId);
            // Gist更新の失敗はインストール成功を妨げない
        }
    }

    private async Task UpdateGistAfterUninstallAsync(string packageId)
    {
        try
        {
            _logger.LogDebug("Updating Gist after uninstalling {PackageId}", packageId);
            await _gistSyncService.AfterUninstallAsync(packageId);
            _logger.LogInformation("Gist updated successfully after uninstalling {PackageId}", packageId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update Gist after uninstalling {PackageId}", packageId);
            // Gist更新の失敗はアンインストール成功を妨げない
        }
    }
}