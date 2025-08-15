using Microsoft.Extensions.Logging;
using NuitsJp.GistGet.Business.Models;
using NuitsJp.GistGet.Infrastructure.Os;
using NuitsJp.GistGet.Infrastructure.WinGet;
using NuitsJp.GistGet.Models;
using System.Diagnostics;

namespace NuitsJp.GistGet.Business;

/// <summary>
/// Gist同期サービスの実装
/// syncコマンドのメイン処理を担当（Gist更新なし、一方向同期）
/// </summary>
public class GistSyncService : IGistSyncService
{
    private readonly IGistManager _gistManager;
    private readonly IWinGetClient _winGetClient;
    private readonly IOsService _osService;
    private readonly ILogger<GistSyncService> _logger;

    public GistSyncService(
        IGistManager gistManager,
        IWinGetClient winGetClient,
        IOsService osService,
        ILogger<GistSyncService> logger)
    {
        _gistManager = gistManager ?? throw new ArgumentNullException(nameof(gistManager));
        _winGetClient = winGetClient ?? throw new ArgumentNullException(nameof(winGetClient));
        _osService = osService ?? throw new ArgumentNullException(nameof(osService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// install/uninstallコマンド専用（syncでは何もしない）
    /// </summary>
    public void AfterInstall(string packageId)
    {
        _logger.LogInformation("Package installed: {PackageId}, Gist sync will be handled separately", packageId);
    }

    /// <summary>
    /// install/uninstallコマンド専用（syncでは何もしない）
    /// </summary>
    public void AfterUninstall(string packageId)
    {
        _logger.LogInformation("Package uninstalled: {PackageId}, Gist sync will be handled separately", packageId);
    }

    /// <summary>
    /// Gist → ローカルの一方向同期を実行
    /// </summary>
    public async Task<SyncResult> SyncAsync()
    {
        var result = new SyncResult { ExitCode = 0 };

        try
        {
            _logger.LogInformation("Starting Gist sync operation");

            // 1. 事前確認
            if (!await _gistManager.IsConfiguredAsync())
            {
                _logger.LogError("Gist configuration not found. Please run 'gistget gist set' first.");
                result.ExitCode = 1;
                return result;
            }

            // 2. Gistからパッケージ定義取得
            var gistPackages = await _gistManager.GetGistPackagesAsync();
            _logger.LogInformation("Retrieved {Count} packages from Gist", gistPackages.Count);

            // 3. ローカル状態取得
            await _winGetClient.InitializeAsync();
            var installedPackages = await _winGetClient.GetInstalledPackagesAsync();
            _logger.LogInformation("Found {Count} installed packages locally", installedPackages.Count);

            // 4. 差分検出
            var plan = DetectDifferences(gistPackages, installedPackages);
            _logger.LogInformation("Sync plan: {Install} to install, {Uninstall} to uninstall, {Skip} already installed, {NotFound} not found",
                plan.ToInstall.Count, plan.ToUninstall.Count, plan.AlreadyInstalled.Count, plan.NotFound.Count);

            if (plan.IsEmpty)
            {
                _logger.LogInformation("No changes required. System is already in sync.");
                return result;
            }

            // 5. 同期実行
            await ExecuteSyncPlan(plan, result);

            // 6. 再起動処理
            if (result.InstalledPackages.Count > 0)
            {
                result.RebootRequired = CheckRebootRequired(result.InstalledPackages);
                if (result.RebootRequired)
                {
                    _logger.LogInformation("Reboot required for installed packages");
                    // 実際の再起動はSyncCommandで処理（ユーザー確認が必要）
                }
            }

            // 7. 結果判定
            if (result.FailedPackages.Count > 0)
            {
                result.ExitCode = 1;
                _logger.LogWarning("Sync completed with {Failed} failures", result.FailedPackages.Count);
            }
            else
            {
                _logger.LogInformation("Gist sync completed successfully");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sync operation failed: {Message}", ex.Message);
            result.ExitCode = 1;
            return result;
        }
    }

    /// <summary>
    /// Gist定義とローカル状態の差分検出ロジック
    /// </summary>
    private SyncPlan DetectDifferences(PackageCollection gistPackages, List<PackageDefinition> installedPackages)
    {
        var plan = new SyncPlan();

        // インストール済みパッケージのIDセットを作成（高速検索用）
        var installedIds = installedPackages.Select(p => p.Id.ToLowerInvariant()).ToHashSet();

        foreach (var gistPackage in gistPackages)
        {
            var packageId = gistPackage.Id.ToLowerInvariant();

            if (string.Equals(gistPackage.Uninstall, "true", StringComparison.OrdinalIgnoreCase))
            {
                // アンインストール対象
                if (installedIds.Contains(packageId))
                {
                    plan.ToUninstall.Add(gistPackage);
                }
                // 既にアンインストール済みの場合は何もしない
            }
            else
            {
                // インストール対象
                if (installedIds.Contains(packageId))
                {
                    // 既にインストール済み（冪等性）
                    plan.AlreadyInstalled.Add(gistPackage);
                }
                else
                {
                    // インストールが必要
                    plan.ToInstall.Add(gistPackage);
                }
            }
        }

        return plan;
    }

    /// <summary>
    /// 同期計画を実行
    /// </summary>
    private async Task ExecuteSyncPlan(SyncPlan plan, SyncResult result)
    {
        // インストール処理
        foreach (var package in plan.ToInstall)
        {
            try
            {
                _logger.LogInformation("Installing package: {PackageId}", package.Id);
                var args = new[] { "install", package.Id, "--accept-source-agreements", "--accept-package-agreements" };
                var exitCode = await _winGetClient.InstallPackageAsync(args);

                if (exitCode == 0)
                {
                    result.InstalledPackages.Add(package.Id);
                    _logger.LogInformation("Successfully installed: {PackageId}", package.Id);
                }
                else
                {
                    result.FailedPackages.Add(package.Id);
                    _logger.LogError("Failed to install: {PackageId}, exit code: {ExitCode}", package.Id, exitCode);
                }
            }
            catch (Exception ex)
            {
                result.FailedPackages.Add(package.Id);
                _logger.LogError(ex, "Exception during install of: {PackageId}", package.Id);
            }
        }

        // アンインストール処理
        foreach (var package in plan.ToUninstall)
        {
            try
            {
                _logger.LogInformation("Uninstalling package: {PackageId}", package.Id);
                var args = new[] { "uninstall", package.Id, "--accept-source-agreements" };
                var exitCode = await _winGetClient.UninstallPackageAsync(args);

                if (exitCode == 0)
                {
                    result.UninstalledPackages.Add(package.Id);
                    _logger.LogInformation("Successfully uninstalled: {PackageId}", package.Id);
                }
                else
                {
                    result.FailedPackages.Add(package.Id);
                    _logger.LogError("Failed to uninstall: {PackageId}, exit code: {ExitCode}", package.Id, exitCode);
                }
            }
            catch (Exception ex)
            {
                result.FailedPackages.Add(package.Id);
                _logger.LogError(ex, "Exception during uninstall of: {PackageId}", package.Id);
            }
        }
    }

    /// <summary>
    /// インストールしたパッケージで再起動が必要かチェック
    /// </summary>
    private bool CheckRebootRequired(List<string> installedPackages)
    {
        // 簡易実装：よく知られた再起動要求パッケージのチェック
        var rebootRequiredPatterns = new[]
        {
            "microsoft.visualstudio",
            "docker.dockerdesktop",
            "oracle.virtualbox",
            "vmware",
            ".net"
        };

        return installedPackages.Any(packageId =>
            rebootRequiredPatterns.Any(pattern =>
                packageId.Contains(pattern, StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// システム再起動の実行
    /// </summary>
    public async Task ExecuteRebootAsync()
    {
        try
        {
            _logger.LogInformation("Executing system reboot via OsService");
            await _osService.ExecuteRebootAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute system reboot: {Message}", ex.Message);
            throw;
        }
    }
}