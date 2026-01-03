// GistGet CLI entry point and dependency injection bootstrap.

using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using NuitsJp.GistGet.Infrastructure;
using NuitsJp.GistGet.Infrastructure.Diagnostics;
using NuitsJp.GistGet.Presentation;
using Spectre.Console;

namespace NuitsJp.GistGet;

/// <summary>
/// Entry point type for the GistGet application.
/// </summary>
internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        ServiceCollection services = new();

        services.AddSingleton<IGitHubClientFactory, GitHubClientFactory>();
        services.AddTransient<IGitHubService, GitHubService>();
        services.AddTransient<IGistGetService, GistGetService>();

        services.AddSingleton(AnsiConsole.Console);
        services.AddTransient<CommandBuilder>();
        services.AddSingleton<IConsoleProxy, SystemConsoleProxy>();
        services.AddTransient<IConsoleService, ConsoleService>();

        services.AddTransient<ICredentialService, CredentialService>();
        services.AddSingleton<IProcessRunner, ProcessRunner>();
        services.AddTransient<IWinGetPassthroughRunner, WinGetPassthroughRunner>();
        services.AddTransient<IWinGetService, WinGetService>();
        services.AddTransient<IWinGetArgumentBuilder, WinGetArgumentBuilder>();

        return await services
            .BuildServiceProvider()
            .GetRequiredService<CommandBuilder>()
            .Build()
            .InvokeAsync(args);
    }
}




