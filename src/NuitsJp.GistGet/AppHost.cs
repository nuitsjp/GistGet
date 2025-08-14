using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NuitsJp.GistGet.Presentation;
using NuitsJp.GistGet.Business;
using NuitsJp.GistGet.Business.Services;
using NuitsJp.GistGet.Infrastructure.WinGet;
using NuitsJp.GistGet.Infrastructure.GitHub;
using NuitsJp.GistGet.Infrastructure.Storage;

using NuitsJp.GistGet.Presentation.Commands;

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

                // Commands
                services.AddSingleton<AuthCommand>();
                services.AddSingleton<IGistConfigService, GistConfigService>();
                services.AddSingleton<IGistManager, GistManager>();
                services.AddSingleton<GistSetCommand>();
                services.AddSingleton<GistStatusCommand>();
                services.AddSingleton<GistShowCommand>();

                // Infrastructure
                services.AddSingleton<IProcessWrapper, ProcessWrapper>();

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
