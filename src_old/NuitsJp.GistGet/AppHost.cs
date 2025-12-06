using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NuitsJp.GistGet.Business;
using NuitsJp.GistGet.Business.Services;
using NuitsJp.GistGet.Infrastructure.GitHub;
using NuitsJp.GistGet.Infrastructure.Os;
using NuitsJp.GistGet.Infrastructure.Storage;
using NuitsJp.GistGet.Infrastructure.WinGet;
using NuitsJp.GistGet.Presentation;
using NuitsJp.GistGet.Presentation.GistConfig;
using NuitsJp.GistGet.Presentation.Login;
using NuitsJp.GistGet.Presentation.Sync;
using NuitsJp.GistGet.Presentation.WinGet;
using NuitsJp.GistGet.Presentation.File;
// using NuitsJp.GistGet.Presentation.Commands; // 旧Commands名前空間をコメントアウト

namespace NuitsJp.GistGet;

/// <summary>
/// アプリケーションのホスト（DI 構築）を行うエントリポイント分離クラス
/// </summary>
public static class AppHost
{
    public static IHost CreateHost()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                // Logging
                services.AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Information);
                });

                // Core services
                services.AddSingleton<ICommandRouter, CommandRouter>();
                services.AddSingleton<IErrorMessageService, ErrorMessageService>();

                // WinGet clients
                services.AddSingleton<IWinGetClient, WinGetComClient>();
                services.AddSingleton<IWinGetPassthroughClient, WinGetPassthroughClient>();

                // Package management (Business layer)
                services.AddSingleton<IPackageManagementService, PackageManagementService>();

                // Gist services (core)
                services.AddSingleton<IGistSyncService, GistSyncService>();

                // GitHub services
                services.AddSingleton<IGitHubAuthService, GitHubAuthService>();
                services.AddSingleton<GitHubAuthService>(); // 一部のコマンドが具象型を要求するため自己型も登録

                // Console abstractions
                services.AddSingleton<ILoginConsole, LoginConsole>();
                services.AddSingleton<ILogoutConsole, LogoutConsole>();
                services.AddSingleton<IGistConfigConsole, GistConfigConsole>();
                services.AddSingleton<ISyncConsole, SyncConsole>();
                services.AddSingleton<IWinGetConsole, WinGetConsole>();
                services.AddSingleton<IFileConsole, FileConsole>();

                // Commands with new namespace structure
                services.AddSingleton<LoginCommand>();
                services.AddSingleton<LogoutCommand>();
                services.AddSingleton<GistSetCommand>();
                services.AddSingleton<GistStatusCommand>();
                services.AddSingleton<GistShowCommand>();
                services.AddSingleton<GistClearCommand>();
                services.AddSingleton<SyncCommand>();
                services.AddSingleton<WinGetCommand>();
                services.AddSingleton<DownloadCommand>();
                services.AddSingleton<UploadCommand>();

                // Business services
                services.AddSingleton<IGistConfigService, GistConfigService>();
                services.AddSingleton<IGistManager, GistManager>();

                // Infrastructure
                services.AddSingleton<IOsService, OsService>();

                // Supporting services used by commands/managers
                services.AddSingleton<GistInputService>();
                services.AddSingleton<IGitHubGistClient, GitHubGistClient>();
                services.AddSingleton<IPackageYamlConverter, PackageYamlConverter>();
                services.AddSingleton<GistManager>();
                services.AddSingleton<IGistConfigurationStorage>(_ => GistConfigurationStorage.CreateDefault());
            })
            .Build();
    }
}