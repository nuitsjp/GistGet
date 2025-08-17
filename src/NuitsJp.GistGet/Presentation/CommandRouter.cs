using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using NuitsJp.GistGet.Business;
using NuitsJp.GistGet.Infrastructure.GitHub;
using NuitsJp.GistGet.Presentation.GistConfig;
using NuitsJp.GistGet.Presentation.Login;
using NuitsJp.GistGet.Presentation.Sync;
using NuitsJp.GistGet.Presentation.WinGet;
using NuitsJp.GistGet.Presentation.File;

namespace NuitsJp.GistGet.Presentation;

/// <summary>
/// コマンド実行サービス（アーキテクチャ改善版）
/// </summary>
public class CommandRouter(
    GistSetCommand gistSetCommand,
    GistStatusCommand gistStatusCommand,
    GistShowCommand gistShowCommand,
    GistClearCommand gistClearCommand,
    SyncCommand syncCommand,
    WinGetCommand winGetCommand,
    ILogger<CommandRouter> logger,
    IErrorMessageService errorMessageService,
    IGitHubAuthService authService,
    IGistManager gistManager,
    LoginCommand loginCommand,
    LogoutCommand logoutCommand,
    DownloadCommand downloadCommand,
    UploadCommand uploadCommand) : ICommandRouter
{

    public async Task<int> ExecuteAsync(string[] args)
    {
        try
        {
            logger.LogInformation("Executing command with args: {Args}", string.Join(" ", args));

            if (args.Length == 0)
                // 引数がない場合はwingetのヘルプを表示
                return await winGetCommand.ExecutePassthroughAsync(["--help"]);

            // サイレントモードの検出と除去
            var (isSilentMode, filteredArgs) = ExtractSilentMode(args);
            logger.LogDebug("Silent mode: {IsSilentMode}", isSilentMode);

            var command = NormalizeCommand(filteredArgs[0].ToLowerInvariant());

            // 認証・Gist設定が必要なコマンドを判定
            if (RequiresAuthentication(command, filteredArgs))
            {
                // 認証チェック・自動ログイン
                if (!await EnsureAuthenticatedAsync(isSilentMode)) return 1;

                // Gist設定チェック・自動設定
                if (RequiresGistConfiguration(command, filteredArgs))
                    if (!await EnsureGistConfiguredAsync(isSilentMode))
                        return 1;
            }

            return await RouteCommandAsync(command, filteredArgs);
        }
        catch (COMException comEx)
        {
            errorMessageService.HandleComException(comEx);
            return 1;
        }
        catch (HttpRequestException httpEx)
        {
            errorMessageService.HandleNetworkException(httpEx);
            return 1;
        }
        catch (InvalidOperationException invEx) when (invEx.Message.Contains("not found"))
        {
            errorMessageService.HandlePackageNotFoundException(invEx);
            return 1;
        }
        catch (Exception ex)
        {
            errorMessageService.HandleUnexpectedException(ex);
            return 1;
        }
    }

    private async Task<int> RouteCommandAsync(string command, string[] args)
    {
        var usesCom = command is "install" or "uninstall" or "upgrade";
        var usesGist = command is "sync";
        var usesFileGist = command is "download" or "upload";
        var usesPassthrough = command is "export" or "import" or "list" or "search" or "show";
        var isLoginCommand = command is "login";
        var isLogoutCommand = command is "logout";
        var isGistSubCommand = command is "gist";

        if (isLoginCommand) return await loginCommand.ExecuteAsync(args);
        if (isLogoutCommand) return await logoutCommand.ExecuteAsync(args);

        if (usesFileGist) return await HandleFileGistCommandAsync(command, args);

        if (isGistSubCommand) return await HandleGistSubCommandAsync(args);


        if (usesGist) return await HandleGistCommandAsync(command, args);

        if (usesCom) return await HandleComCommandAsync(command, args);

        if (usesPassthrough)
        {
            logger.LogDebug("Routing to passthrough client for explicit command: {Command}", command);
            return await winGetCommand.ExecutePassthroughAsync(args);
        }

        logger.LogDebug("Routing to passthrough client for unknown command: {Command}", command);
        return await winGetCommand.ExecutePassthroughAsync(args);
    }

    private async Task<int> HandleGistSubCommandAsync(string[] args)
    {
        if (args.Length < 2)
        {
            System.Console.WriteLine("Usage: gistget gist <command> [options]");
            System.Console.WriteLine();
            System.Console.WriteLine("Commands:");
            System.Console.WriteLine("  set [--gist-id <id>] [--file <filename>]  - Gist設定を行います");
            System.Console.WriteLine("  status                                     - 現在のGist設定状態を表示します");
            System.Console.WriteLine("  show [--raw]                              - Gistの内容を表示します");
            System.Console.WriteLine("  clear                                      - Gist設定をクリアします");
            System.Console.WriteLine();
            System.Console.WriteLine("Examples:");
            System.Console.WriteLine("  gistget gist set --gist-id abc123 --file packages.yaml");
            System.Console.WriteLine("  gistget gist set                  # 対話的に設定");
            System.Console.WriteLine("  gistget gist status");
            System.Console.WriteLine("  gistget gist show");
            System.Console.WriteLine("  gistget gist show --raw          # Raw YAML形式で表示");
            return 1;
        }

        var subCommand = args[1].ToLowerInvariant();
        logger.LogDebug("Routing to Gist sub-command: {SubCommand}", subCommand);

        return subCommand switch
        {
            "set" => await HandleGistSetAsync(args),
            "status" => await gistStatusCommand.ExecuteAsync(),
            "show" => await HandleGistShowAsync(args),
            "clear" => await gistClearCommand.ExecuteAsync(),
            _ => await ShowGistSubCommandHelp(subCommand)
        };
    }

    private async Task<int> HandleGistSetAsync(string[] args)
    {
        string? gistId = null;
        string? fileName = null;

        // 引数解析
        for (var i = 2; i < args.Length; i++)
            if (args[i] == "--gist-id" && i + 1 < args.Length)
            {
                gistId = args[i + 1];
                i++; // Skip next argument
            }
            else if (args[i] == "--file" && i + 1 < args.Length)
            {
                fileName = args[i + 1];
                i++; // Skip next argument
            }

        return await gistSetCommand.ExecuteAsync(gistId, fileName);
    }

    private async Task<int> HandleGistShowAsync(string[] args)
    {
        var raw = args.Contains("--raw");
        return await gistShowCommand.ExecuteAsync(raw);
    }

    private Task<int> ShowGistSubCommandHelp(string invalidSubCommand)
    {
        System.Console.WriteLine($"Error: Unknown gist command '{invalidSubCommand}'");
        System.Console.WriteLine();
        System.Console.WriteLine("Available commands:");
        System.Console.WriteLine("  set     - Gist設定を行います");
        System.Console.WriteLine("  status  - 現在のGist設定状態を表示します");
        System.Console.WriteLine("  show    - Gistの内容を表示します");
        System.Console.WriteLine("  clear   - Gist設定をクリアします");
        System.Console.WriteLine();
        System.Console.WriteLine("Use 'gistget gist' for more information.");
        return Task.FromResult(1);
    }

    /// <summary>
    /// 認証が必要なコマンドかどうかを判定
    /// </summary>
    private bool RequiresAuthentication(string command, string[] args)
    {
        // 認証が必要なコマンド
        return command switch
        {
            "sync" => true,
            "install" or "uninstall" or "upgrade" => true,
            "download" or "upload" => true,
            "gist" when args.Length > 1 && args[1] is "set" or "show" => true,
            _ => false
        };
    }

    /// <summary>
    /// Gist設定が必要なコマンドかどうかを判定
    /// </summary>
    private bool RequiresGistConfiguration(string command, string[] args)
    {
        // Gist設定が必要なコマンド（認証済みが前提）
        return command switch
        {
            "sync" => true,
            "install" or "uninstall" or "upgrade" => true,
            "download" or "upload" => true,
            "gist" when args.Length > 1 && args[1] == "show" => true,
            _ => false
        };
    }

    /// <summary>
    /// 認証状態を確認し、必要に応じて自動ログイン
    /// </summary>
    private async Task<bool> EnsureAuthenticatedAsync(bool isSilentMode = false)
    {
        if (await authService.IsAuthenticatedAsync()) return true;

        if (isSilentMode)
        {
            logger.LogError("Silent mode: Authentication required but not available");
            System.Console.Error.WriteLine("Error: Authentication required but not available in silent mode.");
            System.Console.Error.WriteLine("Please run 'gistget login' first.");
            return false;
        }

        logger.LogInformation("認証が必要です。ログインを開始します...");
        System.Console.WriteLine("認証が必要です。GitHubログインを開始します...");

        // テストで期待されるAuthenticateAsyncメソッドを直接呼び出し
        await authService.AuthenticateAsync();
        return await authService.IsAuthenticatedAsync();
    }

    /// <summary>
    /// Gist設定状態を確認し、必要に応じて自動設定
    /// </summary>
    private async Task<bool> EnsureGistConfiguredAsync(bool isSilentMode = false)
    {
        if (await gistManager.IsConfiguredAsync()) return true;

        if (isSilentMode)
        {
            logger.LogError("Silent mode: Gist configuration required but not available");
            System.Console.Error.WriteLine("Error: Gist configuration required but not available in silent mode.");
            System.Console.Error.WriteLine("Please run 'gistget gist set' first.");
            return false;
        }

        logger.LogInformation("Gist設定が必要です。設定を開始します...");
        System.Console.WriteLine("Gist設定が必要です。設定を開始します...");

        // GistSetCommandを実行（対話形式）
        var result = await gistSetCommand.ExecuteAsync(null, null);
        return result == 0;
    }

    private Task<int> HandleGistCommandAsync(string command, string[] args)
    {
        logger.LogDebug("Routing to Gist service for command: {Command}", command);
        return command switch
        {
            "sync" => syncCommand.ExecuteAsync(args),
            _ => throw new ArgumentException($"Unsupported gist command: {command}")
        };
    }

    private async Task<int> HandleComCommandAsync(string command, string[] args)
    {
        logger.LogDebug("Routing to WinGet COM command: {Command}", command);
        return command switch
        {
            "install" => await winGetCommand.ExecuteInstallAsync(args),
            "uninstall" => await winGetCommand.ExecuteUninstallAsync(args),
            "upgrade" => await winGetCommand.ExecuteUpgradeAsync(args),
            _ => throw new ArgumentException($"Unsupported COM command: {command}")
        };
    }

    private async Task<int> HandleFileGistCommandAsync(string command, string[] args)
    {
        logger.LogDebug("Routing to file Gist command: {Command}", command);
        return command switch
        {
            "download" => await downloadCommand.ExecuteAsync(args),
            "upload" => await uploadCommand.ExecuteAsync(args),
            _ => throw new ArgumentException($"Unsupported file Gist command: {command}")
        };
    }

    /// <summary>
    /// サイレントモード（--silent, -s）フラグを検出して除去
    /// </summary>
    private static (bool isSilentMode, string[] filteredArgs) ExtractSilentMode(string[] args)
    {
        string[] silentFlags = ["--silent", "-s"];
        var filteredList = new List<string>();
        var isSilentMode = false;

        foreach (var arg in args)
        {
            if (silentFlags.Contains(arg.ToLowerInvariant()))
            {
                isSilentMode = true;
            }
            else
            {
                filteredList.Add(arg);
            }
        }

        return (isSilentMode, filteredList.ToArray());
    }

    /// <summary>
    /// コマンドエイリアスを正規コマンドに変換する
    /// </summary>
    private static string NormalizeCommand(string command)
    {
        var aliasMap = new Dictionary<string, string>
        {
            { "add", "install" },
            { "remove", "uninstall" },
            { "rm", "uninstall" },
            { "update", "upgrade" },
            { "ls", "list" },
            { "find", "search" },
            { "view", "show" },
            { "config", "settings" }
        };

        return aliasMap.TryGetValue(command, out var normalizedCommand) ? normalizedCommand : command;
    }
}