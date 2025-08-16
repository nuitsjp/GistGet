using Microsoft.Extensions.Logging;
using NuitsJp.GistGet.Business;
using NuitsJp.GistGet.Infrastructure.Os;
using NuitsJp.GistGet.Infrastructure.WinGet;

namespace NuitsJp.GistGet.Presentation.WinGet;

/// <summary>
/// WinGetコマンド（install/uninstall/upgrade）の実装
/// </summary>
public class WinGetCommand(
    IPackageManagementService packageManagementService,
    IWinGetClient winGetClient,
    IOsService osService,
    IWinGetConsole console,
    ILogger<WinGetCommand> logger)
{
    private readonly IPackageManagementService _packageManagementService = packageManagementService ?? throw new ArgumentNullException(nameof(packageManagementService));
    private readonly IWinGetClient _winGetClient = winGetClient ?? throw new ArgumentNullException(nameof(winGetClient));
    private readonly IWinGetConsole _console = console ?? throw new ArgumentNullException(nameof(console));
    private readonly IOsService _osService = osService ?? throw new ArgumentNullException(nameof(osService));
    private readonly ILogger<WinGetCommand> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    // 再起動が必要なパッケージを追跡
    private readonly List<string> _packagesRequiringReboot = [];

    /// <summary>
    /// installコマンドを実行
    /// </summary>
    public async Task<int> ExecuteInstallAsync(string[] args)
    {
        var packageId = _packageManagementService.ExtractPackageId(args);
        if (packageId == null)
        {
            _console.ShowError(new ArgumentException("Package ID not specified"), "パッケージIDが指定されていません");
            return 1;
        }

        try
        {
            _console.NotifyInstallStarting(packageId);

            // ビジネスロジックに委譲（WinGet操作とGist同期の統合）
            var exitCode = await _packageManagementService.InstallPackageAsync(args);

            if (exitCode == 0)
            {
                _console.NotifyOperationSuccess("インストール", packageId);

                // Gist更新結果のUI表示
                NotifyGistUpdateResult(packageId, "インストール");

                // 再起動チェック
                await CheckAndPromptRebootAsync(packageId);
            }

            return exitCode;
        }
        catch (Exception ex)
        {
            _console.ShowError(ex, $"パッケージ {packageId} のインストールに失敗しました");
            return 1;
        }
    }

    /// <summary>
    /// uninstallコマンドを実行
    /// </summary>
    public async Task<int> ExecuteUninstallAsync(string[] args)
    {
        var packageId = _packageManagementService.ExtractPackageId(args);
        if (packageId == null)
        {
            _console.ShowError(new ArgumentException("Package ID not specified"), "パッケージIDが指定されていません");
            return 1;
        }

        try
        {
            _console.NotifyUninstallStarting(packageId);

            // ビジネスロジックに委譲（WinGet操作とGist同期の統合）
            var exitCode = await _packageManagementService.UninstallPackageAsync(args);

            if (exitCode == 0)
            {
                _console.NotifyOperationSuccess("アンインストール", packageId);

                // Gist更新結果のUI表示
                NotifyGistUpdateResult(packageId, "アンインストール");

                // 再起動チェック
                await CheckAndPromptRebootAsync(packageId);
            }

            return exitCode;
        }
        catch (Exception ex)
        {
            _console.ShowError(ex, $"パッケージ {packageId} のアンインストールに失敗しました");
            return 1;
        }
    }

    /// <summary>
    /// upgradeコマンドを実行
    /// </summary>
    public async Task<int> ExecuteUpgradeAsync(string[] args)
    {
        var packageId = _packageManagementService.ExtractPackageId(args);
        if (packageId == null)
        {
            _console.ShowError(new ArgumentException("Package ID not specified"), "パッケージIDが指定されていません");
            return 1;
        }

        try
        {
            _console.NotifyUpgradeStarting(packageId);

            // ビジネスロジックに委譲（WinGet操作とGist同期の統合）
            var exitCode = await _packageManagementService.UpgradePackageAsync(args);

            if (exitCode == 0)
            {
                _console.NotifyOperationSuccess("アップグレード", packageId);

                // Gist更新結果のUI表示
                NotifyGistUpdateResult(packageId, "アップグレード");

                // 再起動チェック
                await CheckAndPromptRebootAsync(packageId);
            }

            return exitCode;
        }
        catch (Exception ex)
        {
            _console.ShowError(ex, $"パッケージ {packageId} のアップグレードに失敗しました");
            return 1;
        }
    }

    /// <summary>
    /// パススルーコマンドを実行
    /// </summary>
    public async Task<int> ExecutePassthroughAsync(string[] args)
    {
        try
        {
            _logger.LogDebug("Executing passthrough command: {Args}", string.Join(" ", args));

            // パススルーコマンドは単純にWinGetClientに委譲
            return await _winGetClient.ExecutePassthroughAsync(args);
        }
        catch (Exception ex)
        {
            _console.ShowError(ex, "WinGetコマンドの実行に失敗しました");
            return 1;
        }
    }

    /// <summary>
    /// Gist更新結果のUI表示
    /// </summary>
    private void NotifyGistUpdateResult(string packageId, string operation)
    {
        try
        {
            _console.NotifyGistUpdateStarting();
            // パッケージ管理サービスで既にGist更新は完了しているので、成功を通知
            _console.NotifyGistUpdateSuccess();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to notify Gist update for {PackageId} after {Operation}", packageId, operation);
            _console.ShowWarning($"パッケージの{operation}は成功しましたが、Gist更新通知に失敗しました: {ex.Message}");
        }
    }

    /// <summary>
    /// 再起動確認と実行（簡易実装）
    /// </summary>
    private async Task CheckAndPromptRebootAsync(string packageId)
    {
        // 簡易実装: 特定のパッケージは再起動が必要と判定
        var rebootRequiredPackages = new[]
        {
            "Microsoft.VisualStudio",
            "Microsoft.VisualStudioCode",
            "Microsoft.DotNet",
            "Git.Git",
            "Microsoft.PowerToys"
            // 他の主要なシステムレベルパッケージ
        };

        if (rebootRequiredPackages.Any(p => packageId.Contains(p, StringComparison.OrdinalIgnoreCase)))
        {
            _packagesRequiringReboot.Add(packageId);

            if (_console.ConfirmRebootWithPackageList(_packagesRequiringReboot))
            {
                try
                {
                    _console.NotifyRebootExecuting();
                    await _osService.ExecuteRebootAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to restart computer");
                    _console.ShowError(ex, "再起動に失敗しました");
                }
            }
        }
    }
}