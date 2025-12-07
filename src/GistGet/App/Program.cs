
using GistGet;
using GistGet.Command;
using GistGet.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

ServiceCollection services = new();
services.AddTransient<RootCommand>();
services.AddTransient<IWinGetPassthroughRunner, WinGetPassthroughRunner>();

await services
    .BuildServiceProvider()
    .GetRequiredService<RootCommand>()
    .RunAsync(args);