using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NuitsJp.GistGet.Presentation;
using NuitsJp.GistGet.Business;
using NuitsJp.GistGet.Business.Services;
using NuitsJp.GistGet.Infrastructure.Os;
using NuitsJp.GistGet.Infrastructure.WinGet;
using NuitsJp.GistGet.Infrastructure.GitHub;
using NuitsJp.GistGet.Infrastructure.Storage;

// using NuitsJp.GistGet.Presentation.Commands; // 旧Commands名前空間をコメントアウト
using NuitsJp.GistGet.Presentation.Console;
using NuitsJp.GistGet.Presentation.Auth;
using NuitsJp.GistGet.Presentation.GistConfig;
using NuitsJp.GistGet.Presentation.Sync;

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
                services.AddSingleton<IWinGetPassthroughClient, WinGetPassthrough>();

                // Gist services (core)
                services.AddSingleton<IGistSyncService, GistSyncService>();

                // GitHub services
                services.AddSingleton<IGitHubAuthService, GitHubAuthService>();
                services.AddSingleton<GitHubAuthService>(); // 一部のコマンドが具象型を要求するため自己型も登録

                // Console abstractions
                services.AddSingleton<IAuthConsole, AuthConsole>();
                services.AddSingleton<IGistConfigConsole, GistConfigConsole>();
                // services.AddSingleton<ISyncConsole, SyncConsole>(); // TODO: 循環依存解決後に復旧

                // Commands with new namespace structure
                services.AddSingleton<NuitsJp.GistGet.Presentation.Auth.AuthCommand>();
                services.AddSingleton<NuitsJp.GistGet.Presentation.GistConfig.GistSetCommand>();
                services.AddSingleton<NuitsJp.GistGet.Presentation.GistConfig.GistStatusCommand>();
                services.AddSingleton<NuitsJp.GistGet.Presentation.GistConfig.GistShowCommand>();
                // services.AddSingleton<NuitsJp.GistGet.Presentation.Sync.SyncCommand>(); // TODO: 循環依存解決後に復旧

                // Business services
                services.AddSingleton<IGistConfigService, GistConfigService>();
                services.AddSingleton<IGistManager, GistManager>();

                // Infrastructure
                services.AddSingleton<IProcessWrapper, ProcessWrapper>();
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
