using System.CommandLine;
using GistGet;
using GistGet.Infrastructure;
using GistGet.Infrastructure.Diagnostics;
using GistGet.Presentation;
using Microsoft.Extensions.DependencyInjection;

ServiceCollection services = new();

// GistGet
services.AddSingleton<IGitHubClientFactory, GitHubClientFactory>();
services.AddTransient<IGitHubService, GitHubService>();
services.AddTransient<IGistGetService, GistGetService>();

// Presentation
services.AddTransient<CommandBuilder>();
services.AddTransient<IConsoleService, ConsoleService>();

// Infrastructure
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
