using System.CommandLine;
using GistGet;
using GistGet.Infrastructure;
using GistGet.Infrastructure.Diagnostics;
using GistGet.Presentation;
using Microsoft.Extensions.DependencyInjection;

ServiceCollection services = new();

// GistGet
services.AddTransient<IGitHubService, GitHubService>();
services.AddTransient<IGistGetService, GistGetService>();

// Presentation
services.AddTransient<CommandBuilder>();
services.AddTransient<IConsoleService, ConsoleService>();

// Infrastructure
services.AddTransient<ICredentialService, CredentialService>();
services.AddTransient<IWinGetPassthroughRunner, WinGetPassthroughRunner>();

await services
    .BuildServiceProvider()
    .GetRequiredService<CommandBuilder>()
    .Build()
    .InvokeAsync(args);