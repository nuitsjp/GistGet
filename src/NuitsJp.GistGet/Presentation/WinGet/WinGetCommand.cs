using Microsoft.Extensions.Logging;
using NuitsJp.GistGet.Business;
using NuitsJp.GistGet.Infrastructure.Os;
using NuitsJp.GistGet.Infrastructure.WinGet;

namespace NuitsJp.GistGet.Presentation.WinGet;

/// <summary>
/// WinGetコマンド（install/uninstall/upgrade）の実装
/// </summary>
public class WinGetCommand(
    IWinGetClient winGetClient,
    IGistSyncService gistSyncService,
    IOsService osService,
    IWinGetConsole console,
    ILogger<WinGetCommand> logger)
{
    private readonly IWinGetConsole _console = console ?? throw new ArgumentNullException(nameof(console));
    private readonly IGistSyncService _gistSyncService = gistSyncService ?? throw new ArgumentNullException(nameof(gistSyncService));
    private readonly ILogger<WinGetCommand> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IOsService _osService = osService ?? throw new ArgumentNullException(nameof(osService));

    // 再起動が必要なパッケージを追跡
    private readonly List<string> _packagesRequiringReboot = [];
    private readonly IWinGetClient _winGetClient = winGetClient ?? throw new ArgumentNullException(nameof(winGetClient));

    /// <summary>
    /// installコマンドを実行
    /// </summary>
    public async Task<int> ExecuteInstallAsync(string[] args)
    {
        var packageId = GetPackageId(args);
        if (packageId == null)
        {
            _console.ShowError(new ArgumentException("Package ID not specified"), "パッケージIDが指定されていません");
            return 1;
        }

        try
        {
            _console.NotifyInstallStarting(packageId);

            // WinGet COM APIまたはwinget.exeでインストール実行
            var exitCode = await _winGetClient.InstallPackageAsync(args);

            if (exitCode == 0)
            {
                _console.NotifyOperationSuccess("インストール", packageId);

                // Gist自動更新
                await UpdateGistAfterInstall(packageId);

                // 簡易実装: すべてのパッケージで再起動が必要と仮定
                // 将来改善: WinGet COM APIの結果から実際の再起動要否を判定
                await CheckAndPromptRebootAsync(packageId);

                return 0;
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
        var packageId = GetPackageId(args);
        if (packageId == null)
        {
            _console.ShowError(new ArgumentException("Package ID not specified"), "パッケージIDが指定されていません");
            return 1;
        }

        try
        {
            _console.NotifyUninstallStarting(packageId);

            // WinGet COM APIまたはwinget.exeでアンインストール実行
            var exitCode = await _winGetClient.UninstallPackageAsync(args);

            if (exitCode == 0)
            {
                _console.NotifyOperationSuccess("アンインストール", packageId);

                // Gist自動更新
                await UpdateGistAfterUninstall(packageId);

                // 簡易実装: アンインストールでも再起動が必要な場合がある
                await CheckAndPromptRebootAsync(packageId);

                return 0;
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
        var packageId = GetPackageId(args);
        if (packageId == null)
        {
            _console.ShowError(new ArgumentException("Package ID not specified"), "パッケージIDが指定されていません");
            return 1;
        }

        try
        {
            _console.NotifyUpgradeStarting(packageId);

            // WinGet COM APIまたはwinget.exeでアップグレード実行
            var exitCode = await _winGetClient.UpgradePackageAsync(args);

            if (exitCode == 0)
            {
                _console.NotifyOperationSuccess("アップグレード", packageId);

                // TODO: バージョン固定機能の実装
                // PowerShell版のような詳細なバージョン管理を将来実装

                // 簡易実装: アップグレードでも再起動が必要な場合がある
                await CheckAndPromptRebootAsync(packageId);

                return 0;
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

            // WinGetPassthroughを使用してwinget.exeにそのまま渡す
            return await _winGetClient.ExecutePassthroughAsync(args);
        }
        catch (Exception ex)
        {
            _console.ShowError(ex, "WinGetコマンドの実行に失敗しました");
            return 1;
        }
    }

    private async Task UpdateGistAfterInstall(string packageId)
    {
        try
        {
            _console.NotifyGistUpdateStarting();
            await _gistSyncService.AfterInstallAsync(packageId);
            _console.NotifyGistUpdateSuccess();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update Gist after installing {PackageId}", packageId);
            _console.ShowWarning($"パッケージのインストールは成功しましたが、Gist更新に失敗しました: {ex.Message}");
        }
    }

    private async Task UpdateGistAfterUninstall(string packageId)
    {
        try
        {
            _console.NotifyGistUpdateStarting();
            await _gistSyncService.AfterUninstallAsync(packageId);
            _console.NotifyGistUpdateSuccess();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update Gist after uninstalling {PackageId}", packageId);
            _console.ShowWarning($"パッケージのアンインストールは成功しましたが、Gist更新に失敗しました: {ex.Message}");
        }
    }

    private string? GetPackageId(string[] args)
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

        _logger.LogWarning("Package ID not specified in args: {Args}", string.Join(" ", args));
        return null;
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