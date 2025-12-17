// GistGet CLI entry point and dependency injection bootstrap.

using System.CommandLine;
using GistGet;
using GistGet.Infrastructure;
using GistGet.Infrastructure.Diagnostics;
using GistGet.Presentation;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

ServiceCollection services = new();

services.AddSingleton<IGitHubClientFactory, GitHubClientFactory>();
services.AddTransient<IGitHubService, GitHubService>();
services.AddTransient<IGistGetService, GistGetService>();

services.AddSingleton<IAnsiConsole>(AnsiConsole.Console);
services.AddTransient<CommandBuilder>();
services.AddTransient<IConsoleService, ConsoleService>();

services.AddTransient<ICredentialService, CredentialService>();
services.AddSingleton<IProcessRunner, ProcessRunner>();
services.AddTransient<IWinGetPassthroughRunner, WinGetPassthroughRunner>();
services.AddTransient<IWinGetService, WinGetService>();
services.AddTransient<IWinGetArgumentBuilder, WinGetArgumentBuilder>();

await services
    .BuildServiceProvider()
    .GetRequiredService<CommandBuilder>()
    .Build()
    .InvokeAsync(args);

/// <summary>
/// Entry point type for the top-level program.
/// </summary>
internal partial class Program
{
}
